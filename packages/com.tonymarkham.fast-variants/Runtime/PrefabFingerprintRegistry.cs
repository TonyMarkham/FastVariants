using System;
using System.Collections.Generic;
using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    [Serializable]
    public sealed class PrefabFingerprintRegistry
    {
        [SerializeField] List<PrefabFingerprintEntry> m_Entries = new();

        [NonSerialized] Dictionary<PrefabHierarchyPathKey, Transform> m_TransformByPathKey;
        [NonSerialized] Dictionary<Transform, PrefabHierarchyPathKey> m_PathKeyByTransform;
        [NonSerialized] bool m_IndexDirty;

        public IReadOnlyList<PrefabFingerprintEntry> entries => m_Entries ??= new List<PrefabFingerprintEntry>();
        public int count => m_Entries?.Count ?? 0;

        public void SetEntries(IEnumerable<PrefabFingerprintEntry> entries)
        {
            m_Entries = entries != null
                ? new List<PrefabFingerprintEntry>(entries)
                : new List<PrefabFingerprintEntry>();

            m_IndexDirty = true;
        }

        public bool TryGetTransform(PrefabHierarchyPathKey pathKey, out Transform transform)
        {
            transform = null;

            if (pathKey == null)
                return false;

            EnsureLookupTables();
            return m_TransformByPathKey.TryGetValue(pathKey, out transform) && transform != null;
        }

        public bool TryGetPathKey(Transform transform, out PrefabHierarchyPathKey pathKey)
        {
            pathKey = null;

            if (transform == null)
                return false;

            EnsureLookupTables();
            return m_PathKeyByTransform.TryGetValue(transform, out pathKey) && pathKey != null;
        }

        public bool ContainsPathKey(PrefabHierarchyPathKey pathKey)
        {
            if (pathKey == null)
                return false;

            EnsureLookupTables();
            return m_TransformByPathKey.ContainsKey(pathKey);
        }

        public bool ContainsTransform(Transform transform)
        {
            if (transform == null)
                return false;

            EnsureLookupTables();
            return m_PathKeyByTransform.ContainsKey(transform);
        }

        void EnsureLookupTables()
        {
            if (!m_IndexDirty && m_TransformByPathKey != null && m_PathKeyByTransform != null)
                return;

            m_TransformByPathKey = new Dictionary<PrefabHierarchyPathKey, Transform>();
            m_PathKeyByTransform = new Dictionary<Transform, PrefabHierarchyPathKey>();

            if (m_Entries == null)
            {
                m_IndexDirty = false;
                return;
            }

            foreach (var entry in m_Entries)
            {
                if (entry?.pathKey == null || entry.transform == null)
                    continue;

                if (m_TransformByPathKey.ContainsKey(entry.pathKey)
                    || m_PathKeyByTransform.ContainsKey(entry.transform))
                {
                    continue;
                }

                m_TransformByPathKey.Add(entry.pathKey, entry.transform);
                m_PathKeyByTransform.Add(entry.transform, entry.pathKey);
            }

            m_IndexDirty = false;
        }
    }
}
