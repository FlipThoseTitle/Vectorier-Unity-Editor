using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[AddComponentMenu("Vectorier/DynamicPreview")]
public class DynamicPreview : MonoBehaviour
{
    public List<string> DynamicTransformationNames = new List<string>();
    private bool isPreviewRunning = false;

    // isPreviewRunning read-only property
    public bool IsPreviewRunning => isPreviewRunning;

    public void StartPreview()
    {
        if (isPreviewRunning)
        {
            Debug.LogWarning("Preview is already running!");
            return;
        }

        List<GameObject> dynamicObjects = new List<GameObject>();
		dynamicObjects.AddRange(GameObject.FindGameObjectsWithTag("Dynamic"));
		dynamicObjects.AddRange(GameObject.FindGameObjectsWithTag("Image"));
		
		List<Dynamic> matchedDynamics = new List<Dynamic>();
		List<DynamicColor> matchedColors = new List<DynamicColor>();

        foreach (GameObject dynamicObject in dynamicObjects)
        {
            // Get all Dynamic components
            Dynamic[] dynamicComponents = dynamicObject.GetComponents<Dynamic>();
            if (dynamicComponents != null && dynamicComponents.Length > 0)
            {
                foreach (Dynamic dynamicComponent in dynamicComponents)
                {
                    // Match the transformation name
                    if (DynamicTransformationNames.Contains(dynamicComponent.TransformationName))
                    {
                        dynamicComponent.ResetAfterPreviewFinish = false; // Set ResetAfterPreviewFinish to false
                        matchedDynamics.Add(dynamicComponent);
                    }
                }
            }
			DynamicColor dynamicColor = dynamicObject.GetComponent<DynamicColor>();
            if (dynamicColor != null)
            {
                matchedColors.Add(dynamicColor);
            }
        }

        if (matchedDynamics.Count == 0 && matchedColors.Count == 0)
        {
            Debug.LogWarning("No matching Dynamic components found for the specified transformation names.");
            return;
        }

        // Play preview on matched dynamic
        foreach (Dynamic dynamic in matchedDynamics)
        {
            dynamic.PlayPreview();
        }
		
		// Start color preview
		foreach (DynamicColor colorComponent in matchedColors)
        {
            colorComponent.PreviewColor();
        }

        // Disable GUI buttons and wait for preview to finish
        isPreviewRunning = true;
        StartCoroutine(WaitForPreviewToEnd(matchedDynamics, matchedColors));
    }

    public void ResetPreview()
    {
        if (isPreviewRunning)
        {
            Debug.LogWarning("Cannot reset preview while it is running!");
            return;
        }

        List<GameObject> dynamicObjects = new List<GameObject>();
		dynamicObjects.AddRange(GameObject.FindGameObjectsWithTag("Dynamic"));
		dynamicObjects.AddRange(GameObject.FindGameObjectsWithTag("Image"));

        foreach (GameObject dynamicObject in dynamicObjects)
        {
            // Get all Dynamic components
            Dynamic[] dynamicComponents = dynamicObject.GetComponents<Dynamic>();
            if (dynamicComponents != null && dynamicComponents.Length > 0)
            {
                foreach (Dynamic dynamicComponent in dynamicComponents)
                {
                    // Match the transformation name
                    if (DynamicTransformationNames.Contains(dynamicComponent.TransformationName))
                    {
                        dynamicComponent.ResetPreviewPosition(); // Reset position
                    }
                }
            }
			DynamicColor dynamicColor = dynamicObject.GetComponent<DynamicColor>();
            if (dynamicColor != null)
            {
                dynamicColor.ResetColor();
            }
        }
    }

    private System.Collections.IEnumerator WaitForPreviewToEnd(List<Dynamic> dynamics, List<DynamicColor> colorComponents)
    {
        bool allPreviewsFinished = false;

        while (!allPreviewsFinished)
        {
            allPreviewsFinished = true;

            foreach (Dynamic dynamic in dynamics)
            {
                if (dynamic.IsPreviewDisabled)
                {
                    allPreviewsFinished = false;
                    break;
                }
            }
			foreach (DynamicColor colorComponent in colorComponents)
            {
                if (colorComponent.IsTransitioning)
                {
                    allPreviewsFinished = false;
                    break;
                }
            }

            yield return null; // Wait for the next frame
        }

        // Re-enable ResetAfterPreviewFinish and GUI buttons
        foreach (Dynamic dynamic in dynamics)
        {
            dynamic.ResetAfterPreviewFinish = true;
        }

        isPreviewRunning = false;
        Debug.Log("All previews finished.");
    }
}


[CustomEditor(typeof(DynamicPreview))]
[CanEditMultipleObjects]
public class DynamicPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox("Multi-object editing is not supported for DynamicPreview.", MessageType.Warning);
            return;
        }

        DrawDefaultInspector();
        DynamicPreview dynamicPreview = (DynamicPreview)target;

        // Disable GUI buttons if a preview is running
        GUI.enabled = !dynamicPreview.IsPreviewRunning;

        if (GUILayout.Button("Start Preview"))
        {
            dynamicPreview.StartPreview();
        }

        if (GUILayout.Button("Reset Preview"))
        {
            dynamicPreview.ResetPreview();
        }

        // Re-enable GUI for other controls
        GUI.enabled = true;
    }
}