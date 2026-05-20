using System.Collections.Generic;
using FastVariants.GameObjectVisibilityVariant;
using FastVariants.PrefabFingerprint;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ConfigurationManagerAsset = FastVariants.ConfigurationManager.ConfigurationManager;

namespace FastVariants.Editor
{
    [CustomEditor(typeof(GameObjectVisibilityVariantAsset))]
    public class GameObjectVisibilityVariantAssetEditor : UnityEditor.Editor
    {
        [SerializeField] VisualTreeAsset m_VisualTreeAsset;
        [SerializeField] StyleSheet m_Stylesheet;

        const string ItemGameObjectsPropertyName = "m_ItemGameObjects";
        const string VariantSetPropertyName = "m_GameObjectVisibilityVariantSet";
        const string FeatureSetsPropertyName = "m_FeatureSets";
        const string ProductPropertyName = "m_Product";
        const string DropZoneName = "target-gameobject-drop-zone";
        const string DropZoneStatusName = "target-drop-zone-status";
        const string DefaultDropZoneStatus = "Drop prefab assets, or scene prefab instance children from the owning Configuration Manager product prefab.";

        bool m_ValidatingTargetGameObjects;

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

            root.Q<PropertyField>("item-is-valid")?.SetEnabled(false);
            root.Q<PropertyField>("variant")?.SetEnabled(false);
            root.Q<PropertyField>("protobuf")?.SetEnabled(false);

            SetupTargetDropZone(root);

            return root;
        }

        void SetupTargetDropZone(VisualElement root)
        {
            var dropZone = root.Q<VisualElement>(DropZoneName);
            if (dropZone == null)
                return;

            var statusLabel = root.Q<Label>(DropZoneStatusName);
            SetDropZoneStatus(statusLabel, DefaultDropZoneStatus);
            TrackTargetGameObjectChanges(root, statusLabel);

            dropZone.RegisterCallback<DragEnterEvent>(_ => dropZone.AddToClassList("fv-drop-zone--active"));
            dropZone.RegisterCallback<DragLeaveEvent>(_ => ClearDropZoneState(dropZone));
            dropZone.RegisterCallback<DragExitedEvent>(_ => ClearDropZoneState(dropZone));

            dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                var canRegister = TryGetDraggedProjectGameObjects(out _, out var statusText);

                DragAndDrop.visualMode = canRegister ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                dropZone.EnableInClassList("fv-drop-zone--active", canRegister);
                dropZone.EnableInClassList("fv-drop-zone--rejected", !canRegister);
                SetDropZoneStatus(statusLabel, statusText);
                evt.StopPropagation();
            });

            dropZone.RegisterCallback<DragPerformEvent>(evt =>
            {
                var canRegister = TryGetDraggedProjectGameObjects(out var projectGameObjects, out var statusText);
                ClearDropZoneState(dropZone);

                if (!canRegister)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    SetDropZoneStatus(statusLabel, statusText);
                    evt.StopPropagation();
                    return;
                }

                DragAndDrop.AcceptDrag();
                var addedGameObjects = AddTargetGameObjects(projectGameObjects);
                SetDropZoneStatus(statusLabel, addedGameObjects.Count switch
                {
                    0 => "Dropped targets are already registered.",
                    1 => $"Registered Project GameObject: {addedGameObjects[0].name}",
                    _ => $"Registered {addedGameObjects.Count} Project GameObjects."
                });
                evt.StopPropagation();
            });
        }

        void TrackTargetGameObjectChanges(VisualElement root, Label statusLabel)
        {
            var itemGameObjectsProperty = serializedObject.FindProperty(ItemGameObjectsPropertyName);
            if (itemGameObjectsProperty == null || !itemGameObjectsProperty.isArray)
                return;

            root.TrackPropertyValue(itemGameObjectsProperty, property => ValidateTargetGameObjects(property, statusLabel));
        }

        void ValidateTargetGameObjects(SerializedProperty targetsProperty, Label statusLabel)
        {
            if (m_ValidatingTargetGameObjects)
                return;

            if (targetsProperty == null || !targetsProperty.isArray || targetsProperty.arraySize == 0)
            {
                SetDropZoneStatus(statusLabel, DefaultDropZoneStatus);
                return;
            }

            if (!TryGetOwningConfigurationManagerProduct(out var configurationManager, out var productPrefab, out var statusText))
            {
                SetDropZoneStatus(statusLabel, statusText);
                return;
            }

            m_ValidatingTargetGameObjects = true;

            var removedInvalidTarget = false;
            var removedProductRoot = false;
            var removedDuplicateTarget = false;
            var changedTarget = false;
            var validTargets = new List<GameObject>();

            Undo.RecordObject(serializedObject.targetObject, "Validate Target GameObjects");

            for (var i = targetsProperty.arraySize - 1; i >= 0; i--)
            {
                var element = targetsProperty.GetArrayElementAtIndex(i);
                var selectedGameObject = element.objectReferenceValue as GameObject;
                if (selectedGameObject == null)
                    continue;

                if (!PrefabFingerprintRegistryEditorUtility.TryGetRegisteredGameObject(configurationManager.prefabFingerprint, productPrefab, selectedGameObject, out var fingerprintGameObject, out var isProductRoot)
                    || isProductRoot)
                {
                    if (isProductRoot)
                        removedProductRoot = true;
                    else
                        removedInvalidTarget = true;

                    RemoveArrayElementAtIndex(targetsProperty, i);
                    changedTarget = true;
                    continue;
                }

                if (validTargets.Contains(fingerprintGameObject))
                {
                    removedDuplicateTarget = true;
                    RemoveArrayElementAtIndex(targetsProperty, i);
                    changedTarget = true;
                    continue;
                }

                validTargets.Add(fingerprintGameObject);
                if (fingerprintGameObject != selectedGameObject)
                {
                    element.objectReferenceValue = fingerprintGameObject;
                    changedTarget = true;
                }
            }

            if (changedTarget)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
            }

            m_ValidatingTargetGameObjects = false;

            if (removedProductRoot)
                SetDropZoneStatus(statusLabel, $"Removed product root target. Select children of {productPrefab.name}, not the product prefab itself.");
            else if (removedInvalidTarget)
                SetDropZoneStatus(statusLabel, $"Removed targets that are not registered in the {productPrefab.name} product fingerprint.");
            else if (removedDuplicateTarget)
                SetDropZoneStatus(statusLabel, "Removed duplicate target GameObjects.");
            else if (validTargets.Count == 1)
                SetDropZoneStatus(statusLabel, $"Registered Project GameObject: {validTargets[0].name}");
            else if (validTargets.Count > 1)
                SetDropZoneStatus(statusLabel, $"Registered {validTargets.Count} Project GameObjects.");
            else
                SetDropZoneStatus(statusLabel, DefaultDropZoneStatus);
        }

        List<GameObject> AddTargetGameObjects(IReadOnlyList<GameObject> gameObjects)
        {
            var addedGameObjects = new List<GameObject>();

            if (gameObjects == null || gameObjects.Count == 0)
                return addedGameObjects;

            Undo.RecordObject(serializedObject.targetObject, "Set Target GameObjects");
            serializedObject.Update();

            var targetsProperty = serializedObject.FindProperty(ItemGameObjectsPropertyName);
            if (targetsProperty == null || !targetsProperty.isArray)
                return addedGameObjects;

            foreach (var gameObject in gameObjects)
            {
                if (gameObject == null || ContainsGameObject(targetsProperty, gameObject))
                    continue;

                var newIndex = targetsProperty.arraySize;
                targetsProperty.InsertArrayElementAtIndex(newIndex);
                targetsProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = gameObject;
                addedGameObjects.Add(gameObject);
            }

            if (addedGameObjects.Count == 0)
                return addedGameObjects;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
            return addedGameObjects;
        }

        static bool ContainsGameObject(SerializedProperty targetsProperty, GameObject gameObject)
        {
            if (targetsProperty == null || !targetsProperty.isArray || gameObject == null)
                return false;

            for (var i = 0; i < targetsProperty.arraySize; i++)
            {
                if (targetsProperty.GetArrayElementAtIndex(i).objectReferenceValue == gameObject)
                    return true;
            }

            return false;
        }

        bool TryGetDraggedProjectGameObjects(out List<GameObject> projectGameObjects, out string statusText)
        {
            projectGameObjects = new List<GameObject>();

            if (!TryGetOwningConfigurationManagerProduct(out var configurationManager, out var productPrefab, out statusText))
                return false;

            var droppedProductRoot = false;

            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                var draggedGameObject = GetGameObject(objectReference);
                if (draggedGameObject == null)
                    continue;

                if (!PrefabFingerprintRegistryEditorUtility.TryGetRegisteredGameObject(configurationManager.prefabFingerprint, productPrefab, draggedGameObject, out var fingerprintGameObject, out var isProductRoot))
                {
                    if (isProductRoot)
                        droppedProductRoot = true;

                    continue;
                }

                if (isProductRoot)
                {
                    droppedProductRoot = true;
                    continue;
                }

                if (!projectGameObjects.Contains(fingerprintGameObject))
                    projectGameObjects.Add(fingerprintGameObject);
            }

            if (projectGameObjects.Count > 0)
            {
                statusText = projectGameObjects.Count == 1
                    ? $"Ready to register {projectGameObjects[0].name} from product prefab {productPrefab.name}."
                    : $"Ready to register {projectGameObjects.Count} targets from product prefab {productPrefab.name}.";
                return true;
            }

            statusText = droppedProductRoot
                ? $"Drop children of {productPrefab.name}, not the product prefab itself."
                : $"Drop prefab objects registered in the {productPrefab.name} product fingerprint.";
            return false;
        }

        static void RemoveArrayElementAtIndex(SerializedProperty arrayProperty, int index)
        {
            arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            arrayProperty.DeleteArrayElementAtIndex(index);
        }

        static GameObject ResolveProjectGameObject(Object objectReference)
        {
            var gameObject = GetGameObject(objectReference);

            if (gameObject == null)
                return null;

            if (EditorUtility.IsPersistent(gameObject))
                return gameObject;

            return PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        }

        bool TryGetOwningConfigurationManagerProduct(out ConfigurationManagerAsset configurationManager, out GameObject productPrefab, out string statusText)
        {
            configurationManager = null;
            productPrefab = null;

            configurationManager = GetOwningConfigurationManager();
            if (configurationManager == null)
            {
                statusText = "This variant must belong to a Configuration Manager before targets can be dropped.";
                return false;
            }

            var serializedConfigurationManager = new SerializedObject(configurationManager);
            var productProperty = serializedConfigurationManager.FindProperty(ProductPropertyName);
            productPrefab = ResolveProjectGameObject(productProperty?.objectReferenceValue);

            if (productPrefab == null)
            {
                statusText = "Assign a Product prefab on the owning Configuration Manager before dropping targets.";
                return false;
            }

            if (configurationManager.prefabFingerprint.count == 0)
            {
                statusText = $"The Configuration Manager product fingerprint is empty. Reassign {productPrefab.name} on the Configuration Manager to rebuild it.";
                return false;
            }

            statusText = null;
            return true;
        }

        ConfigurationManagerAsset GetOwningConfigurationManager()
        {
            var assetPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            var variantSet = serializedObject.FindProperty(VariantSetPropertyName)?.objectReferenceValue as GameObjectVisibilityVariantSetAsset;
            ConfigurationManagerAsset fallbackConfigurationManager = null;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is not ConfigurationManagerAsset configurationManager)
                    continue;

                fallbackConfigurationManager ??= configurationManager;

                if (variantSet != null && ContainsFeatureSet(configurationManager, variantSet))
                    return configurationManager;
            }

            return fallbackConfigurationManager;
        }

        static bool ContainsFeatureSet(ConfigurationManagerAsset configurationManager, GameObjectVisibilityVariantSetAsset variantSet)
        {
            var serializedConfigurationManager = new SerializedObject(configurationManager);
            var featureSetsProperty = serializedConfigurationManager.FindProperty(FeatureSetsPropertyName);

            if (featureSetsProperty == null || !featureSetsProperty.isArray)
                return false;

            for (var i = 0; i < featureSetsProperty.arraySize; i++)
            {
                if (featureSetsProperty.GetArrayElementAtIndex(i).objectReferenceValue == variantSet)
                    return true;
            }

            return false;
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

        static void SetDropZoneStatus(Label statusLabel, string text)
        {
            if (statusLabel != null)
                statusLabel.text = text;
        }
    }
}
