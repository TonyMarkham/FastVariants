using System;
using System.Collections.Generic;
using FastVariants.Abstract;
using Google.Protobuf;
using FastVariants.Core.Utilities;
using UnityEngine;
using VisibilityVariantSetProto = FastVariants.Proto.VisibilityVariantSet;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastVariants.GameObjectVisibilityVariant
{
    [Serializable]
    [CreateAssetMenu(fileName = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_SET_ASSET_FILE, menuName = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_SET_ASSET_MENU, order = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_SET_ASSET_ORDER)]
    public class GameObjectVisibilityVariantSetAsset : FeatureSetAsset
    {
        [SerializeField] GameObjectVisibilityVariantSet m_VariantSet;
        [SerializeField] List<GameObjectVisibilityVariantAsset> m_Variants;

        public GameObjectVisibilityVariantSet variantSet
        {
            get
            {
                Sync();
                return m_VariantSet;
            }
        }

        internal GameObjectVisibilityVariantSet currentVariantSet => m_VariantSet;

        public VisibilityVariantSetProto ToProto()
        {
            Sync();
            return BuildProto();
        }

#if UNITY_EDITOR
        [ContextMenu("Add GameObject Visibility Variant")]
        public void AddGameObjectVisibilityVariant()
        {
            var assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Save the GameObject Visibility Variant Set as an asset before adding embedded variants.", this);
                return;
            }

            var variantAsset = CreateInstance<GameObjectVisibilityVariantAsset>();
            variantAsset.name = ObjectNames.GetUniqueName(
                GetEmbeddedAssetNames(assetPath),
                MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_ASSET_FILE);
            variantAsset.SetVariantSet(this);

            AssetDatabase.AddObjectToAsset(variantAsset, this);

            m_Variants ??= new List<GameObjectVisibilityVariantAsset>();
            m_Variants.Add(variantAsset);

            EditorUtility.SetDirty(variantAsset);
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
#endif

        protected override void EnsureFeatureSet()
        {
            if (m_VariantSet is null
                || m_VariantSet.id.IsEmptyId())
            {
                m_VariantSet = new GameObjectVisibilityVariantSet(m_ItemName, m_ItemCode);
            }
        }

        protected override void SyncFeatureSet()
        {
            SyncFeatureSetIdentity(m_VariantSet);

            m_VariantSet.ClearFeatures();

            if (m_Variants is not null)
            {
                foreach (var variantAsset in m_Variants)
                {
                    if (variantAsset is null || variantAsset.variant is null)
                        continue;

                    m_VariantSet.TryAddFeature(variantAsset.variant);
                }
            }
        }

        protected override IMessage BuildProtoMessage()
        {
            return BuildProto();
        }

        VisibilityVariantSetProto BuildProto()
        {
            var proto = new VisibilityVariantSetProto
            {
                Id = m_VariantSet?.id ?? string.Empty,
                Name = m_ItemName ?? string.Empty,
                Code = m_ItemCode ?? string.Empty,
                ActiveVariantId = m_VariantSet?.activeFeature?.id ?? string.Empty
            };

            if (m_Variants is null)
                return proto;

            foreach (var variantAsset in m_Variants)
            {
                if (variantAsset is null)
                    continue;

                proto.Variants.Add(variantAsset.ToProto());
            }

            return proto;
        }
    }
}
