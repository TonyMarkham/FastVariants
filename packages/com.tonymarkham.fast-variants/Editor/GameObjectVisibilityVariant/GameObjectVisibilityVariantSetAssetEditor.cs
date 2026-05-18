using FastVariants.GameObjectVisibilityVariant;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastVariants.Editor
{
    [CustomEditor(typeof(GameObjectVisibilityVariantSetAsset))]
    public class GameObjectVisibilityVariantSetAssetEditor : UnityEditor.Editor
    {
        [SerializeField] VisualTreeAsset m_VisualTreeAsset;
        [SerializeField] StyleSheet m_Stylesheet;

        const string AddVariantButtonName = "add-variant-button";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            if (m_Stylesheet != null)
                root.styleSheets.Add(m_Stylesheet);

            if (m_VisualTreeAsset == null)
            {
                root.Add(new Label("Missing inspector VisualTreeAsset reference."));
                return root;
            }

            m_VisualTreeAsset.CloneTree(root);

            root.Q<PropertyField>("variant-set")?.SetEnabled(false);
            root.Q<PropertyField>("protobuf")?.SetEnabled(false);

            var addVariantButton = root.Q<Button>(AddVariantButtonName);
            if (addVariantButton != null)
                addVariantButton.clicked += AddGameObjectVisibilityVariant;

            return root;
        }

        void AddGameObjectVisibilityVariant()
        {
            serializedObject.ApplyModifiedProperties();

            var variantSetAsset = (GameObjectVisibilityVariantSetAsset)target;
            variantSetAsset.AddGameObjectVisibilityVariant();

            serializedObject.Update();
        }
    }
}
