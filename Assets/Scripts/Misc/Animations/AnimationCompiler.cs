using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

using Vector3 = UnityEngine.Vector3;

// -=-=-=- //

public class AnimationCompile : MonoBehaviour {
	[MenuItem("Vectorier/Miscellaneous/Animation/Compile from XML")]
	public static void AnimationCompile_Singleton() {
		AnimationCompileWrite(false);
	}

	[MenuItem("Vectorier/Miscellaneous/Animation/Compile from XML (Batch)")]
	public static void Animationompile_Batch() {
		AnimationCompileWrite(true);
	}

	public static void AnimationCompileWrite(bool batch) {
		string cwd = Application.dataPath;
		string pardir = Path.Combine(VectorierSettings.GameDirectory, "animations");
		string inputPath;
		string[] inputFiles;

		if (pardir == null) {
			pardir = Directory.GetParent(cwd).ToString();
		}

		if (!batch) {
			inputPath = EditorUtility.OpenFilePanel("Select .xml file", pardir, "xml");
			if (string.IsNullOrEmpty(inputPath)) return;
			inputFiles = new string[] { inputPath };
		} else {
			inputPath = EditorUtility.OpenFolderPanel("Select directory containing .xml files", pardir, "");
			if (string.IsNullOrEmpty(inputPath)) return;
			inputFiles = Directory.GetFiles(inputPath, "*.xml");
		}

		foreach (string file in inputFiles) {
			try {
				string outputPath = Path.ChangeExtension(file, ".bin");

				using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
				using (BinaryWriter writer = new BinaryWriter(fileStream)) {
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(file);

					XmlElement root = xmlDoc.DocumentElement;
					if (root == null || root.Name != "Frames" || !root.HasAttribute("Count")) {
						Debug.Log(@"Invalid XML format: Missing <Frames> root element or its ""Count"" attribute.");
						return;
					}

					int frameCount = int.Parse(root.GetAttribute("Count"));
					writer.Write(frameCount);

					foreach (XmlNode frameNode in root.ChildNodes) {
						if (frameNode.NodeType != XmlNodeType.Element || !frameNode.Name.StartsWith("Frame_")) {
							continue;
						}

						// Placeholder byte
						writer.Write((byte)0);
						List<Vector3> vectors = new List<Vector3>();

						foreach (XmlNode node in frameNode.ChildNodes) {
							if (node.NodeType != XmlNodeType.Element || node.Name != "Node") continue;

							float x = float.Parse(node.Attributes["X"].Value, CultureInfo.InvariantCulture);
							float y = -float.Parse(node.Attributes["Y"].Value, CultureInfo.InvariantCulture); // Invert back
							float z = float.Parse(node.Attributes["Z"].Value, CultureInfo.InvariantCulture);

							vectors.Add(new Vector3(x, y, z));
						}

						writer.Write(vectors.Count);
						foreach (Vector3 vec in vectors) {
							writer.Write(vec.x);
							writer.Write(vec.y);
							writer.Write(vec.z);
						}
					}
				}

				outputPath = outputPath
					.Substring(cwd.Length + 1)
					.Replace('/', Path.DirectorySeparatorChar);

				if (!batch) {
					Debug.Log($"File compiled to: {outputPath}");
				}

				if (Array.IndexOf(inputFiles, file) == inputFiles.Length - 1) {
					if (batch) {
						outputPath = Directory.GetParent(outputPath).ToString();
					}
					EditorUtility.DisplayDialog("Success", $"Files successfully compiled to: {outputPath}", "OK");
				}

			} catch (Exception ex) {
				Debug.LogError($"Error processing {file}: {ex.Message}");

				string message = batch
					? $"Failed to process: {file}\nError: {ex.Message}"
					: $"An error occurred while compiling the file:\n{ex.Message}";

				EditorUtility.DisplayDialog("Error", message, "OK");

				if (batch) return;
			}
		}
	}
}
