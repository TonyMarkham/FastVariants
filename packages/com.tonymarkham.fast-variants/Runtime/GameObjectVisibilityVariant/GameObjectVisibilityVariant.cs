using System;
using FastVariants.Core.Feature;
using FastVariants.Core.Utilities;
using UnityEngine;

namespace FastVariants.GameObjectVisibilityVariant
{
    [Serializable]
    public class GameObjectVisibilityVariant : Feature<GameObject>
    {
        [SerializeField] GameObject m_GameObject;

        public override GameObject value => m_GameObject;

        public GameObjectVisibilityVariant()
        {
        }

        public GameObjectVisibilityVariant(string name, string code, GameObject gameObject) : base(name, code)
        {
            Set(gameObject);
        }

        public void Set(GameObject gameObject)
        {
            TrySetProperty(ref m_GameObject, gameObject);
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(name)
                   && !string.IsNullOrEmpty(code)
                   && !featureSetId.IsEmptyId()
                   && value != null
                   && value.GetEntityId().IsValid();
        }
    }
}
