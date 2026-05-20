using System;
using UnityEngine;

namespace FastVariants.PrefabFingerprint
{
    [Serializable]
    public sealed class PrefabFingerprintEntry
    {
        [SerializeField] PrefabHierarchyPathKey m_PathKey;
        [SerializeField] Transform m_Transform;

        public PrefabHierarchyPathKey pathKey => m_PathKey;
        public Transform transform => m_Transform;

        public PrefabFingerprintEntry()
        {
        }

        public PrefabFingerprintEntry(PrefabHierarchyPathKey pathKey, Transform transform)
        {
            m_PathKey = pathKey;
            m_Transform = transform;
        }
    }
}
