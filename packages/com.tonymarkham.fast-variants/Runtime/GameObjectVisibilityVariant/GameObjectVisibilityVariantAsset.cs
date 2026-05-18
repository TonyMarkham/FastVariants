using System;
using FastVariants.Abstract;
using Google.Protobuf;
using FastVariants.Core.Utilities;
using UnityEngine;
using VisibilityTargetKindProto = FastVariants.Proto.VisibilityTargetKind;
using VisibilityVariantProto = FastVariants.Proto.VisibilityVariant;

namespace FastVariants.GameObjectVisibilityVariant
{
    [Serializable]
    [CreateAssetMenu(fileName = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_ASSET_FILE, menuName = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_ASSET_MENU, order = MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_ASSET_ORDER)]
    public class GameObjectVisibilityVariantAsset : FeatureAsset
    {
        [SerializeField] GameObject m_ItemGameObject;
        [SerializeField] GameObjectVisibilityVariant m_GameObjectVisibilityVariant;
        [SerializeField] GameObjectVisibilityVariantSetAsset m_GameObjectVisibilityVariantSet;

        public GameObjectVisibilityVariant variant
        {
            get
            {
                Sync();
                return m_GameObjectVisibilityVariant;
            }
        }

        public VisibilityVariantProto ToProto()
        {
            Sync();
            return BuildProto();
        }

        internal void SetVariantSet(GameObjectVisibilityVariantSetAsset variantSet)
        {
            m_GameObjectVisibilityVariantSet = variantSet;
        }

        protected override void EnsureFeature()
        {
            if (m_GameObjectVisibilityVariant is null
                || m_GameObjectVisibilityVariant.id.IsEmptyId())
            {
                m_GameObjectVisibilityVariant = new GameObjectVisibilityVariant(m_ItemName, m_ItemCode, m_ItemGameObject);
            }
        }

        protected override void SyncFeature()
        {
            SyncFeatureIdentity(m_GameObjectVisibilityVariant);

            if (m_GameObjectVisibilityVariant.value != m_ItemGameObject)
            {
                m_GameObjectVisibilityVariant.Set(m_ItemGameObject);
            }

            var featureSetId = m_GameObjectVisibilityVariantSet?.currentVariantSet?.id ?? GuidUtilities.EmptyId;
            m_GameObjectVisibilityVariant.SetFeatureSetId(featureSetId);
        }

        protected override bool IsFeatureValid()
        {
            return m_GameObjectVisibilityVariant?.IsValid() ?? false;
        }

        protected override IMessage BuildProtoMessage()
        {
            return BuildProto();
        }

        VisibilityVariantProto BuildProto()
        {
            return new VisibilityVariantProto
            {
                Id = m_GameObjectVisibilityVariant?.id ?? string.Empty,
                Name = m_ItemName ?? string.Empty,
                Code = m_ItemCode ?? string.Empty,
                TargetId = GetEntityId(m_ItemGameObject),
                TargetKind = VisibilityTargetKindProto.Entity
            };
        }

        static string GetEntityId(GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            var entityId = gameObject.GetEntityId();
            return entityId.IsValid() ? entityId.ToString() : string.Empty;
        }
    }
}
