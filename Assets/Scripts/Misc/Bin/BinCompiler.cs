using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class BinCompile : MonoBehaviour
{
    [MenuItem("Vectorier/Miscellaneous/Bin/Bin Compile")]
    public static void BinCompileMenu()
    {
        // Show file selection dialog
        string filePath = EditorUtility.OpenFilePanel("Select .bindec file", "", "bindec");

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                CompileBinFile(filePath);
                EditorUtility.DisplayDialog("Success", "Binary file compiled successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error compiling file: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Error compiling file: " + ex.Message, "OK");
            }
        }
    }

    private static void CompileBinFile(string filePath)
    {
        string outputFilePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".bin");

        using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fileStream))
        {
            List<List<Vector3>> blocks = new List<List<Vector3>>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = reader.ReadLine();
                if (line == null || !line.StartsWith("Binary blocks count: "))
                {
                    throw new Exception("Invalid .bindec file format.");
                }

                int blockCount = int.Parse(line.Substring("Binary blocks count: ".Length));

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim() == "END")
                    {
                        continue;
                    }

                    List<Vector3> block = new List<Vector3>();
                    blocks.Add(block);

                    string[] parts = line.Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                    int setCount = int.Parse(parts[0].Trim(new[] { '[', ']' })); // Extract set count

                    for (int i = 1; i <= setCount; i++)
                    {
                        string[] vectorParts = parts[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (vectorParts.Length == 3)
                        {
                            Vector3 vector = new Vector3
                            {
                                x = float.Parse(vectorParts[0]),
                                y = -float.Parse(vectorParts[1]), // Negate the y value
                                z = float.Parse(vectorParts[2])
                            };

                            block.Add(vector);
                        }
                    }
                }
            }

            writer.Write(blocks.Count);

            foreach (var block in blocks)
            {
                writer.Write((byte)0);
                writer.Write(block.Count);

                foreach (var vector in block)
                {
                    writer.Write(vector.x);
                    writer.Write(vector.y);
                    writer.Write(vector.z);
                }
            }
        }

        Debug.Log("Binary file compiled to: " + outputFilePath);
    }
}
