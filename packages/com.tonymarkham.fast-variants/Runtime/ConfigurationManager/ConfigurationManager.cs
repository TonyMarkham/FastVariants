using System;
using System.Collections.Generic;
using FastVariants.Abstract;
using FastVariants.PrefabFingerprint;
using UnityEngine;

namespace FastVariants.ConfigurationManager
{
    [Serializable]
    [CreateAssetMenu(fileName = MenuManager.CONFIGURATION_MANAGER_ASSET_FILE, menuName = MenuManager.CONFIGURATION_MANAGER_ASSET_MENU, order = MenuManager.CONFIGURATION_MANAGER_ASSET_ORDER)]
    public class ConfigurationManager : ScriptableObject
    {
        [SerializeField] string m_ItemName;
        [SerializeField] GameObject m_Product;
        [SerializeField] List<FeatureSetAsset> m_FeatureSets;
        [SerializeField] PrefabFingerprintRegistry m_PrefabFingerprint = new();

        public PrefabFingerprintRegistry prefabFingerprint => m_PrefabFingerprint ??= new PrefabFingerprintRegistry();

        public bool TryGetPrefabTransform(PrefabHierarchyPathKey pathKey, out Transform transform)
        {
            return prefabFingerprint.TryGetTransform(pathKey, out transform);
        }

        public bool TryGetPrefabPathKey(Transform transform, out PrefabHierarchyPathKey pathKey)
        {
            return prefabFingerprint.TryGetPathKey(transform, out pathKey);
        }

        public bool TryGetPrefabPathKey(GameObject gameObject, out PrefabHierarchyPathKey pathKey)
        {
            return TryGetPrefabPathKey(gameObject != null ? gameObject.transform : null, out pathKey);
        }
    }
}
