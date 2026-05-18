using FastVariants.GameObjectVisibilityVariant;
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

        const string ItemGameObjectPropertyName = "m_ItemGameObject";
        const string VariantSetPropertyName = "m_GameObjectVisibilityVariantSet";
        const string FeatureSetsPropertyName = "m_FeatureSets";
        const string ProductPropertyName = "m_Product";
        const string DropZoneName = "target-gameobject-drop-zone";
        const string DropZoneStatusName = "target-drop-zone-status";

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
            SetDropZoneStatus(statusLabel, "Drop a prefab asset, or a scene prefab instance child from the owning Configuration Manager product prefab.");

            dropZone.RegisterCallback<DragEnterEvent>(_ => dropZone.AddToClassList("fv-drop-zone--active"));
            dropZone.RegisterCallback<DragLeaveEvent>(_ => ClearDropZoneState(dropZone));
            dropZone.RegisterCallback<DragExitedEvent>(_ => ClearDropZoneState(dropZone));

            dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                var canRegister = TryGetDraggedProjectGameObject(out _, out var statusText);

                DragAndDrop.visualMode = canRegister ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                dropZone.EnableInClassList("fv-drop-zone--active", canRegister);
                dropZone.EnableInClassList("fv-drop-zone--rejected", !canRegister);
                SetDropZoneStatus(statusLabel, statusText);
                evt.StopPropagation();
            });

            dropZone.RegisterCallback<DragPerformEvent>(evt =>
            {
                var canRegister = TryGetDraggedProjectGameObject(out var projectGameObject, out var statusText);
                ClearDropZoneState(dropZone);

                if (!canRegister)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    SetDropZoneStatus(statusLabel, statusText);
                    evt.StopPropagation();
                    return;
                }

                DragAndDrop.AcceptDrag();

                Undo.RecordObject(serializedObject.targetObject, "Set Target GameObject");
                serializedObject.Update();

                var property = serializedObject.FindProperty(ItemGameObjectPropertyName);
                property.objectReferenceValue = projectGameObject;
                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(serializedObject.targetObject);
                SetDropZoneStatus(statusLabel, $"Registered Project GameObject: {projectGameObject.name}");
                evt.StopPropagation();
            });
        }

        bool TryGetDraggedProjectGameObject(out GameObject projectGameObject, out string statusText)
        {
            projectGameObject = null;

            if (!TryGetOwningProductPrefab(out var productPrefab, out statusText))
                return false;

            var droppedProductRoot = false;

            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                var draggedGameObject = GetGameObject(objectReference);
                if (draggedGameObject == null)
                    continue;

                var draggedProjectGameObject = ResolveProjectGameObject(draggedGameObject);
                if (draggedProjectGameObject == null)
                    continue;

                if (draggedProjectGameObject == productPrefab)
                {
                    droppedProductRoot = true;
                    continue;
                }

                if (!IsInsideProductPrefab(draggedGameObject, draggedProjectGameObject, productPrefab))
                    continue;

                projectGameObject = draggedProjectGameObject;
                statusText = $"Ready to register {projectGameObject.name} from product prefab {productPrefab.name}.";
                return true;
            }

            statusText = droppedProductRoot
                ? $"Drop a child of {productPrefab.name}, not the product prefab itself."
                : $"Drop a prefab object from inside the {productPrefab.name} product prefab.";
            return false;
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

        bool TryGetOwningProductPrefab(out GameObject productPrefab, out string statusText)
        {
            productPrefab = null;

            var configurationManager = GetOwningConfigurationManager();
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

        static bool IsInsideProductPrefab(GameObject draggedGameObject, GameObject projectGameObject, GameObject productPrefab)
        {
            if (draggedGameObject == null || projectGameObject == null || productPrefab == null)
                return false;

            if (EditorUtility.IsPersistent(draggedGameObject))
                return IsTransformUnder(projectGameObject.transform, productPrefab.transform);

            return IsSceneObjectInsideProductPrefabInstance(draggedGameObject, productPrefab)
                   || IsTransformUnder(projectGameObject.transform, productPrefab.transform);
        }

        static bool IsSceneObjectInsideProductPrefabInstance(GameObject gameObject, GameObject productPrefab)
        {
            for (var current = gameObject.transform; current != null; current = current.parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject) == productPrefab)
                    return true;
            }

            return false;
        }

        static bool IsTransformUnder(Transform child, Transform parent)
        {
            for (var current = child; current != null; current = current.parent)
            {
                if (current == parent)
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
