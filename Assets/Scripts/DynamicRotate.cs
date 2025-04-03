using System;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Vectorier/DynamicRotate")]
public class DynamicRotate : MonoBehaviour
{
    [Tooltip("Transformation name of the dynamic object")]
    public string TransformationName = "Transform_name";

    public enum AnchorPoint
    {
        TopLeft,
        TopRight,
        Center,
        BottomLeft,
        BottomRight
    }

    [Serializable]
    public class TransformationRotate
    {
        [Tooltip("Move Duration in Seconds")]
        public float MoveDuration = 1.5f;

        [Tooltip("Rotation Angle")]
        public float Angle = 0.0f;

        [Tooltip("Pivot that will be used for Rotation Point")]
        public AnchorPoint Anchor = AnchorPoint.Center;
    }

    [SerializeField] public TransformationRotate Rotation;

    private GameObject previewObject;
    private bool isRotating = false;

    public void SetupPreview()
    {
        if (previewObject != null) return;

        previewObject = new GameObject("RotatePreview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;

        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
        if (originalRenderer != null)
        {
            SpriteRenderer previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = originalRenderer.sprite;
            previewRenderer.sortingOrder = originalRenderer.sortingOrder + 1;

            Color color = previewRenderer.color;
            color.a = 120f / 255f; // Set transparency
            previewRenderer.color = color;
        }

        isRotating = true;
        EditorUtility.SetDirty(this);
    }

    public void UpdateRotationFromPreview()
    {
        if (!isRotating || previewObject == null)
        {
            Debug.LogError("Preview object is missing or rotation is not active");
            return;
        }

        Rotation.Angle = previewObject.transform.eulerAngles.z;

        Debug.Log("Updated Rotation from RotatePreview");

        EditorUtility.SetDirty(this);
        SceneView.RepaintAll();
    }

    public void FinishSetup()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        isRotating = false;
        EditorUtility.SetDirty(this);
    }
}

[CustomEditor(typeof(DynamicRotate))]
[CanEditMultipleObjects]
public class DynamicRotateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Multi-object editing is not supported for DynamicRotate.", MessageType.Warning);
            return;
        }

        DrawDefaultInspector();
        DynamicRotate dynamicRotateComponent = (DynamicRotate)target;

        if (GUILayout.Button("Setup Preview"))
        {
            dynamicRotateComponent.SetupPreview();
        }

        if (GUILayout.Button("Update Rotation From Preview"))
        {
            dynamicRotateComponent.UpdateRotationFromPreview();
        }

        if (GUILayout.Button("Finish Setup"))
        {
            dynamicRotateComponent.FinishSetup();
        }

        EditorUtility.SetDirty(dynamicRotateComponent);
    }
}
