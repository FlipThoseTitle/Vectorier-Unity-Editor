using UnityEngine;
using UnityEditor;

public class OrderInLayerAdder : EditorWindow
{
    private string input = "0";

    [MenuItem("Vectorier/Miscellaneous/Adds to Order In Layer", false, 0)]
    private static void ShowWindow()
    {
        // Show the custom editor window
        GetWindow<OrderInLayerAdder>("Add to Order In Layer");
    }

    private void OnGUI()
    {
        // Display a text field to get the input from the user
        GUILayout.Label("Enter a number to add to Order in Layer:", EditorStyles.boldLabel);
        input = EditorGUILayout.TextField("Number to Add:", input);

        // Add a button to apply the changes
        if (GUILayout.Button("Apply"))
        {
            // Try to parse the input to an integer
            if (int.TryParse(input, out int numberToAdd))
            {
                // Get the selected GameObjects in the Unity editor
                GameObject[] selectedObjects = Selection.gameObjects;

                foreach (GameObject obj in selectedObjects)
                {
                    SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

                    if (sr != null)
                    {
                        // Add the number to the existing order in layer
                        sr.sortingOrder += numberToAdd;

                        // Mark the object as dirty to save changes
                        EditorUtility.SetDirty(sr);
                    }
                }
                // Close the window after applying changes
                this.Close();
            }
            else
            {
                // Show an error message if the input is not a valid integer
                EditorUtility.DisplayDialog("Error", "Please enter a valid number.", "OK");
            }
        }
    }
}
