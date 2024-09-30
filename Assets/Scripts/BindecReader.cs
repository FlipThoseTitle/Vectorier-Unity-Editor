using System.IO;
using UnityEngine;
using UnityEditor;

public class BindecReader : EditorWindow
{
    private string filePath = "";
    private bool incrementNames = false;
    private bool useSF2Rig = false;

    // node name for Vector
    private static readonly string[] predefinedNames = new string[]
    {
        "NHip_1", "NHip_2", "NStomach", "NChest", "NNeck", "NShoulder_1", "NShoulder_2",
        "NKnee_1", "NKnee_2", "NAnkle_1", "NAnkle_2", "NToe_1", "NHeel_1", "NToeTip_1", "NToeS_1", "NHeel_2", "NToe_2",
        "NToeTip_2", "NToeS_2", "NElbow_1", "NElbow_2", "NWrist_1", "NWrist_2", "NKnuckles_1", "NFingertips_1",
        "NKnucklesS_1", "NKnuckles_2", "NFingertips_2", "NKnucklesS_2", "NHead", "NTop", "NChestS_1", "NChestS_2",
        "NStomachS_1", "NStomachS_2", "NChestF", "NStomachF", "NPelvisF", "NHeadS_1", "NHeadS_2", "NHeadF", "NPivot",
        "DetectorH", "DetectorV", "COM", "Camera"
    };

    // node name for Shadow Fight 2
    private static readonly string[] predefinedNamesSF2 = new string[]
    {
        "NTop", "NNeck", "NShoulder_2", "NShoulder_1", "NElbow_2", "NElbow_1", "NWrist_2", "NWrist_1",
        "NFingertipsSS_2", "NFingertipsSS_1", "NHip_2", "NHip_1", "NKnee_2", "NKnee_1", "NAnkle_2",
        "NAnkle_1", "NToe_2", "NToe_1", "NPivot", "Weapon-Node1_1", "Weapon-Node2_1", "Weapon-Node3_1",
        "Weapon-Node4_1", "Weapon-Node1_2", "Weapon-Node2_2", "Weapon-Node3_2", "Weapon-Node4_2",
        "NStomach", "NChest", "NToeTip_2", "NHeel_2", "NHeel_1", "NToeS_2", "NToeTip_1", "NToeS_1",
        "NKnuckles_2", "NKnucklesS_2", "NKnuckles_1", "NKnucklesS_1", "NFingertips_2", "NFingertips_1",
        "NFingertipsS_2", "NFingertipsS_1", "NHead", "NChestS_2", "NChestS_1", "NStomachS_2",
        "NStomachS_1", "NChestF", "NStomachF", "NPelvisF", "NHeadS_2", "NHeadS_1", "NHeadF", "COM",
        "MacroNode1_2", "MacroNode2_2", "MacroNode3_2", "MacroNode4_2", "MacroNode5_2", "MacroNode6_2",
        "MacroNode1_1", "MacroNode2_1", "MacroNode3_1", "MacroNode4_1", "MacroNode5_1", "MacroNode6_1"
    };

    [MenuItem("Vectorier/Miscellaneous/Bin/Bindec Read")]
    public static void ShowWindow()
    {
        GetWindow<BindecReader>("Bindec Reader");
    }

    void OnGUI()
    {
        GUILayout.Label("Select .bindec file", EditorStyles.boldLabel);

        if (GUILayout.Button("Select File"))
        {
            filePath = EditorUtility.OpenFilePanel("Select .bindec file", "", "bindec");
        }

        GUILayout.Label("File Path: " + filePath);

        incrementNames = GUILayout.Toggle(incrementNames, "Increment Sphere Names");

        // Add checkbox for Use SF2 Rig
        useSF2Rig = GUILayout.Toggle(useSF2Rig, "Use SF2 Rig");

        if (!string.IsNullOrEmpty(filePath) && GUILayout.Button("Read and Place Objects"))
        {
            ReadFileAndPlaceObjects(filePath, incrementNames, useSF2Rig);
        }
    }

    private static void ReadFileAndPlaceObjects(string path, bool incrementNames, bool useSF2Rig)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        int frameIndex = 1; // frame starts from 1

        string[] selectedNames = useSF2Rig ? predefinedNamesSF2 : predefinedNames;

        foreach (string line in lines)
        {
            if (line.Contains("END"))
            {
                GameObject parentObject = new GameObject("Frame" + frameIndex);
                frameIndex++;

                Vector3[] positions = ParseLine(line);
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 position = positions[i];
                    if (position != Vector3.zero)
                    {
                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.position = position;
                        sphere.transform.parent = parentObject.transform; // Set the parent

                        if (incrementNames)
                        {
                            if (i < selectedNames.Length)
                            {
                                sphere.name = selectedNames[i];
                            }
                            else
                            {
                                sphere.name = "Sphere_" + (i + 1); // name to default if node name ran out
                            }
                        }
                    }
                }
            }
        }
    }

    private static Vector3[] ParseLine(string line)
    {
        // Get everything between the square brackets and the "END" token
        int startIndex = line.IndexOf('[') + 1;
        int endIndex = line.LastIndexOf("END");
        if (startIndex == -1 || endIndex == -1)
        {
            Debug.LogError("Line format is incorrect: " + line);
            return new Vector3[0];
        }

        string coordinatePart = line.Substring(startIndex, endIndex - startIndex);
        // Extracting all coordinate sets between curly braces
        var matches = System.Text.RegularExpressions.Regex.Matches(coordinatePart, @"\{([^}]*)\}");
        Vector3[] positions = new Vector3[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            string triplet = matches[i].Groups[1].Value;
            string[] values = triplet.Split(',');

            if (values.Length != 3)
            {
                Debug.LogError("Triplet format is incorrect: " + triplet);
                continue;
            }

            if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y) && float.TryParse(values[2], out float z))
            {
                // Invert the y-axis
                positions[i] = new Vector3(x, -y, z);
            }
            else
            {
                Debug.LogError("Error parsing float values from triplet: " + triplet);
                positions[i] = Vector3.zero;
            }
        }
        return positions;
    }
}
