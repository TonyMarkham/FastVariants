using System;
using System.Collections.Generic;
using FastVariants.Core.Feature;
using FastVariants.Core.Utilities;
using UnityEngine;

namespace FastVariants.GameObjectVisibilityVariant
{
    [Serializable]
    public class GameObjectVisibilityVariant : Feature<IReadOnlyList<GameObject>>
    {
        [SerializeField, HideInInspector] GameObject m_GameObject;
        [SerializeField] List<GameObject> m_GameObjects = new();

        public override IReadOnlyList<GameObject> value
        {
            get
            {
                EnsureGameObjects();
                return m_GameObjects;
            }
        }

        public GameObjectVisibilityVariant()
        {
        }

        public GameObjectVisibilityVariant(string name, string code, GameObject gameObject) : base(name, code)
        {
            Set(gameObject);
        }

        public GameObjectVisibilityVariant(string name, string code, IEnumerable<GameObject> gameObjects) : base(name, code)
        {
            Set(gameObjects);
        }

        public void Set(GameObject gameObject)
        {
            Set(gameObject != null ? new[] { gameObject } : null);
        }

        public void Set(IEnumerable<GameObject> gameObjects)
        {
            EnsureGameObjects();
            m_GameObjects.Clear();

            if (gameObjects != null)
            {
                foreach (var gameObject in gameObjects)
                {
                    if (gameObject != null && !m_GameObjects.Contains(gameObject))
                        m_GameObjects.Add(gameObject);
                }
            }

            m_GameObject = null;
        }

        public override bool IsValid()
        {
            if (string.IsNullOrEmpty(name)
                || string.IsNullOrEmpty(code)
                || featureSetId.IsEmptyId()
                || value.Count == 0)
            {
                return false;
            }

            foreach (var gameObject in value)
            {
                if (gameObject == null || !gameObject.GetEntityId().IsValid())
                    return false;
            }

            return true;
        }

        void EnsureGameObjects()
        {
            m_GameObjects ??= new List<GameObject>();

            if (m_GameObject != null && !m_GameObjects.Contains(m_GameObject))
            {
                m_GameObjects.Add(m_GameObject);
                m_GameObject = null;
            }
        }
    }
}
