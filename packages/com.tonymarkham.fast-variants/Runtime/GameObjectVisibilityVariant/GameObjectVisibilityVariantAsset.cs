using System;
using System.Collections.Generic;
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
        [SerializeField, HideInInspector] GameObject m_ItemGameObject;
        [SerializeField] List<GameObject> m_ItemGameObjects = new();
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
            EnsureItemGameObjects();

            if (m_GameObjectVisibilityVariant is null
                || m_GameObjectVisibilityVariant.id.IsEmptyId())
            {
                m_GameObjectVisibilityVariant = new GameObjectVisibilityVariant(m_ItemName, m_ItemCode, m_ItemGameObjects);
            }
        }

        protected override void SyncFeature()
        {
            EnsureItemGameObjects();
            SyncFeatureIdentity(m_GameObjectVisibilityVariant);

            m_GameObjectVisibilityVariant.Set(m_ItemGameObjects);

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
            var proto = new VisibilityVariantProto
            {
                Id = m_GameObjectVisibilityVariant?.id ?? string.Empty,
                Name = m_ItemName ?? string.Empty,
                Code = m_ItemCode ?? string.Empty,
                TargetKind = VisibilityTargetKindProto.Entity
            };

            if (m_ItemGameObjects == null)
                return proto;

            foreach (var gameObject in m_ItemGameObjects)
            {
                var entityId = GetEntityId(gameObject);
                if (string.IsNullOrEmpty(entityId))
                    continue;

                proto.TargetIds.Add(entityId);
            }

            if (proto.TargetIds.Count > 0)
                proto.TargetId = proto.TargetIds[0];

            return proto;
        }

        void EnsureItemGameObjects()
        {
            m_ItemGameObjects ??= new List<GameObject>();

            if (m_ItemGameObject != null && !m_ItemGameObjects.Contains(m_ItemGameObject))
            {
                m_ItemGameObjects.Add(m_ItemGameObject);
                m_ItemGameObject = null;
            }
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
