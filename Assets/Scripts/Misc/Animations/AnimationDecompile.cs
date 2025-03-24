using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

using Vectorier;

using Vector3 = UnityEngine.Vector3;

// -=-=-=- //

public class AnimationDecompile : MonoBehaviour {
	[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN")]
	public static void AnimationDecompile_Singleton() {
		AnimationDecompileWrite(false);
	}

	[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN (Batch)")]
	public static void AnimationDecompile_Batch() {
		AnimationDecompileWrite(true);
	}

	public static void AnimationDecompileWrite(bool batch) {
		string cwd = Application.dataPath;
		string pardir = Path.Combine(VectorierSettings.GameDirectory, "animations");
		string inputPath;
		string[] inputFiles;

		if (pardir == null) {
			pardir = Directory.GetParent(cwd).ToString();
		}

		if (!batch) {
			inputPath = EditorUtility.OpenFilePanel("Select .bin file", pardir, "bin");
			if (string.IsNullOrEmpty(inputPath)) return;
			inputFiles = new string[] { inputPath };
		} else {
			inputPath = EditorUtility.OpenFolderPanel("Select directory containing .bin files", pardir, "");
			if (string.IsNullOrEmpty(inputPath)) return;
			inputFiles = Directory.GetFiles(inputPath, "*.bin");
		}

		foreach (string file in inputFiles) {
			try {
				string outputPath = Path.ChangeExtension(file, ".xml");
				XmlDocument outputFileContent = Animations.Parse(file);

				using (StringWriter stringWriter = new StringWriter()) {
					using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter) {
						Formatting = Formatting.Indented,
						Indentation = 1,
						IndentChar = '\t'
					}) {
						outputFileContent.WriteTo(xmlTextWriter);
					}

					string formattedXml = stringWriter.ToString().Replace(" />", "/>");
					File.WriteAllText(outputPath, formattedXml, Encoding.UTF8);
				}

				outputPath = outputPath
					.Substring(cwd.Length + 1)
					.Replace(Path.DirectorySeparatorChar, '/')
					.Trim('/');

				if (!batch) {
					Debug.Log($"File written to: {outputPath}");
				}

				if (Array.IndexOf(inputFiles, file) == inputFiles.Length - 1) {
					if (batch) {
						outputPath = Directory.GetParent(outputPath).ToString();
					}
					EditorUtility.DisplayDialog("Success", $"Files successfully written to: {outputPath}", "OK");
				}
				
			} catch (Exception ex) {
				Debug.LogError($"Error processing {file}: {ex.Message}");

				string message = batch 
					? $"Failed to process: {file}\nError: {ex.Message}"
					: $"An error occurred while processing the file:\n{ex.Message}";

				EditorUtility.DisplayDialog("Error", message, "OK");

				if (batch) return;
			}
		}
	}
}