use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let protoc = protoc_bin_vendored::protoc_bin_path()?;
    let manifest_dir = PathBuf::from(env::var("CARGO_MANIFEST_DIR")?);
    let proto_dir = manifest_dir.join("../../proto");
    let proto_files = collect_proto_files(&proto_dir)?;
    let unity_generated_dir =
        manifest_dir.join("../../../packages/com.tonymarkham.fast-variants/Core/Generated");
    let proto_file_paths = proto_files.iter().map(PathBuf::as_path).collect::<Vec<_>>();

    let mut config = prost_build::Config::new();
    config.protoc_executable(&protoc);
    config.compile_protos(&proto_file_paths, &[proto_dir.as_path()])?;

    generate_csharp_protos(&protoc, &proto_dir, &proto_files, &unity_generated_dir)?;

    println!("cargo:rerun-if-changed={}", proto_dir.display());
    Ok(())
}

fn collect_proto_files(proto_dir: &Path) -> Result<Vec<PathBuf>, Box<dyn std::error::Error>> {
    let mut proto_files = fs::read_dir(proto_dir)?
        .filter_map(Result::ok)
        .map(|entry| entry.path())
        .filter(|path| {
            path.extension()
                .is_some_and(|extension| extension == "proto")
        })
        .collect::<Vec<_>>();

    proto_files.sort();

    if proto_files.is_empty() {
        return Err(format!("no .proto files found in {}", proto_dir.display()).into());
    }

    Ok(proto_files)
}

fn generate_csharp_protos(
    protoc: &Path,
    proto_dir: &Path,
    proto_files: &[PathBuf],
    output_dir: &Path,
) -> Result<(), Box<dyn std::error::Error>> {
    std::fs::create_dir_all(output_dir)?;

    let mut command = Command::new(protoc);
    command
        .arg(format!("--csharp_out={}", output_dir.display()))
        .arg(format!("--proto_path={}", proto_dir.display()));

    for proto_file in proto_files {
        command.arg(proto_file);
    }

    let output = command.output()?;

    if output.status.success() {
        return Ok(());
    }

    let stdout = String::from_utf8_lossy(&output.stdout);
    let stderr = String::from_utf8_lossy(&output.stderr);
    Err(format!("protoc C# generation failed:\n{stdout}{stderr}").into())
}
