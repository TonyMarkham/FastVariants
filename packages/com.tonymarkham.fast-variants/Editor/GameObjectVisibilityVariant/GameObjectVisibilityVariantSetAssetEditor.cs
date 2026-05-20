using System.Collections.Generic;
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
        const string VariantsPropertyName = "m_Variants";
        const string DefaultVariantPropertyName = "m_DefaultVariant";

        readonly List<GameObjectVisibilityVariantAsset> m_TrackedVariants = new();

        bool m_ValidatingDefaultVariant;

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

            TrackRemovedEmbeddedVariants(root);
            TrackDefaultVariant(root);
            EnsureDefaultVariantSelection();

            return root;
        }

        void AddGameObjectVisibilityVariant()
        {
            serializedObject.ApplyModifiedProperties();

            var variantSetAsset = (GameObjectVisibilityVariantSetAsset)target;
            variantSetAsset.AddGameObjectVisibilityVariant();

            serializedObject.Update();
            CacheVariantReferences();
            EnsureDefaultVariantSelection();
        }

        void TrackDefaultVariant(VisualElement root)
        {
            var defaultVariantProperty = serializedObject.FindProperty(DefaultVariantPropertyName);
            if (defaultVariantProperty == null)
                return;

            root.TrackPropertyValue(defaultVariantProperty, ValidateDefaultVariantSelection);
        }

        void ValidateDefaultVariantSelection(SerializedProperty defaultVariantProperty)
        {
            if (m_ValidatingDefaultVariant)
                return;

            var variants = GetVariantReferences();
            var defaultVariant = defaultVariantProperty.objectReferenceValue as GameObjectVisibilityVariantAsset;
            if (defaultVariant != null && variants.Contains(defaultVariant))
                return;

            m_ValidatingDefaultVariant = true;

            Undo.RecordObject(serializedObject.targetObject, "Set Default Variant");
            defaultVariantProperty.objectReferenceValue = GetFirstVariant(variants);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);

            m_ValidatingDefaultVariant = false;
        }

        void EnsureDefaultVariantSelection()
        {
            if (m_ValidatingDefaultVariant)
                return;

            var defaultVariantProperty = serializedObject.FindProperty(DefaultVariantPropertyName);
            if (defaultVariantProperty == null)
                return;

            var variants = GetVariantReferences();
            var defaultVariant = defaultVariantProperty.objectReferenceValue as GameObjectVisibilityVariantAsset;
            if (defaultVariant != null && variants.Contains(defaultVariant))
                return;

            var fallbackVariant = GetFirstVariant(variants);
            if (defaultVariant == fallbackVariant)
                return;

            m_ValidatingDefaultVariant = true;

            Undo.RecordObject(serializedObject.targetObject, "Set Default Variant");
            defaultVariantProperty.objectReferenceValue = fallbackVariant;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);

            m_ValidatingDefaultVariant = false;
        }

        void TrackRemovedEmbeddedVariants(VisualElement root)
        {
            var variantsProperty = serializedObject.FindProperty(VariantsPropertyName);
            if (variantsProperty == null || !variantsProperty.isArray)
                return;

            CacheVariantReferences(variantsProperty);
            root.TrackPropertyValue(variantsProperty, RemoveDeletedEmbeddedVariants);
        }

        void RemoveDeletedEmbeddedVariants(SerializedProperty variantsProperty)
        {
            var currentVariants = GetVariantReferences(variantsProperty);
            var removedVariants = new List<GameObjectVisibilityVariantAsset>();

            foreach (var trackedVariant in m_TrackedVariants)
            {
                if (trackedVariant == null
                    || currentVariants.Contains(trackedVariant)
                    || removedVariants.Contains(trackedVariant))
                {
                    continue;
                }

                removedVariants.Add(trackedVariant);
            }

            m_TrackedVariants.Clear();
            m_TrackedVariants.AddRange(currentVariants);
            EnsureDefaultVariantSelection();

            if (removedVariants.Count == 0)
                return;

            var variantSetAsset = (GameObjectVisibilityVariantSetAsset)target;
            var assetPath = AssetDatabase.GetAssetPath(variantSetAsset);
            var removedEmbeddedAsset = false;

            foreach (var removedVariant in removedVariants)
            {
                if (!IsEmbeddedAssetAtPath(removedVariant, assetPath))
                    continue;

                Undo.DestroyObjectImmediate(removedVariant);
                removedEmbeddedAsset = true;
            }

            if (!removedEmbeddedAsset)
                return;

            EditorUtility.SetDirty(variantSetAsset);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.ImportAsset(assetPath);

            serializedObject.Update();
            CacheVariantReferences();
            EnsureDefaultVariantSelection();
        }

        void CacheVariantReferences()
        {
            var variantsProperty = serializedObject.FindProperty(VariantsPropertyName);
            CacheVariantReferences(variantsProperty);
        }

        void CacheVariantReferences(SerializedProperty variantsProperty)
        {
            m_TrackedVariants.Clear();

            if (variantsProperty == null || !variantsProperty.isArray)
                return;

            m_TrackedVariants.AddRange(GetVariantReferences(variantsProperty));
        }

        List<GameObjectVisibilityVariantAsset> GetVariantReferences()
        {
            var variantsProperty = serializedObject.FindProperty(VariantsPropertyName);
            return GetVariantReferences(variantsProperty);
        }

        static List<GameObjectVisibilityVariantAsset> GetVariantReferences(SerializedProperty variantsProperty)
        {
            var variants = new List<GameObjectVisibilityVariantAsset>();

            if (variantsProperty == null || !variantsProperty.isArray)
                return variants;

            for (var i = 0; i < variantsProperty.arraySize; i++)
            {
                variants.Add(variantsProperty.GetArrayElementAtIndex(i).objectReferenceValue as GameObjectVisibilityVariantAsset);
            }

            return variants;
        }

        static GameObjectVisibilityVariantAsset GetFirstVariant(List<GameObjectVisibilityVariantAsset> variants)
        {
            if (variants == null)
                return null;

            foreach (var variant in variants)
            {
                if (variant != null)
                    return variant;
            }

            return null;
        }

        static bool IsEmbeddedAssetAtPath(Object asset, string assetPath)
        {
            return asset != null
                   && !string.IsNullOrEmpty(assetPath)
                   && AssetDatabase.IsSubAsset(asset)
                   && AssetDatabase.GetAssetPath(asset) == assetPath;
        }
    }
}
