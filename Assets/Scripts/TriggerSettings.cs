using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[AddComponentMenu("Vectorier/Trigger Settings")]
public class TriggerSettings : MonoBehaviour
{
	[TextArea(3, 10)]
	public string Content = @"<Init>
	<SetVariable Name=""$Active"" Value=""1""/>
	<SetVariable Name=""$Node"" Value=""COM""/>
	<SetVariable Name=""$AI"" Value=""0""/>
	<SetVariable Name=""Flag1"" Value=""0""/>
</Init>
<Template Name=""Control""/>";
}

[CustomEditor(typeof(TriggerSettings))]
public class TriggerSettingsEditor : Editor
{
	private string templatesDirectory;

	private List<string> templatePaths = new List<string>();
	private List<string> templateLabels = new List<string>();
	private int selectedTemplateIndex = 0;

	private static Dictionary<string, System.DateTime> lastKnownTemplateFiles = new Dictionary<string, System.DateTime>();
	private static double lastCheckTime = 0;

	private void OnEnable()
	{
		templatesDirectory = Path.Combine(Application.dataPath, "Scripts", "TriggerSettings", "Templates");
		LoadTemplatePaths();
		EditorApplication.update += OnEditorUpdate;
	}

	private void OnDisable()
	{
		EditorApplication.update -= OnEditorUpdate;
	}

	private void OnEditorUpdate()
	{
		if (EditorApplication.timeSinceStartup - lastCheckTime < 1)
		{
			return;
		}

		lastCheckTime = EditorApplication.timeSinceStartup;

		if (!Directory.Exists(templatesDirectory))
		{
			return;
		}

		var currentFiles = Directory.GetFiles(templatesDirectory, "*.xml", SearchOption.AllDirectories)
			.ToDictionary(path => path, path => File.GetLastWriteTimeUtc(path));

		bool changed = false;

		// Check if any file was added or modified
		foreach (var kvp in currentFiles)
		{
			if (!lastKnownTemplateFiles.TryGetValue(kvp.Key, out var lastWrite) || kvp.Value != lastWrite)
			{
				changed = true;
				break;
			}
		}

		// Check if any file was removed
		if (!changed && lastKnownTemplateFiles.Count != currentFiles.Count)
		{
			changed = true;
		}

		if (changed)
		{
			lastKnownTemplateFiles = currentFiles;
			LoadTemplatePaths();
		}
	}

	public override void OnInspectorGUI()
	{
		var script = (TriggerSettings)target;
		var style = new GUIStyle(EditorStyles.textArea);

		int lineCount = Mathf.Max(5, script.Content?.Split('\n').Length ?? 0);
		float multiplier = 0.85f;
		float lineHeight = EditorGUIUtility.singleLineHeight * multiplier;
		float height = lineCount * lineHeight;

		GUIContent label = new GUIContent("Content", "Content of the Trigger Content subnode.");
		GUILayout.Label(label);

		Undo.RecordObject(script, "Edit Trigger Content");

		string newContent = EditorGUILayout.TextArea(script.Content, style, GUILayout.Height(height));
		if (newContent != script.Content)
		{
			script.Content = newContent;
			EditorUtility.SetDirty(script);
		}

		GUILayout.Space(10);

		if (templatePaths.Count == 0)
		{
			LoadTemplatePaths();
		}

		EditorGUILayout.BeginHorizontal();

		GUIContent dropdownLabel = new GUIContent("Template", "Select a template to load");
		GUILayout.Label(dropdownLabel, GUILayout.Width(100));

		selectedTemplateIndex = EditorGUILayout.Popup(
			selectedTemplateIndex,
			templateLabels.ToArray(),
			GUILayout.Height(20)
		);

		GUILayout.Space(10);

		GUIContent refreshButton = new GUIContent("↻", "Refresh template list");
		if (GUILayout.Button(refreshButton, GUILayout.Width(25)))
		{
			LoadTemplatePaths();
			GUI.FocusControl(null);
		}

		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Load Template", GUILayout.Height(25)))
		{
			string fullPath = Path.Combine(templatesDirectory, templatePaths[selectedTemplateIndex]);

			if (File.Exists(fullPath))
			{
				string previousContent = script.Content;

				Undo.RecordObject(script, "Load XML Template");
				script.Content = File.ReadAllText(fullPath);

				if (script.Content != previousContent)
				{
					EditorUtility.SetDirty(script);
				}

				GUI.FocusControl(null);
			}
			else
			{
				Debug.LogWarning($"Template file not found: {fullPath}");
			}
		}

		GUILayout.Space(3);

		GUIContent cleanupContent = new GUIContent("Cleanup", "Beautify and format the XML content.");
		if (GUILayout.Button(cleanupContent, GUILayout.Height(37)))
		{
			Undo.RecordObject(script, "Cleanup");
			try
			{
				script.Content = BeautifyXml(script.Content);
				script.Content = BeautifyXml(script.Content); // double pass
				LoadTemplatePaths();
			}
			catch
			{
				// pass
			}
			EditorUtility.SetDirty(script);
			GUI.FocusControl(null);
		}
	}

	private void LoadTemplatePaths()
	{
		templatePaths.Clear();
		templateLabels.Clear();

		if (Directory.Exists(templatesDirectory))
		{
			var files = Directory.GetFiles(templatesDirectory, "*.xml", SearchOption.AllDirectories);

			foreach (var path in files.OrderBy(p => p))
			{
				string relativePath = path.Replace(templatesDirectory + Path.DirectorySeparatorChar, "").Replace("\\", "/");
				templatePaths.Add(relativePath);

				string noExt = Path.ChangeExtension(relativePath, null);
				string[] parts = noExt.Split('/');
				string label = string.Join(" / ", parts.Select(p =>
					System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(p.Replace("_", " ").ToLower())
				));
				templateLabels.Add(label);
			}

			// Update the file tracking map
			lastKnownTemplateFiles = files.ToDictionary(path => path, path => File.GetLastWriteTimeUtc(path));
		}
		else
		{
			templatePaths.Add("< select template >");
			templateLabels.Add("< select template >");
		}
	}

	private string BeautifyXml(string rawXml)
	{
		try
		{
			var lines = rawXml.Split('\n');
			var trimmedLines = lines.Select(line => line.Trim());

			var filteredLines = trimmedLines.Where(line =>
				line != "<" &&
				line != ">" &&
				!line.Contains("<!--") &&
				!line.Contains("-->") &&
				!line.Contains("<Events></Events") &&
				!line.Contains("<Events><Events/>") &&
				!line.Contains("<Events/>") &&
				!line.Contains("<Conditions></Conditions>") &&
				!line.Contains("<Conditions><Conditions/>") &&
				!line.Contains("<Conditions/>") &&
				!line.Contains("<Actions></Actions>") &&
				!line.Contains("<Actions><Actions/>") &&
				!line.Contains("<Actions/>")
			);

			rawXml = string.Join("\n", filteredLines).Trim();

			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml($"<Root>{rawXml}</Root>");

			string unformattedXml;
			var flatSettings = new XmlWriterSettings
			{
				Indent = false,
				NewLineHandling = NewLineHandling.None,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			};

			using (var flatWriter = new StringWriter())
			using (var xmlFlat = XmlWriter.Create(flatWriter, flatSettings))
			{
				foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
				{
					node.WriteTo(xmlFlat);
				}
				xmlFlat.Flush();
				unformattedXml = flatWriter.ToString();
			}

			var reDoc = new XmlDocument();
			reDoc.LoadXml($"<Root>{unformattedXml}</Root>");

			var beautifySettings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineHandling = NewLineHandling.None,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			};

			using (var prettyWriter = new StringWriter())
			using (var xmlPretty = XmlWriter.Create(prettyWriter, beautifySettings))
			{
				foreach (XmlNode node in reDoc.DocumentElement.ChildNodes)
				{
					node.WriteTo(xmlPretty);
				}
				xmlPretty.Flush();
				return prettyWriter.ToString().Replace(" />", "/>");
			}
		}
		catch (XmlException ex)
		{
			Debug.LogWarning("XML Beautify failed: " + ex.Message);
			return rawXml;
		}
	}

}
