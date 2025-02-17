using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class BinDecompile : MonoBehaviour
{
    [MenuItem("Vectorier/Miscellaneous/Bin/Bin Decompile")]
    public static void BinDecompileMenu()
    {
        // Show file selection dialog
        string filePath = EditorUtility.OpenFilePanel("Select .bin file", "", "bin");

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                string outputContent = ReadBinaryFile(filePath);
                string outputFilePath = Path.ChangeExtension(filePath, ".bindec");
                File.WriteAllText(outputFilePath, outputContent);
                Debug.Log("File written to: " + outputFilePath);
                EditorUtility.DisplayDialog("Success", "File written to: " + outputFilePath, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading file: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Error reading file: " + ex.Message, "OK");
            }
        }
    }

    private static string ReadBinaryFile(string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fileStream, Encoding.UTF8))
        {
            StringBuilder outputBuilder = new StringBuilder();

            int blockCount = reader.ReadInt32();
            outputBuilder.AppendLine($"Binary blocks count: {blockCount}");

            for (int i = 0; i < blockCount; i++)
            {
                reader.ReadByte();
                int setCount = reader.ReadInt32();
                outputBuilder.Append($"[{setCount}]");

                for (int j = 0; j < setCount; j++)
                {
                    Vector3 vector3 = new Vector3
                    {
                        x = reader.ReadSingle(),
                        y = -reader.ReadSingle(),
                        z = reader.ReadSingle()
                    };

                    outputBuilder.Append($"{{{vector3.x},{vector3.y},{vector3.z}}}");
                }

                outputBuilder.AppendLine("END");
            }

            return outputBuilder.ToString();
        }
    }
}
