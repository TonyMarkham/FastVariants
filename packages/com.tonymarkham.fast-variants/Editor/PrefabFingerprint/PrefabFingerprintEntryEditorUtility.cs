using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    public static class PrefabFingerprintEntryEditorUtility
    {
        public static GameObject GetGameObject(PrefabFingerprintEntry entry)
        {
            return entry?.transform != null ? entry.transform.gameObject : null;
        }

        public static bool IsValid(PrefabFingerprintEntry entry)
        {
            return entry?.pathKey != null && entry.transform != null;
        }

        public static bool AreEqual(PrefabFingerprintEntry left, PrefabFingerprintEntry right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return Equals(left.pathKey, right.pathKey)
                   && left.transform == right.transform;
        }

        public static int GetHashCode(PrefabFingerprintEntry entry)
        {
            if (entry == null)
                return 0;

            var hash = 17;
            hash = hash * 31 + (entry.pathKey != null ? entry.pathKey.GetHashCode() : 0);
            hash = hash * 31 + (entry.transform != null ? entry.transform.GetHashCode() : 0);
            return hash;
        }
    }
}
