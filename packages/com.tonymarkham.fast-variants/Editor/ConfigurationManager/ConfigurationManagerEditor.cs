using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ConfigurationManagerAsset = FastVariants.ConfigurationManager.ConfigurationManager;

namespace FastVariants.Editor
{
    [CustomEditor(typeof(ConfigurationManagerAsset))]
    public class ConfigurationManagerEditor : UnityEditor.Editor
    {
        [SerializeField] VisualTreeAsset m_VisualTreeAsset;
        [SerializeField] StyleSheet m_Stylesheet;

        const string AddVariantSetButtonName = "add-visibility-variant-set-button";

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

            SetupAddVariantSetButton(root);

            return root;
        }

        void SetupAddVariantSetButton(VisualElement root)
        {
            var addVariantSetButton = root.Q<Button>(AddVariantSetButtonName);
            if (addVariantSetButton != null)
                addVariantSetButton.clicked += AddGameObjectVisibilityVariantSet;
        }

        void AddGameObjectVisibilityVariantSet()
        {
            serializedObject.ApplyModifiedProperties();

            var configurationManager = (ConfigurationManagerAsset)target;
            configurationManager.AddGameObjectVisibilityVariantSet();

            serializedObject.Update();
        }
    }
}
