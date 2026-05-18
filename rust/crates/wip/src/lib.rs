use prost::Message;
use std::ptr;
use std::slice;

pub mod proto {
    include!(concat!(env!("OUT_DIR"), "/fast_variants.rs"));
}

pub use proto::Variant;

#[repr(C)]
pub struct FastVariantsBuffer {
    pub ptr: *mut u8,
    pub len: usize,
}

pub fn decode_variant(bytes: &[u8]) -> Result<Variant, prost::DecodeError> {
    Variant::decode(bytes)
}

pub fn encode_variant(variant: &Variant) -> Vec<u8> {
    variant.encode_to_vec()
}

pub fn process_variant(mut variant: Variant) -> Variant {
    variant.code = append_rust_suffix(&variant.code);
    variant
}

#[unsafe(no_mangle)]
/// # Safety
///
/// `input_ptr` must point to `input_len` readable bytes, and `output` must be
/// valid for writing one `FastVariantsBuffer`.
pub unsafe extern "C" fn fast_variants_process_variant(
    input_ptr: *const u8,
    input_len: usize,
    output: *mut FastVariantsBuffer,
) -> i32 {
    if input_ptr.is_null() || output.is_null() {
        return -1;
    }

    let input = unsafe { slice::from_raw_parts(input_ptr, input_len) };
    let variant = match decode_variant(input) {
        Ok(variant) => variant,
        Err(_) => return -2,
    };

    let bytes = encode_variant(&process_variant(variant));
    let mut boxed = bytes.into_boxed_slice();
    let buffer = FastVariantsBuffer {
        ptr: boxed.as_mut_ptr(),
        len: boxed.len(),
    };

    Box::leak(boxed);
    unsafe { ptr::write(output, buffer) };
    0
}

#[unsafe(no_mangle)]
/// # Safety
///
/// `buffer` must be a buffer previously returned by `fast_variants_process_variant`
/// and not already freed.
pub unsafe extern "C" fn fast_variants_free_buffer(buffer: FastVariantsBuffer) {
    if buffer.ptr.is_null() {
        return;
    }

    let slice_ptr = ptr::slice_from_raw_parts_mut(buffer.ptr, buffer.len);
    unsafe { drop(Box::from_raw(slice_ptr)) };
}

fn append_rust_suffix(code: &str) -> String {
    if code.ends_with("-rust") {
        code.to_owned()
    } else if code.is_empty() {
        "rust".to_owned()
    } else {
        format!("{code}-rust")
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn variant_round_trips_through_protobuf() {
        let variant = Variant {
            id: "variant-id".to_owned(),
            name: "Example Variant".to_owned(),
            type_tag: "Variant".to_owned(),
            code: "example-variant".to_owned(),
            feature_set_id: "feature-set-id".to_owned(),
            enabled: true,
        };

        let bytes = encode_variant(&variant);
        let decoded = decode_variant(&bytes).expect("variant should decode");
        let processed = process_variant(decoded);

        assert_eq!(processed.code, "example-variant-rust");
        assert!(processed.enabled);
    }
}
