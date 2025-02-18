using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class SpriteRendererExport : MonoBehaviour
{
    public static void ExportSpriteWithColor(SpriteRenderer spriteRenderer, string path, bool asPng = true)
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is not assigned.");
            return;
        }

        Texture2D originalTexture = spriteRenderer.sprite.texture;

        // Save the original import settings
        string assetPath = AssetDatabase.GetAssetPath(originalTexture);
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        bool wasReadable = textureImporter.isReadable;
        TextureImporterCompression originalCompression = textureImporter.textureCompression;

        // Set texture to be readable
        textureImporter.isReadable = true;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // Create new Texture2D with same dimensions
        Texture2D modifiedTexture = new Texture2D(originalTexture.width, originalTexture.height);

        // Apply SpriteRenderer color to the texture
        for (int y = 0; y < originalTexture.height; y++)
        {
            for (int x = 0; x < originalTexture.width; x++)
            {
                Color originalColor = originalTexture.GetPixel(x, y);
                Color modifiedColor = originalColor * spriteRenderer.color;
                modifiedTexture.SetPixel(x, y, modifiedColor);
            }
        }
        modifiedTexture.Apply();

        // Save the modified texture as an image
        SaveTexture(modifiedTexture, path, asPng);

        // Revert the texture settings to their old setting
        textureImporter.isReadable = wasReadable;
        textureImporter.textureCompression = originalCompression;
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    static void SaveTexture(Texture2D texture, string filePath, bool asPng)
    {
        byte[] bytes;
        if (asPng)
        {
            bytes = texture.EncodeToPNG();
        }
        else
        {
            bytes = texture.EncodeToJPG();
        }
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Texture saved to " + filePath);
    }

    [MenuItem("Vectorier/Miscellaneous/Export Selected Sprites")]
    public static void ExportSelectedSprites()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder to Save Sprites", "", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("No folder selected.");
            return;
        }

        bool asPng = true;

        // Iterate over selected objects
        foreach (GameObject obj in Selection.gameObjects)
        {
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                string cleanName = Regex.Replace(obj.name, @" \((.*?)\)", string.Empty);

                // Create file name with hex color
                string fileName = cleanName + (asPng ? ".png" : ".jpg");
                string filePath = Path.Combine(folderPath, fileName);

                // Export sprite regardless of the color
                ExportSpriteWithColor(spriteRenderer, filePath, asPng);
            }
        }

        Debug.Log("Selected sprites have been exported.");
    }
}
