using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Vectorier/DynamicSize")]
public class DynamicSize : MonoBehaviour
{
    [Tooltip("Transformation name of the dynamic object")]
    public string TransformationName = "Transform_name";

    [Serializable]
    public class TransformationSize
    {
        [Tooltip("Move Duration in Seconds")]
        public float MoveDuration = 1.5f;

        [Tooltip("Final Width of the object")]
        public float FinalWidth = 0.0f;

        [Tooltip("Final Height of the object")]
        public float FinalHeight = 0.0f;
    }

    [SerializeField] public TransformationSize Size;

    private GameObject previewObject;
    private SpriteRenderer spriteRenderer;
    private Vector2 originalResolution;
    private bool isResizing = false;

    private void GetSpriteRenderer()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				originalResolution = new Vector2(
					spriteRenderer.sprite.texture.width,
					spriteRenderer.sprite.texture.height
				);
			}
			else
			{
				Debug.LogError("SpriteRenderer or Sprite is missing");
				originalResolution = Vector2.one;
			}
		}
	}

    public void SetupPreview()
    {
        if (previewObject != null) return;

        GetSpriteRenderer();

        previewObject = new GameObject("SizePreview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localScale = Vector3.one;

        SpriteRenderer previewRenderer = previewObject.AddComponent<SpriteRenderer>();
        previewRenderer.sprite = spriteRenderer.sprite;
        previewRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Color color = previewRenderer.color;
        color.a = 120f / 255f; // Set alpha to 120
        previewRenderer.color = color;

        isResizing = true;
        EditorUtility.SetDirty(this);
    }

    public void UpdateSizeFromPreview()
	{
		if (!isResizing || previewObject == null)
		{
			Debug.LogError("Preview object is missing or resizing is not active");
			return;
		}

		SpriteRenderer previewRenderer = previewObject.GetComponent<SpriteRenderer>();
		if (previewRenderer == null || previewRenderer.sprite == null)
		{
			Debug.LogError("SizePreview does not have a valid SpriteRenderer or Sprite");
			return;
		}

		Vector2 previewOriginalResolution = new Vector2(
			previewRenderer.sprite.texture.width,
			previewRenderer.sprite.texture.height
		);

		if (previewOriginalResolution.x <= 0 || previewOriginalResolution.y <= 0)
		{
			Debug.LogError($"SizePreview's original resolution is invalid: {previewOriginalResolution.x}x{previewOriginalResolution.y}");
			return;
		}

		// Calculate new width and height based on SizePreview world scale
		Vector3 scale = previewObject.transform.lossyScale; 
		Size.FinalWidth = previewOriginalResolution.x * scale.x;
		Size.FinalHeight = previewOriginalResolution.y * scale.y;

		Debug.Log("Updated Size from SizePreview");

		EditorUtility.SetDirty(this);
		SceneView.RepaintAll();
	}

    public void FinishSetup()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        isResizing = false;
        EditorUtility.SetDirty(this);
    }

}


[CustomEditor(typeof(DynamicSize))]
[CanEditMultipleObjects]
public class DynamicSizeEditor : Editor
{
    public override void OnInspectorGUI()
    {
		if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Multi-object editing is not supported for DynamicSize.", MessageType.Warning);
            return;
        }
		
        DrawDefaultInspector();
        DynamicSize dynamicSizeComponent = (DynamicSize)target;

        if (GUILayout.Button("Setup Preview"))
        {
            dynamicSizeComponent.SetupPreview();
        }
		
		if (GUILayout.Button("Update Size From Preview"))
        {
            dynamicSizeComponent.UpdateSizeFromPreview();
        }

        if (GUILayout.Button("Finish Setup"))
        {
            dynamicSizeComponent.FinishSetup();
        }

        EditorUtility.SetDirty(dynamicSizeComponent);
    }
}
