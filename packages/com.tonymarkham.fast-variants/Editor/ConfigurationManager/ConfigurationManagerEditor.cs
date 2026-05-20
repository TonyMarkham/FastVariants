using System.Collections.Generic;
using FastVariants.Abstract;
using FastVariants.GameObjectVisibilityVariant;
using FastVariants.PrefabFingerprint;
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
        const string FingerprintDictionaryName = "prefab-fingerprint-dictionary";
        const string ProductDropZoneName = "product-prefab-drop-zone";
        const string ProductDropZoneStatusName = "product-drop-zone-status";
        const string ProductPropertyName = "m_Product";
        const string FeatureSetsPropertyName = "m_FeatureSets";
        const string VariantsPropertyName = "m_Variants";
        const string DefaultProductDropZoneStatus = "Drop one Project prefab asset, or one prefab instance from the Hierarchy.";

        readonly List<FeatureSetAsset> m_TrackedFeatureSets = new();

        VisualElement m_FingerprintDictionaryElement;
        Label m_ProductDropZoneStatusLabel;
        bool m_UpdatingProductPrefab;

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

            SetupGeneratedData(root);
            SetupProductDropZone(root);
            TrackProductPrefabChanges(root);
            SetupAddVariantSetButton(root);
            TrackRemovedEmbeddedFeatureSets(root);

            return root;
        }

        void SetupAddVariantSetButton(VisualElement root)
        {
            var addVariantSetButton = root.Q<Button>(AddVariantSetButtonName);
            if (addVariantSetButton != null)
                addVariantSetButton.clicked += AddGameObjectVisibilityVariantSet;
        }

        void SetupGeneratedData(VisualElement root)
        {
            m_FingerprintDictionaryElement = root.Q<VisualElement>(FingerprintDictionaryName);
            RefreshFingerprintDictionaryView();
        }

        void SetupProductDropZone(VisualElement root)
        {
            var dropZone = root.Q<VisualElement>(ProductDropZoneName);
            if (dropZone == null)
                return;

            m_ProductDropZoneStatusLabel = root.Q<Label>(ProductDropZoneStatusName);
            RefreshProductDropZoneStatus();

            dropZone.RegisterCallback<DragEnterEvent>(_ => dropZone.AddToClassList("fv-drop-zone--active"));
            dropZone.RegisterCallback<DragLeaveEvent>(_ => ClearDropZoneState(dropZone));
            dropZone.RegisterCallback<DragExitedEvent>(_ => ClearDropZoneState(dropZone));

            dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                var canRegister = TryGetDraggedProductPrefab(out _, out var statusText);

                DragAndDrop.visualMode = canRegister ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                dropZone.EnableInClassList("fv-drop-zone--active", canRegister);
                dropZone.EnableInClassList("fv-drop-zone--rejected", !canRegister);
                SetProductDropZoneStatus(statusText);
                evt.StopPropagation();
            });

            dropZone.RegisterCallback<DragPerformEvent>(evt =>
            {
                var canRegister = TryGetDraggedProductPrefab(out var productPrefab, out var statusText);
                ClearDropZoneState(dropZone);

                if (!canRegister)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    SetProductDropZoneStatus(statusText);
                    evt.StopPropagation();
                    return;
                }

                DragAndDrop.AcceptDrag();
                SetProductPrefab(productPrefab);
                SetProductDropZoneStatus($"Product prefab registered: {productPrefab.name}");
                evt.StopPropagation();
            });
        }

        void RefreshFingerprintDictionaryView()
        {
            if (m_FingerprintDictionaryElement == null)
                return;

            m_FingerprintDictionaryElement.Clear();

            var configurationManager = (ConfigurationManagerAsset)target;
            var entries = configurationManager.prefabFingerprint.entries;
            if (entries.Count == 0)
            {
                var emptyLabel = new Label("No prefab fingerprint entries. Assign a Product prefab to generate them.");
                emptyLabel.AddToClassList("fv-fingerprint-empty");
                m_FingerprintDictionaryElement.Add(emptyLabel);
                return;
            }

            m_FingerprintDictionaryElement.Add(CreateFingerprintHeader());

            foreach (var entry in entries)
                m_FingerprintDictionaryElement.Add(CreateFingerprintRow(entry));
        }

        static VisualElement CreateFingerprintHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("fv-fingerprint-header");

            var keyLabel = new Label("Fingerprint Key");
            keyLabel.AddToClassList("fv-fingerprint-key");
            keyLabel.AddToClassList("fv-fingerprint-header-label");

            var objectLabel = new Label("GameObject");
            objectLabel.AddToClassList("fv-fingerprint-object");
            objectLabel.AddToClassList("fv-fingerprint-header-label");

            header.Add(keyLabel);
            header.Add(objectLabel);
            return header;
        }

        static VisualElement CreateFingerprintRow(PrefabFingerprintEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("fv-fingerprint-row");

            var keyLabel = new Label(GetFingerprintKeyLabel(entry));
            keyLabel.AddToClassList("fv-fingerprint-key");

            var gameObject = entry?.transform != null ? entry.transform.gameObject : null;
            var objectField = new ObjectField
            {
                objectType = typeof(GameObject),
                value = gameObject
            };
            objectField.AddToClassList("fv-fingerprint-object");
            objectField.SetEnabled(false);

            row.Add(keyLabel);
            row.Add(objectField);
            return row;
        }

        static string GetFingerprintKeyLabel(PrefabFingerprintEntry entry)
        {
            if (entry?.pathKey == null)
                return "(missing key)";

            return entry.pathKey.isEmpty ? "(root)" : entry.pathKey.value;
        }

        void TrackProductPrefabChanges(VisualElement root)
        {
            var productProperty = serializedObject.FindProperty(ProductPropertyName);
            if (productProperty == null)
                return;

            root.TrackPropertyValue(productProperty, ValidateProductPrefab);
        }

        void ValidateProductPrefab(SerializedProperty productProperty)
        {
            if (m_UpdatingProductPrefab)
                return;

            serializedObject.ApplyModifiedProperties();

            productProperty = serializedObject.FindProperty(ProductPropertyName);
            var product = productProperty?.objectReferenceValue as GameObject;
            if (product == null)
            {
                Undo.RecordObject(serializedObject.targetObject, "Clear Product Prefab Fingerprint");
                RebuildProductFingerprint(null);
                SetProductDropZoneStatus(DefaultProductDropZoneStatus);
                return;
            }

            if (!TryResolveProjectPrefab(product, out var productPrefab))
            {
                m_UpdatingProductPrefab = true;

                Undo.RecordObject(serializedObject.targetObject, "Clear Invalid Product Prefab");
                serializedObject.Update();
                productProperty = serializedObject.FindProperty(ProductPropertyName);
                productProperty.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();

                m_UpdatingProductPrefab = false;

                RebuildProductFingerprint(null);
                SetProductDropZoneStatus("Product cleared. Drop a Project prefab asset, or a prefab instance from the Hierarchy.");
                return;
            }

            if (productPrefab != product)
                SetProductPrefab(productPrefab);
            else
                RebuildProductFingerprint(productPrefab);

            SetProductDropZoneStatus($"Product prefab registered: {productPrefab.name}");
        }

        void SetProductPrefab(GameObject productPrefab)
        {
            m_UpdatingProductPrefab = true;

            Undo.RecordObject(serializedObject.targetObject, "Set Product Prefab");
            serializedObject.Update();

            var productProperty = serializedObject.FindProperty(ProductPropertyName);
            productProperty.objectReferenceValue = productPrefab;
            serializedObject.ApplyModifiedProperties();

            RebuildProductFingerprint(productPrefab);

            m_UpdatingProductPrefab = false;
        }

        void RebuildProductFingerprint(GameObject productPrefab)
        {
            var configurationManager = (ConfigurationManagerAsset)target;

            Undo.RecordObject(configurationManager, "Rebuild Product Prefab Fingerprint");

            if (productPrefab != null)
                PrefabFingerprintRegistryEditorUtility.RebuildFromRoot(configurationManager.prefabFingerprint, productPrefab.transform);
            else
                PrefabFingerprintRegistryEditorUtility.Clear(configurationManager.prefabFingerprint);

            EditorUtility.SetDirty(configurationManager);
            serializedObject.Update();
            RefreshFingerprintDictionaryView();
        }

        bool TryGetDraggedProductPrefab(out GameObject productPrefab, out string statusText)
        {
            productPrefab = null;
            var productPrefabs = new List<GameObject>();

            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                if (!TryResolveProjectPrefab(objectReference, out var draggedProductPrefab)
                    || productPrefabs.Contains(draggedProductPrefab))
                {
                    continue;
                }

                productPrefabs.Add(draggedProductPrefab);
            }

            if (productPrefabs.Count > 1)
            {
                statusText = "Drop one Product prefab at a time.";
                return false;
            }

            if (productPrefabs.Count == 0)
            {
                statusText = "Drop one Project prefab asset, or one prefab instance from the Hierarchy.";
                return false;
            }

            productPrefab = productPrefabs[0];
            statusText = $"Ready to register Product prefab: {productPrefab.name}";
            return true;
        }

        static bool TryResolveProjectPrefab(Object objectReference, out GameObject productPrefab)
        {
            productPrefab = null;

            var gameObject = GetGameObject(objectReference);
            if (gameObject == null)
                return false;

            if (EditorUtility.IsPersistent(gameObject))
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(gameObject))
                    return false;

                productPrefab = gameObject.transform.root.gameObject;
                return productPrefab != null;
            }

            var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (instanceRoot == null)
                return false;

            productPrefab = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
            return productPrefab != null && EditorUtility.IsPersistent(productPrefab);
        }

        void RefreshProductDropZoneStatus()
        {
            var productProperty = serializedObject.FindProperty(ProductPropertyName);
            var product = productProperty?.objectReferenceValue as GameObject;
            SetProductDropZoneStatus(product != null
                ? $"Product prefab registered: {product.name}"
                : DefaultProductDropZoneStatus);
        }

        void SetProductDropZoneStatus(string text)
        {
            if (m_ProductDropZoneStatusLabel != null)
                m_ProductDropZoneStatusLabel.text = text;
        }

        void AddGameObjectVisibilityVariantSet()
        {
            serializedObject.ApplyModifiedProperties();

            var configurationManager = (ConfigurationManagerAsset)target;
            var assetPath = AssetDatabase.GetAssetPath(configurationManager);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Save the Configuration Manager as an asset before adding embedded variant sets.", configurationManager);
                return;
            }

            var variantSetAsset = CreateInstance<GameObjectVisibilityVariantSetAsset>();
            variantSetAsset.name = ObjectNames.GetUniqueName(
                GetEmbeddedAssetNames(assetPath),
                MenuManager.GAMEOBJECT_VISIBILITY_VARIANT_SET_ASSET_FILE);

            AssetDatabase.AddObjectToAsset(variantSetAsset, configurationManager);

            serializedObject.Update();
            var featureSetsProperty = serializedObject.FindProperty(FeatureSetsPropertyName);
            if (featureSetsProperty == null || !featureSetsProperty.isArray)
            {
                Debug.LogError("Could not find the Configuration Manager feature set list.", configurationManager);
                DestroyImmediate(variantSetAsset, true);
                return;
            }

            var newIndex = featureSetsProperty.arraySize;
            featureSetsProperty.InsertArrayElementAtIndex(newIndex);
            featureSetsProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = variantSetAsset;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(variantSetAsset);
            EditorUtility.SetDirty(configurationManager);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            serializedObject.Update();
            CacheFeatureSetReferences();
        }

        void TrackRemovedEmbeddedFeatureSets(VisualElement root)
        {
            var featureSetsProperty = serializedObject.FindProperty(FeatureSetsPropertyName);
            if (featureSetsProperty == null || !featureSetsProperty.isArray)
                return;

            CacheFeatureSetReferences(featureSetsProperty);
            root.TrackPropertyValue(featureSetsProperty, RemoveDeletedEmbeddedFeatureSets);
        }

        void RemoveDeletedEmbeddedFeatureSets(SerializedProperty featureSetsProperty)
        {
            var currentFeatureSets = GetFeatureSetReferences(featureSetsProperty);
            var removedFeatureSets = new List<FeatureSetAsset>();

            foreach (var trackedFeatureSet in m_TrackedFeatureSets)
            {
                if (trackedFeatureSet == null
                    || currentFeatureSets.Contains(trackedFeatureSet)
                    || removedFeatureSets.Contains(trackedFeatureSet))
                {
                    continue;
                }

                removedFeatureSets.Add(trackedFeatureSet);
            }

            m_TrackedFeatureSets.Clear();
            m_TrackedFeatureSets.AddRange(currentFeatureSets);

            if (removedFeatureSets.Count == 0)
                return;

            var configurationManager = (ConfigurationManagerAsset)target;
            var assetPath = AssetDatabase.GetAssetPath(configurationManager);
            var removedEmbeddedAsset = false;

            foreach (var removedFeatureSet in removedFeatureSets)
            {
                if (!IsEmbeddedAssetAtPath(removedFeatureSet, assetPath))
                    continue;

                RemoveEmbeddedFeatures(removedFeatureSet, assetPath);
                Undo.DestroyObjectImmediate(removedFeatureSet);
                removedEmbeddedAsset = true;
            }

            if (!removedEmbeddedAsset)
                return;

            EditorUtility.SetDirty(configurationManager);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.ImportAsset(assetPath);

            serializedObject.Update();
            CacheFeatureSetReferences();
        }

        static void RemoveEmbeddedFeatures(FeatureSetAsset featureSet, string assetPath)
        {
            if (featureSet is not GameObjectVisibilityVariantSetAsset visibilityVariantSet)
                return;

            var serializedFeatureSet = new SerializedObject(visibilityVariantSet);
            var variantsProperty = serializedFeatureSet.FindProperty(VariantsPropertyName);
            if (variantsProperty == null || !variantsProperty.isArray)
                return;

            for (var i = 0; i < variantsProperty.arraySize; i++)
            {
                var variant = variantsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (IsEmbeddedAssetAtPath(variant, assetPath))
                    Undo.DestroyObjectImmediate(variant);
            }
        }

        void CacheFeatureSetReferences()
        {
            var featureSetsProperty = serializedObject.FindProperty(FeatureSetsPropertyName);
            CacheFeatureSetReferences(featureSetsProperty);
        }

        void CacheFeatureSetReferences(SerializedProperty featureSetsProperty)
        {
            m_TrackedFeatureSets.Clear();

            if (featureSetsProperty == null || !featureSetsProperty.isArray)
                return;

            m_TrackedFeatureSets.AddRange(GetFeatureSetReferences(featureSetsProperty));
        }

        static List<FeatureSetAsset> GetFeatureSetReferences(SerializedProperty featureSetsProperty)
        {
            var featureSets = new List<FeatureSetAsset>();

            if (featureSetsProperty == null || !featureSetsProperty.isArray)
                return featureSets;

            for (var i = 0; i < featureSetsProperty.arraySize; i++)
            {
                featureSets.Add(featureSetsProperty.GetArrayElementAtIndex(i).objectReferenceValue as FeatureSetAsset);
            }

            return featureSets;
        }

        static bool IsEmbeddedAssetAtPath(Object asset, string assetPath)
        {
            return asset != null
                   && !string.IsNullOrEmpty(assetPath)
                   && AssetDatabase.IsSubAsset(asset)
                   && AssetDatabase.GetAssetPath(asset) == assetPath;
        }

        static GameObject GetGameObject(Object objectReference)
        {
            return objectReference switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };
        }

        static void ClearDropZoneState(VisualElement dropZone)
        {
            dropZone.RemoveFromClassList("fv-drop-zone--active");
            dropZone.RemoveFromClassList("fv-drop-zone--rejected");
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
}
