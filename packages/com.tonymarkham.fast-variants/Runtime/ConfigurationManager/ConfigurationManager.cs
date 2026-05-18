using System;
using System.Collections.Generic;
using FastVariants.Abstract;
using FastVariants.GameObjectVisibilityVariant;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastVariants.ConfigurationManager
{
    [Serializable]
    [CreateAssetMenu(fileName = MenuManager.CONFIGURATION_MANAGER_ASSET_FILE, menuName = MenuManager.CONFIGURATION_MANAGER_ASSET_MENU, order = MenuManager.CONFIGURATION_MANAGER_ASSET_ORDER)]
    public class ConfigurationManager : ScriptableObject
    {
        [SerializeField] string m_ItemName;
        [SerializeField] GameObject m_Product;
        [SerializeField] List<FeatureSetAsset> m_FeatureSets;
        
#if UNITY_EDITOR
        [ContextMenu("Add GameObject Visibility Variant Set")]
        public void AddGameObjectVisibilityVariantSet()
        {
            var assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Save the Configuration Manager as an asset before adding embedded variant sets.", this);
                return;
            }

            var variantSetAsset = CreateInstance<GameObjectVisibilityVariantSetAsset>();
            variantSetAsset.name = ObjectNames.GetUniqueName(
                GetEmbeddedAssetNames(assetPath),
                MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_SET_ASSET_FILE);

            AssetDatabase.AddObjectToAsset(variantSetAsset, this);

            m_FeatureSets ??= new List<FeatureSetAsset>();
            m_FeatureSets.Add(variantSetAsset);

            EditorUtility.SetDirty(variantSetAsset);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
        }
        
        static string[] GetEmbeddedAssetNames(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var names = new string[assets.Length];

            for (var i = 0; i < assets.Length; i++)
                names[i] = assets[i].name;

            return names;
        }
    }
#endif
}
