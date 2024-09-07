using UnityEngine;
using UnityEditor;
using System.IO;

public class NameVariation : MonoBehaviour
{
    public string nameString = ""; // string to be add behind the name of each files
    public bool addInFront; // add infront instead of the back

    [MenuItem("Vectorier/Miscellaneous/Batch Rename")]
    static void RenameFiles()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", ""); //folder path
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is not selected!");
            return;
        }

        NameVariation fileRenamer = FindObjectOfType<NameVariation>(); //find the nameVariation component in the scene
        if (fileRenamer == null)
        {
            Debug.LogError("FileRenamer component not found in the scene!");
            return;
        }

        string nameString = fileRenamer.nameString;
        bool addInFront = fileRenamer.addInFront;

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Folder does not exist: " + folderPath);
            return;
        }

        string[] files = Directory.GetFiles(folderPath);

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName);
            string newFileName;

            if (addInFront)
            {
                // Add nameString to the front of the file name
                newFileName = nameString + fileNameWithoutExtension + fileExtension;
            }
            else
            {
                // Add nameString to the back of the file name
                newFileName = fileNameWithoutExtension + nameString + fileExtension;
            }

            string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

            File.Move(filePath, newFilePath);
        }


        Debug.Log("File renaming completed!");
    }
}
