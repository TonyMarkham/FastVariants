using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    public static class PrefabFingerprintRegistryEditorUtility
    {
        public static bool TryAdd(PrefabFingerprintRegistry registry, PrefabHierarchyPathKey pathKey, Transform transform)
        {
            if (registry == null || pathKey == null || transform == null)
                return false;

            if (registry.ContainsPathKey(pathKey) || registry.ContainsTransform(transform))
                return false;

            var entries = CopyEntries(registry);
            entries.Add(new PrefabFingerprintEntry(pathKey, transform));
            registry.SetEntries(entries);
            return true;
        }

        public static bool TryAdd(PrefabFingerprintRegistry registry, PrefabFingerprintEntry entry)
        {
            if (!PrefabFingerprintEntryEditorUtility.IsValid(entry))
                return false;

            return TryAdd(registry, entry.pathKey, entry.transform);
        }

        public static bool Remove(PrefabFingerprintRegistry registry, PrefabHierarchyPathKey pathKey)
        {
            if (registry == null || pathKey == null)
                return false;

            var entries = CopyEntries(registry);
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i]?.pathKey != pathKey)
                    continue;

                entries.RemoveAt(i);
                registry.SetEntries(entries);
                return true;
            }

            return false;
        }

        public static void Clear(PrefabFingerprintRegistry registry)
        {
            registry?.SetEntries(null);
        }

        public static bool RebuildFromRoot(PrefabFingerprintRegistry registry, Transform root)
        {
            if (registry == null || root == null)
                return false;

            var entries = new List<PrefabFingerprintEntry>();
            AddHierarchyEntries(root, root, entries);
            registry.SetEntries(entries);
            return true;
        }

        public static bool TryGetRegisteredGameObject(PrefabFingerprintRegistry registry, GameObject productPrefab, GameObject gameObject, out GameObject registeredGameObject, out bool isProductRoot)
        {
            registeredGameObject = null;
            isProductRoot = false;

            if (!TryGetRegisteredPathKey(registry, productPrefab, gameObject, out var pathKey))
                return false;

            isProductRoot = pathKey.isEmpty;
            if (!registry.TryGetTransform(pathKey, out var registeredTransform))
                return false;

            registeredGameObject = registeredTransform.gameObject;
            return registeredGameObject != null;
        }

        public static bool TryGetRegisteredPathKey(PrefabFingerprintRegistry registry, GameObject productPrefab, GameObject gameObject, out PrefabHierarchyPathKey pathKey)
        {
            pathKey = null;

            if (registry == null || productPrefab == null || gameObject == null)
                return false;

            if (EditorUtility.IsPersistent(gameObject)
                && registry.TryGetPathKey(gameObject.transform, out pathKey))
            {
                return true;
            }

            if (!EditorUtility.IsPersistent(gameObject)
                && PrefabUtility.GetCorrespondingObjectFromSource(gameObject) != null
                && TryGetProductPrefabInstanceRoot(gameObject, productPrefab, out var instanceRoot))
            {
                return PrefabHierarchyPathKeyEditorUtility.TryCreate(gameObject.transform, instanceRoot, out pathKey);
            }

            return false;
        }

        public static bool HasUniqueEntries(PrefabFingerprintRegistry registry)
        {
            if (registry == null)
                return false;

            var entries = registry.entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!PrefabFingerprintEntryEditorUtility.IsValid(entry))
                    return false;

                for (var j = i + 1; j < entries.Count; j++)
                {
                    var other = entries[j];
                    if (other == null)
                        return false;

                    if (entry.pathKey == other.pathKey || entry.transform == other.transform)
                        return false;
                }
            }

            return true;
        }

        static void AddHierarchyEntries(Transform transform, Transform root, List<PrefabFingerprintEntry> entries)
        {
            if (PrefabHierarchyPathKeyEditorUtility.TryCreate(transform, root, out var pathKey))
                entries.Add(new PrefabFingerprintEntry(pathKey, transform));

            for (var i = 0; i < transform.childCount; i++)
                AddHierarchyEntries(transform.GetChild(i), root, entries);
        }

        static bool TryGetProductPrefabInstanceRoot(GameObject gameObject, GameObject productPrefab, out Transform instanceRoot)
        {
            instanceRoot = null;

            for (var current = gameObject != null ? gameObject.transform : null; current != null; current = current.parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject) == productPrefab)
                {
                    instanceRoot = current;
                    return true;
                }
            }

            return false;
        }

        static List<PrefabFingerprintEntry> CopyEntries(PrefabFingerprintRegistry registry)
        {
            var entries = new List<PrefabFingerprintEntry>();

            if (registry == null)
                return entries;

            foreach (var entry in registry.entries)
            {
                if (entry != null)
                    entries.Add(entry);
            }

            return entries;
        }
    }
}
