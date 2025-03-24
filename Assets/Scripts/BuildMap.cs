using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using Vectorier;

using Debug = UnityEngine.Debug;

// -=-=-=- //

public class BuildMap : MonoBehaviour
{
	// ReSharper disable once InconsistentNaming
	internal string vectorFilePath { get; set; }

	void Awake() 
	{
		vectorFilePath = VectorierSettings.GameDirectory;
	}

	public static event Action MapBuilt;

	// Flag to indicate if the build is for running the game
	public static bool IsBuildForRunGame { get; set; } = false;

	// -=-=-=- //

	// Level Settings
	[Header("Level Settings")]

	// -=-=-=- //

	[Tooltip(@"Level that will get overridden.

⚠️ Will throw an error if given XML will not be detected as a level!")]
	public string mapToOverride = "DOWNTOWN_STORY_02";
	// public string mapToOverride = "DOWNTOWN_STORY_01";

	[Tooltip("Music that will be played on the level.")]
	public string levelMusic = "music_dinamic";

	[Tooltip("Volume of the music.")]
	public string MusicVolume = "0.3";

	[Tooltip(@"Background image.

If empty, inherited from ""customBackgroundMirror"" variable.

⚠️ Has to be located inside “track_content_2048.dz” file archive base!")]
	public string customBackground = "v_bg";

	[Tooltip(@"Background image for left side. (even nodes)

If empty, inherited from ""customBackground"" variable.

⚠️ Has to be located inside “track_content_2048.dz” file archive base!")]
	public string customBackgroundMirror = "";

	[Tooltip("Background width.")]
	public float backgroundWidth = 2121f;

	[Tooltip("Background height.")]
	public float backgroundHeight = 1116f;

	[Tooltip(@"Background horizontal position + 1120. (offset)

⚠️ Affects mobile gameplay, use with caution.")]
	public float backgroundXPosition = 0f;

	[Tooltip(@"Background vertical position.

⚠️ Affects mobile gameplay, use with caution.")]
	public float backgroundYPosition = 0f;

	// Gameplay
	[Serializable]
	public class PlayerSettings
	{
		public string playerModelName = "Player";

		[Tooltip("Player's spawn name.")]
		public string playerSpawnName = "PlayerSpawn";

		[Tooltip("Duration until the player appears, in seconds.")]
		public float playerSpawnTime;

		[Tooltip("Time after player disappears after death (Default: 3)")]
		public float playerLifeTime = 3;

		[Tooltip(@"Player Appearance (default: “1”)

Accepts multiline entries, pipe characters are treated as newlines, then newlines are treated like pipes.

Lines starting with “>” mean comments. Empty lines are removed.")]
		[TextArea(3, 8)]
		[FormerlySerializedAs("playerSkin")]
		public string playerSkins = "1";
	}
	[Serializable]
	public class HunterSettings
	{
		public string hunterModelName = "Hunter";

		[Tooltip("Hunter's spawn name.")]
		public string hunterSpawnName = "DefaultSpawn";

		[Tooltip("Duration until the hunter appears, in seconds.")]
		public float hunterSpawnTime = 2f;

		[Tooltip("Time after hunter disappears after death (Default: 3)")]
		public float hunterLifeTime = 5f;

		[Tooltip(@"Time (ms) until hunter arrests the player when $Node variable content node is located inside the `TriggerCatch` or `TriggerCatchFront` objects (Default: 300)

⚠️ Not applicable to `TriggerCatchFast`.")]
		public int hunterCatchDistance = 300;

		[Tooltip("Hunter respawn name.")]
		public string hunterAllowedSpawn = "Respawn";

		[Tooltip(@"Hunter model appearance (default: “hunter”)

Accepts multiline entries, pipe characters are treated as newlines, then newlines are treated like pipes.

Lines starting with “>” mean comments. Empty lines are removed.")]
		[TextArea(3, 8)]
		[FormerlySerializedAs("hunterSkin")]
		public string hunterSkins = "hunter";

		[Tooltip("Determines whether hunter is able do to tricks.")]
		public bool hunterTrickAllowed;

		[Tooltip("Determines whether hunter's icon is shown.")]
		public bool hunterIcon = true;

		[Tooltip("Hunter AI type number (Default: 1)")]
		public int hunterAIType = 1;
	}

	[Header("Gameplay (Common Mode)")]
	[SerializeField]
	private PlayerSettings Player;

	[SerializeField]
	private HunterSettings Hunter;

	[Tooltip(@"Uses custom properties instead of prefixed

⚠️ Ignores the above settings for player and hunter!")]
	public bool useCustomProperties;

	[TextArea(5, 20)]
	public string CustomModelProperties = @"<Model
	Name=""Player""
	Type=""1""
	Color=""0""
	BirthSpawn=""PlayerSpawn""
	AI=""0""
	Time=""0""
	Skins=""1""
	Respawns=""Hunter""
	ForceBlasts=""Hunter""
	Trick=""1""
	Item=""1""
	Victory=""1""
	Lose=""1""
	LifeTime=""3""
/>

<Model
	Name=""Hunter""
	Type=""0""
	Color=""0""
	BirthSpawn=""DefaultSpawn""
	AI=""1""
	Time=""0.8""
	AllowedSpawns=""Respawn""
	Skins=""hunter""
	Murders=""Player|Helper""
	Arrests=""Player""
	Icon=""1""
	LifeTime=""3""
/>

<!-- Uncomment those lines to add more models --/>

<!-- Model
	Name=""Hunter2""
	Type=""0""
	Color=""0""
	BirthSpawn=""DefaultSpawn""
	AI=""2""
	Time=""0.8""
	AllowedSpawns=""Respawn""
	Skins=""hunter""
	Murders=""Player|Helper""
	Arrests=""Player""
	Icon=""1""
	LifeTime=""3""
--/>

<!--Model
	Name=""Helper""
	Type=""0""
	Color=""0""
	BirthSpawn=""HelperSpawn""
	AI=""3""
	Time=""0.3""
	AllowedSpawns=""RespawnHelper""
	Skins=""revolution_girl""
	Trick=""0""
	Item=""0""
	Victory=""0""
	Lose=""0""
	LifeTime=""3""
--/>";


	// Miscellaneous
	[Header("Miscellaneous")]

	[Tooltip("Outputs objects writing to console while building the map.")]
	public bool debugObjectWriting;

	public bool hunterPlaced;

	[Tooltip("Divide GameObject's position by it's layer object factor.")]
	public bool correctFactorPosition = true;

	// -=-=-=- //

	[MenuItem("Vectorier/BuildMap")]
	public static void BuildDZ() { Build(true, true); }

	[MenuItem("Vectorier/BuildMap (Fast) #&B")]
	public static void BuildZlib() { Build(false, true); }

	[MenuItem("Vectorier/BuildMap XML Only")]
	public static void BuildXml() { Build(false, false); }

	// -=-=-=- //
	// Variables
	public string globalRegex = @" ?\((.*?)\)| ?\[.*?\]";

	// -=-=-=- //

	public static bool IsVisible(GameObject obj)
	{
		return !obj.CompareTag("EditorOnly") && 
			!obj.CompareTag("Unused") && 
			!SceneVisibilityManager.instance.IsHidden(obj) && 
			obj.activeInHierarchy;
	}

	public object ParseSkins(string SkinsInput, bool list = false)
	{
		// Split input by newline and remove any carriage returns
		List<string> SkinsList = SkinsInput
			.Replace(Path.DirectorySeparatorChar, '/')
			.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Trim('\r'))
			.ToList();

		// Split each entry by "|" and collect all parts
		List<string> splitList = new List<string>();

		foreach (var line in SkinsList)
		{
			splitList.AddRange(line.Split('|'));
		}

		// Remove empty entries
		splitList = splitList.Where(elem => !string.IsNullOrWhiteSpace(elem)).ToList();

		// Remove entries starting with ">"
		splitList = splitList.Where(elem => !elem.StartsWith(">")).ToList();

		if (list)
		{
			return splitList;
		}

		// Join all remaining entries with pipe
		return string.Join("|", splitList);
	}

	public static void Build(bool useDZ, bool compileMap)
	{
		// This is used to cache the BuildMap component. This is done to avoid the FindObjectOfType method in loop and other places.
		// This is a slow operation.
		var buildMap = FindObjectOfType<BuildMap>();

		#if UNITY_EDITOR

		UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

		#endif

		if (string.IsNullOrEmpty(buildMap.vectorFilePath))
		{
			buildMap.vectorFilePath = VectorierSettings.GameDirectory;
		}
		Debug.Log("Building...");

		// Start the stopwatch
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();

		// -=-=-=- //

		// Erase last build
		File.Delete(Application.dataPath + "/XML/build-map.xml");
		File.Copy(Application.dataPath + "/XML/empty-map-DONT-MODIFY.xml", Application.dataPath + "/XML/build-map.xml");

		// Open the object.xml
		XmlDocument xml = new XmlDocument();
		xml.Load(Application.dataPath + "/XML/build-map.xml");

		XmlNode rootNode = xml.DocumentElement.SelectSingleNode("/Root/Track");

		// Search for the selected object in the object.xml
		foreach (XmlNode node in rootNode)
		{
			string factorValue = node.Attributes.GetNamedItem("Factor").Value;

			if (factorValue == "1")
			{
				// Set the properties into the level
				buildMap.SetLevelProperties(xml, node);

				// Get all GameObjects with tag "Image", then arrange them based on sorting order
				GameObject[] imagesInScene = GameObject.FindGameObjectsWithTag("Image")
											.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
											.ToArray();

				// Write every GameObject with tag "Object", "Image", "Platform", "Area" and "Trigger" in the build-map.xml
				foreach (GameObject spawnInScene in GameObject.FindGameObjectsWithTag("Spawn"))
				{
					UnityEngine.Transform parent = spawnInScene.transform.parent;
					if (parent != null && parent.CompareTag("Object") && !IsVisible(spawnInScene))
					{
						continue;
					}

					XmlElement spawnNode;
					buildMap.ConvertToSpawn(node, xml, spawnInScene, out spawnNode, false);

					// Check if spawnNode is not null before appending
					if (spawnNode != null)
					{
						node.FirstChild.AppendChild(spawnNode);
						xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
					}
				}

				// Image
				foreach (GameObject imageInScene in imagesInScene)
				{
					UnityEngine.Transform parent = imageInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(imageInScene))
					{
						continue;
					}

					XmlElement imageNode;
					buildMap.ConvertToImage(node, xml, imageInScene, out imageNode, false);

					// Place it into the Object node
					node.FirstChild.AppendChild(imageNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Object
				foreach (GameObject objectInScene in GameObject.FindGameObjectsWithTag("Object"))
				{
					if (!IsVisible(objectInScene))
					{
						continue;
					}

					XmlElement objectNode;
					buildMap.ConvertToObject(node, xml, objectInScene, out objectNode, false);
					node.FirstChild.AppendChild(objectNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				foreach (GameObject itemInScene in GameObject.FindGameObjectsWithTag("Item"))
				{
					UnityEngine.Transform parent = itemInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(itemInScene))
					{
						continue;
					}

					XmlElement itemNode;
					buildMap.ConvertToItem(node, xml, itemInScene, out itemNode, false);
					node.FirstChild.AppendChild(itemNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Platforms
				foreach (GameObject platformInScene in GameObject.FindGameObjectsWithTag("Platform"))
				{
					UnityEngine.Transform parent = platformInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(platformInScene))
					{
						continue;
					}

					XmlElement platformNode;
					buildMap.ConvertToPlatform(node, xml, platformInScene, out platformNode, false);
					node.FirstChild.AppendChild(platformNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Trapezoid
				foreach (GameObject trapezoidInScene in GameObject.FindGameObjectsWithTag("Trapezoid"))
				{
					UnityEngine.Transform parent = trapezoidInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(trapezoidInScene))
					{
						continue;
					}

					XmlElement trapezoidNode;
					buildMap.ConvertToTrapezoid(node, xml, trapezoidInScene, out trapezoidNode, false);
					node.FirstChild.AppendChild(trapezoidNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Trigger
				foreach (GameObject triggerInScene in GameObject.FindGameObjectsWithTag("Trigger"))
				{
					UnityEngine.Transform parent = triggerInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(triggerInScene))
					{
						continue;
					}

					XmlElement triggerNode;
					buildMap.ConvertToTrigger(node, xml, triggerInScene, out triggerNode, false);
					node.FirstChild.AppendChild(triggerNode);

					// Apply the modification to the build-map.xml with proper format
					XmlWriterSettings settings = new XmlWriterSettings
					{
						Indent = true,
						IndentChars = "  ",
						NewLineChars = "\r\n",
						NewLineHandling = NewLineHandling.Replace
					};

					using (XmlWriter writer = XmlWriter.Create(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml", settings))
					{
						xml.Save(writer);
					}
				}

				// Area
				foreach (GameObject areaInScene in GameObject.FindGameObjectsWithTag("Area"))
				{
					UnityEngine.Transform parent = areaInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(areaInScene))
					{
						continue;
					}

					XmlElement areaNode;
					buildMap.ConvertToArea(node, xml, areaInScene, out areaNode, false);
					node.FirstChild.AppendChild(areaNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Model
				foreach (GameObject modelInScene in GameObject.FindGameObjectsWithTag("Model"))
				{
					UnityEngine.Transform parent = modelInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(modelInScene))
					{
						continue;
					}

					XmlElement modelNode;
					buildMap.ConvertToModel(node, xml, modelInScene, out modelNode, false);
					node.FirstChild.AppendChild(modelNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Camera
				foreach (GameObject camInScene in GameObject.FindGameObjectsWithTag("Camera"))
				{
					if (!IsVisible(camInScene))
					{
						continue;
					}

					// Note: This is actually a trigger, but with camera zoom properties
					XmlElement cameraNode;
					buildMap.ConvertToCamera(node, xml, camInScene, out cameraNode, false);
					node.FirstChild.AppendChild(cameraNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Dynamic
				foreach (GameObject dynamicInScene in GameObject.FindGameObjectsWithTag("Dynamic"))
				{
					if (!IsVisible(dynamicInScene))
					{
						continue;
					}

					XmlElement dynamicNode;
					UnityEngine.Transform dynamicInSceneTransform = dynamicInScene.transform;
					UnityEngine.Transform parent = dynamicInSceneTransform.parent;
					if (parent != null && parent.CompareTag("Dynamic"))
					{
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}
					buildMap.ConvertToDynamic(node, xml, dynamicInScene, dynamicInSceneTransform, out dynamicNode);
					node.FirstChild.AppendChild(dynamicNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Animation
				foreach (GameObject animationInScene in GameObject.FindGameObjectsWithTag("Animation"))
				{
					UnityEngine.Transform parent = animationInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(animationInScene))
					{
						continue;
					}

					XmlElement animNode;
					buildMap.ConvertToAnimation(node, xml, animationInScene, out animNode, false);
					node.FirstChild.AppendChild(animNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}

				// Particle
				foreach (GameObject particleInScene in GameObject.FindGameObjectsWithTag("Particle"))
				{
					UnityEngine.Transform parent = particleInScene.transform.parent;
					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object")) || !IsVisible(particleInScene))
					{
						continue;
					}

					XmlElement particleNode;
					buildMap.ConvertToParticle(node, xml, particleInScene, out particleNode, false);
					node.FirstChild.AppendChild(particleNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");

				}
			}

			// Backdrop

			// Get all GameObjects with tag "Backdrop", then arrange them based on sorting order
			GameObject[] BackdropsInScene = GameObject.FindGameObjectsWithTag("Backdrop")
										.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
										.ToArray();

			// Get all GameObjects with tag "Top Image", then arrange them based on sorting order
			GameObject[] frontimagesInScene = GameObject.FindGameObjectsWithTag("Top Image")
										.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
										.ToArray();

			// Write every GameObject with tag "Backdrop" in the build-map.xml
			if (factorValue == "0.1")
			{
				foreach (GameObject bdInScene in BackdropsInScene)
				{
					SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Factor_0.1")
					{
						XmlElement backdropNode;
						buildMap.ConvertToBackdrop(node, xml, bdInScene, 0.1f, out backdropNode);
						node.FirstChild.AppendChild(backdropNode);

						// Apply the modification to the build-map.xml file}
						xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
					}
				}
			}
			if (factorValue == "0.25")
			{
				foreach (GameObject bdInScene in BackdropsInScene)
				{
					SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Factor_0.25")
					{
						XmlElement backdropNode;
						buildMap.ConvertToBackdrop(node, xml, bdInScene, 0.25f, out backdropNode);
						node.FirstChild.AppendChild(backdropNode);

						// Apply the modification to the build-map.xml file}
						xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
					}
				}
			}
			if (factorValue == "0.5")
			{
				foreach (GameObject bdInScene in BackdropsInScene)
				{
					SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Factor_0.5" || spriteRenderer.sortingLayerName == "Default")
					{
						XmlElement backdropNode;
						buildMap.ConvertToBackdrop(node, xml, bdInScene, 0.5f, out backdropNode);
						node.FirstChild.AppendChild(backdropNode);

						// Apply the modification to the build-map.xml file}
						xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
					}
				}
			}
			if (factorValue == "0.8")
			{
				foreach (GameObject bdInScene in BackdropsInScene)
				{
					SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Factor_0.8")
					{
						XmlElement backdropNode;
						buildMap.ConvertToBackdrop(node, xml, bdInScene, 0.8f, out backdropNode);
						node.FirstChild.AppendChild(backdropNode);

						// Apply the modification to the build-map.xml file}
						xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
					}
				}
			}

			if (factorValue == "1.001")
			{
				foreach (GameObject frontimageInScene in frontimagesInScene)
				{
					XmlElement topimgNode;
					buildMap.ConvertToTopImage(node, xml, frontimageInScene, out topimgNode);
					node.FirstChild.AppendChild(topimgNode);

					// Apply the modification to the build-map.xml file}
					xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml");
				}
			}
		}

		// vv  Build level directly into Vector (sweet !)  vv
		if (compileMap)
		{
			buildMap.StartDzip(useDZ);
			buildMap.hunterPlaced = false;
		}

		// Show Stopwatch
		stopwatch.Stop();
		TimeSpan ts = stopwatch.Elapsed;
		string formattedTime = ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);

		Debug.Log($"Building done! ({formattedTime} seconds)");

		// -=-=-=- //

		// If the build was for running the game, invoke the MapBuilt event
		if (IsBuildForRunGame)
		{
			MapBuilt?.Invoke();

			// Reset the flag after the build
			IsBuildForRunGame = false;
		}
	}

	void StartDzip(bool useDZ)
	{
		// Check if Vector.exe is running - if yes, close it
		Process[] processes = Process.GetProcessesByName("Vector");
		foreach (Process process in processes)
		{
			if (!process.HasExited)
			{
				Debug.LogWarning("Closing Vector (be careful next time)");

				process.Kill();
				process.WaitForExit();
			}
		}

		// Start compressing levels into level_xml.dz
		string batchFileName = useDZ ? "compile-map.bat" : "compile-map-optimized.bat";
		string batchFilePath = Path.Combine(Application.dataPath, "XML/dzip", batchFileName);
		string batchDirectory = Path.GetDirectoryName(batchFilePath);

		if (!File.Exists(batchFilePath))
		{
			Debug.LogError($"Batch file not found: {batchFilePath}");
			return;
		}

		Process batchProcess = new Process
		{
			StartInfo = {
				FileName = batchFilePath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				// Set working directory
				WorkingDirectory = batchDirectory
			}
		};

		// Start the process
		try
		{
			batchProcess.Start();

			// Wait for the process to exit
			batchProcess.WaitForExit();

			// Check exit code if necessary
			if (batchProcess.ExitCode != 0)
			{
				string errorOutput = batchProcess.StandardError.ReadToEnd();
				Debug.LogError($"dzip.exe encountered an error: {errorOutput}");
			}
			else
			{
				// Move the file if the process succeeded
				string sourceFilePath = Path.Combine(Application.dataPath, "XML/dzip/level_xml.dz");
				string destinationFilePath = Path.Combine(vectorFilePath, "level_xml.dz");

				if (File.Exists(sourceFilePath))
				{
					if (File.Exists(destinationFilePath))
					{
						File.Delete(destinationFilePath);
					}

					File.Copy(sourceFilePath, destinationFilePath);
				}
				else
				{
					Debug.LogError("level_xml.dz was not found! Check if your Vector path is correct");
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to start dzip.exe: {e.Message}");
		}
		finally
		{
			// Ensure to close the process resources
			batchProcess.Close();
		}

		// Trigger the event if the build was intended for running the game
		if (IsBuildForRunGame)
		{
			MapBuilt?.Invoke();

			// Reset flag after building
			IsBuildForRunGame = false;
		}
	}

	// -=-=-=-=-=- //

	void SetLevelProperties(XmlDocument xml, XmlNode objectNode)
	{
		// Find all objects
		GameObject[] allObj = FindObjectsOfType<GameObject>();
		XmlNode rootNode = xml.DocumentElement.SelectSingleNode("/Root");

		// Set the background
		XmlNode objNode = xml.SelectSingleNode("/Root/Track/Object[@Factor='0.05']");
		if (objNode != null)
		{
			XmlNode contentNode = objNode.SelectSingleNode("Content");

			if (contentNode != null)
			{
				XmlNodeList imageNodes = contentNode.SelectNodes("Image");

				// Indexing starts at 0 (relatively even)
				for (int i = 0; i < imageNodes.Count; i++)
				{
					XmlNode imageNode = imageNodes[i];

					if (i % 2 == 0)
					{
						imageNode.Attributes["ClassName"].Value = 
							!string.IsNullOrEmpty(customBackground) ? customBackground : 
							(string.IsNullOrEmpty(customBackgroundMirror) ? "defaultBackground" : customBackgroundMirror);
					}
					else
					{
						imageNode.Attributes["ClassName"].Value = 
							!string.IsNullOrEmpty(customBackgroundMirror) ? customBackgroundMirror : 
							(string.IsNullOrEmpty(customBackground) ? "defaultBackground" : customBackground);
					}

					// Set dimensions
					imageNode.Attributes["Width"].Value = backgroundWidth.ToString(CultureInfo.InvariantCulture);
					imageNode.Attributes["Height"].Value = backgroundHeight.ToString(CultureInfo.InvariantCulture);

					imageNode.Attributes["X"].Value = (-3740 + (i * backgroundWidth)).ToString(CultureInfo.InvariantCulture);
					imageNode.Attributes["Y"].Value = (-500 + backgroundYPosition).ToString(CultureInfo.InvariantCulture);
				}
			}
		}

		// Set the music
		if (levelMusic != null)
		{
			XmlNode musicNode = xml.DocumentElement.SelectSingleNode("/Root/Music");
			XmlAttribute musicAttribute = musicNode.Attributes["Name"];
			XmlAttribute musicVolAttribute = musicNode.Attributes["Volume"];
			if (musicAttribute.Value != null)
			{
				musicAttribute.Value = levelMusic;
				musicVolAttribute.Value = MusicVolume;
			}
		}
		else Debug.LogWarning("No music name specified.");


		// Set player, hunter properties
		
		// Loop to see if the object has buildmap component under it
		foreach (GameObject allObjects in allObj)
		{
			BuildMap buildMap = allObjects.GetComponent<BuildMap>();
			if (useCustomProperties)
			{
				foreach (XmlNode modelsNode in rootNode)
				{
					// Search for the models node
					if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode")
					{
						// If there is child node then remove it
						while (modelsNode.HasChildNodes)
						{
							modelsNode.RemoveChild(modelsNode.FirstChild);
						}

						XmlDocument tempDoc = new XmlDocument();
						tempDoc.LoadXml($"<root>{CustomModelProperties}</root>");
						foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes)
						{
							XmlNode importedNode = xml.ImportNode(childNode, true);
							modelsNode.AppendChild(importedNode);
						}
					}
				}
			}
			else if (!useCustomProperties)
			{
				foreach (XmlNode modelsNode in rootNode)
				{
					if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode")
					{
						foreach (XmlNode modelNode in modelsNode.ChildNodes)
						{
							if (modelNode.Attributes["Name"].Value == "Player" || modelNode.Attributes["Name"].Value == Player.playerModelName)
							{
								// Player model name
								modelNode.Attributes["Name"].Value = Player.playerModelName;

								// Spawn time
								modelNode.Attributes["Time"].Value = Player.playerSpawnTime.ToString();

								// Life Time
								(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Player.playerLifeTime.ToString(CultureInfo.InvariantCulture);

								// Spawn name
								modelNode.Attributes["BirthSpawn"].Value = Player.playerSpawnName;

								// Skins
								XmlAttribute playerSkins = xml.CreateAttribute("Skins");

								// Check if hunterSkins are specified
								if (string.IsNullOrEmpty(Player.playerSkins))
								{
									playerSkins.Value = "1";
									Debug.LogWarning("Player skin isn't specified, setting to default");
								}
								else
								{
									playerSkins.Value = ParseSkins(Player.playerSkins) as string;

									if (string.IsNullOrEmpty(playerSkins.Value))
									{
										playerSkins.Value = "blank";
									}
								}
								modelNode.Attributes.Append(playerSkins);

							}
							if (modelNode.Attributes["Name"].Value == "Hunter" || modelNode.Attributes["Name"].Value == Hunter.hunterModelName)
							{
								// Hunter model name
								modelNode.Attributes["Name"].Value = Hunter.hunterModelName;

								// Spawn time
								modelNode.Attributes["Time"].Value = Hunter.hunterSpawnTime.ToString();

								// Life Time
								(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Hunter.hunterLifeTime.ToString(CultureInfo.InvariantCulture);

								// Spawn name
								modelNode.Attributes["BirthSpawn"].Value = Hunter.hunterSpawnName;

								// AI number
								modelNode.Attributes["AI"].Value = Hunter.hunterAIType.ToString();

								// Huntericon
								if (Hunter.hunterIcon)
								{
									modelNode.Attributes["Icon"].Value = "1";
								}
								else
								{
									modelNode.Attributes["Icon"].Value = "0";
								}

								// Skins
								XmlAttribute hunterSkins = xml.CreateAttribute("Skins");

								// Check if hunterSkins are specified
								if (string.IsNullOrEmpty(Hunter.hunterSkins))
								{
									hunterSkins.Value = "hunter";
									Debug.LogWarning("Hunter skin isn't specified, setting to default");
								}
								else
								{
									hunterSkins.Value = ParseSkins(Hunter.hunterSkins) as string;

									if (string.IsNullOrEmpty(hunterSkins.Value))
									{
										hunterSkins.Value = "blank";
									}
								}
								modelNode.Attributes.Append(hunterSkins);

								// Trick

								// Check if hunter is allowed to do trick
								if (Hunter.hunterTrickAllowed)
								{
									XmlAttribute hunterTrick = xml.CreateAttribute("Trick");
									hunterTrick.Value = "1";
									modelNode.Attributes.Append(hunterTrick);
								}
							}
						}
					}
				}
			}
		}
	}

	void ConvertToAnimation(XmlNode node, XmlDocument xml, GameObject animationInScene, out XmlElement animNode, bool localPosition)
	{
		animNode = null;

		// Animation Properties Component
		AnimationProperties AnimationComponent = animationInScene.GetComponent<AnimationProperties>();

		if (animationInScene.name == "Camera")
		{
			return;
		}

		XmlElement animationElement = xml.CreateElement("Animation");

		Vector3 pos = localPosition ? animationInScene.transform.localPosition : animationInScene.transform.position;
		
		animationElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(pos.x));
		animationElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-pos.y));

		animationElement.SetAttribute("Width", AnimationComponent.Width);
		animationElement.SetAttribute("Height", AnimationComponent.Height);

		// Default is 1
		animationElement.SetAttribute("Type", AnimationComponent.Type);

		foreach (var (attribute, value) in new (string, string)[] 
		{ 
			("Direction", AnimationComponent.Direction), 
			("Acceleration", AnimationComponent.Acceleration),
			("Time", AnimationComponent.Time) 
		})
		{
			if (!string.IsNullOrEmpty(value))
			{
				animationElement.SetAttribute(attribute, value);
			}
		}

		animationElement.SetAttribute("ScaleX", AnimationComponent.ScaleX);
		animationElement.SetAttribute("ScaleY", AnimationComponent.ScaleY);

		animationElement.SetAttribute("ClassName", Regex.Replace(animationInScene.name, globalRegex, string.Empty));
		animNode = animationElement;
	}

	void ConvertToTopImage(XmlNode node, XmlDocument xml, GameObject frontimageInScene, out XmlElement topimgNode)
	{
		topimgNode = null;

		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(frontimageInScene.name, globalRegex, string.Empty));

		if (frontimageInScene.name == "Camera")
		{
			return;
		}

		XmlElement ielement = xml.CreateElement("Image");
		XmlElement transformationElement = xml.CreateElement("Transformation");
		XmlElement propertiesElement = xml.CreateElement("Properties");
		XmlElement staticElement = xml.CreateElement("Static");
		XmlElement matrixElement = xml.CreateElement("Matrix");
		SpriteRenderer spriteRenderer = frontimageInScene.GetComponent<SpriteRenderer>();
		DynamicColor dynamicColor = frontimageInScene.GetComponent<DynamicColor>();
		Color color = spriteRenderer.color;

		// Check if SpriteRenderer exists
		if (spriteRenderer != null && spriteRenderer.sprite != null)
		{
			// Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
			float imagePosX = frontimageInScene.transform.position.x * 100;
			float imagePosY = -frontimageInScene.transform.position.y * 100;

			// Get rotation degrees (Z-axis)
			// Normalize to 0-360 degrees
			float rotationAngle = frontimageInScene.transform.eulerAngles.z % 360; 

			// Flipping
			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY)
			{
				rotationAngle = -rotationAngle;
			}

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY)
			{
				// Bounding box size in world space
				Bounds bounds = spriteRenderer.bounds;
				float worldWidth = bounds.size.x * 100;
				float worldHeight = bounds.size.y * 100;

				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;

				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * frontimageInScene.transform.localScale.x;
				float imageHeight = nativeHeight * frontimageInScene.transform.localScale.y;

				// Matrix values
				float A = imageWidth, B = 0, C = 0, D = imageHeight;

				// Cases
				if (rotationAngle == 90)
				{
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				}
				else if (rotationAngle == 180)
				{
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				}
				else if (rotationAngle == 270)
				{
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				}
				else
				{
					// Formula for free rotation
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (spriteRenderer.flipX)
				{
					A = -A;
					C = -C;
				}
				if (spriteRenderer.flipY)
				{
					B = -B;
					D = -D;
				}

				// Calculate bounding box top-left position
				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				// Calculate Tx and Ty
				float Tx = imagePosX - topLeftX;
				float Ty = imagePosY - topLeftY;

				// Setting all of the attributes
				ielement.SetAttribute("X", Math.Round(topLeftX).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Y", Math.Round(topLeftY).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("ClassName", Regex.Replace(frontimageInScene.name, globalRegex, string.Empty));
				ielement.SetAttribute("Width", worldWidth.ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Height", worldHeight.ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("NativeX", nativeWidth.ToString());
				ielement.SetAttribute("NativeY", nativeHeight.ToString());

				// Apply transformation matrix
				matrixElement.SetAttribute("A", A.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("B", B.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("C", C.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("D", D.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Tx", Tx.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Ty", Ty.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				ielement.AppendChild(propertiesElement);
			}
			else
			{
				Bounds bounds = spriteRenderer.sprite.bounds;
				Vector3 scale = frontimageInScene.transform.localScale;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;
				ielement.SetAttribute("X", Math.Round(imagePosX).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Y", Math.Round(imagePosY).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("ClassName", Regex.Replace(frontimageInScene.name, globalRegex, string.Empty));
				ielement.SetAttribute("Width", (width * scale.x).ToString());
				ielement.SetAttribute("Height", (height * scale.y).ToString());
				ielement.SetAttribute("NativeX", width.ToString());
				ielement.SetAttribute("NativeY", height.ToString());
			}

			// Color of the Image
			if (color.r != 1.000 || color.g != 1.000 || color.b != 1.000 || color.a != 1.000)
			{
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				staticElement.AppendChild(colorElement);
				propertiesElement.AppendChild(staticElement);
				ielement.AppendChild(propertiesElement);
			}

			// Dynamic Color Component
			if (dynamicColor != null)
			{
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement colorElement = xml.CreateElement("Color");

				// Transformation Name
				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				// Set ColorStart (StartColor) and ColorFinish (EndColor)
				string startColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.StartColor) + Mathf.RoundToInt(dynamicColor.StartColor.a * 255).ToString("X2");
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.EndColor) + Mathf.RoundToInt(dynamicColor.EndColor.a * 255).ToString("X2");

				colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

				// Calculate Frames (Duration * 60) or 1 if Duration is 0
				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElement.SetAttribute("Frames", frames.ToString());

				transformationElement.AppendChild(colorElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				ielement.AppendChild(propertiesElement);
			}
		}
		topimgNode = ielement;
	}

	void ConvertToSpawn(XmlNode node, XmlDocument xml, GameObject spawnInScene, out XmlElement spawnNode, bool localPosition)
	{
		spawnNode = null;

		// Respawn component
		Respawn RespawnComponent = spawnInScene.GetComponent<Respawn>();
		// Spawn component
		Spawn Spawn = spawnInScene.GetComponent<Spawn>();

		XmlElement spawnElement = xml.CreateElement("Spawn");
		Spawn[] SpawnComponent = FindObjectsOfType<Spawn>();

		if (RespawnComponent != null && Spawn == null)
		{
			// Root
			XmlElement objectElement = xml.CreateElement("Object");
			objectElement.SetAttribute("X", "0");
			objectElement.SetAttribute("Y", "0");

			// Content
			XmlElement contentElement = xml.CreateElement("Content");

			foreach (Spawn spawns in SpawnComponent)
			{
				// Check every game object that has the spawn component
				GameObject gameObjwithSpawnComponent = spawns.gameObject;

				if (RespawnComponent.RespawnName == gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName)
				{
					if (gameObjwithSpawnComponent.GetComponent<Spawn>().RefersToRespawn)
					{
						// Spawn element
						XmlElement spawnInsideElement = xml.CreateElement("Spawn");

						Vector3 position = localPosition ? gameObjwithSpawnComponent.transform.localPosition : gameObjwithSpawnComponent.transform.position;

						// Set X and Y attributes correctly using position.x and position.y
						spawnInsideElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
						spawnInsideElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

						spawnInsideElement.SetAttribute("Name", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName);
						spawnInsideElement.SetAttribute("Animation", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnAnimation);
						contentElement.AppendChild(spawnInsideElement);
					}
				}
			}

			// Trigger element
			XmlElement triggerElement = xml.CreateElement("Trigger");
			triggerElement.SetAttribute("Name", RespawnComponent.TriggerName);

			Vector3 spawnPosition = localPosition ? spawnInScene.transform.localPosition : spawnInScene.transform.position;

			triggerElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(spawnPosition.x));
			triggerElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-spawnPosition.y));

			SpriteRenderer spriteRenderer = spawnInScene.GetComponent<SpriteRenderer>();
			// Get the Sprite Size in Width and Height
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = spawnInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor

				// Width of the Image
				triggerElement.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				triggerElement.SetAttribute("Height", (height * scale.y).ToString());
			}

			// Create the properties element and its child static element
			XmlElement propertiesElement = xml.CreateElement("Properties");
			XmlElement staticElement = xml.CreateElement("Static");
			XmlElement selectionElement = xml.CreateElement("Selection");
			selectionElement.SetAttribute("Choice", "AITriggers");
			selectionElement.SetAttribute("Variant", "CommonMode");

			staticElement.AppendChild(selectionElement);
			propertiesElement.AppendChild(staticElement);
			triggerElement.AppendChild(propertiesElement);

			// Create content element inside trigger element
			XmlElement triggerContentElement = xml.CreateElement("Content");

			// Create the init element and its child setVariable element
			XmlElement initElement = xml.CreateElement("Init");

			float Frames = RespawnComponent.RespawnSecond * 60;

			string[][] setVariables =
			{
				new[] { "Name", "$Active", "Value", "1" },
				new[] { "Name", "$Node", "Value", "COM" },
				new[] { "Name", "Spawn", "Value", RespawnComponent.RespawnName },
				new[] { "Name", "Frames", "Value", Frames.ToString() },
				new[] { "Name", "SpawnModel", "Value", RespawnComponent.Spawnmodel },
				new[] { "Name", "Reversed", "Value", "0" },
				new[] { "Name", "$AI", "Value", "0" },
				new[] { "Name", "Flag1", "Value", "0" },
			};

			// Add each setVariable element to the init element
			foreach (var setVariable in setVariables)
			{
				XmlElement setVariableElement = xml.CreateElement("SetVariable");
				setVariableElement.SetAttribute(setVariable[0], setVariable[1]);
				setVariableElement.SetAttribute(setVariable[2], setVariable[3]);
				initElement.AppendChild(setVariableElement);
			}

			triggerContentElement.AppendChild(initElement);

			// Create template element inside content element
			if (RespawnComponent.RespawnOnScreen)
			{
				XmlElement templateElement = xml.CreateElement("Loop");
				templateElement.SetAttribute("Template", "Respawn_OnScreen.Player");
				XmlElement templateElement2 = xml.CreateElement("Loop");
				templateElement2.SetAttribute("Template", "Respawn_OnScreen.Timeout");
				triggerContentElement.AppendChild(templateElement);
				triggerContentElement.AppendChild(templateElement2);
			}
			else
			{
				XmlElement templateElement = xml.CreateElement("Template");
				templateElement.SetAttribute("Name", "Respawn_OnScreen");
				triggerContentElement.AppendChild(templateElement);
			}

			triggerElement.AppendChild(triggerContentElement);
			contentElement.AppendChild(triggerElement);
			objectElement.AppendChild(contentElement);
			spawnNode = objectElement;
		}
		else if (RespawnComponent == null && Spawn != null)
		{
			if (Spawn.RefersToRespawn == false)
			{
				var position = localPosition ? spawnInScene.transform.localPosition : spawnInScene.transform.position;

				spawnElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
				spawnElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

				spawnElement.SetAttribute("Name", Spawn.SpawnName);
				spawnElement.SetAttribute("Animation", Spawn.SpawnAnimation);

				spawnNode = spawnElement;
			}
		}
	}

	void ConvertToBackdrop(XmlNode node, XmlDocument xml, GameObject bdInScene, float FactorAmount, out XmlElement backdropNode)
	{
		backdropNode = null;

		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(bdInScene.name, globalRegex, string.Empty));

		SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
		DynamicColor dynamicColor = bdInScene.GetComponent<DynamicColor>();
		XmlElement transformationElement = xml.CreateElement("Transformation");

		if (bdInScene.name == "Camera")
		{
			return;
		}

		BuildMap buildMapInstance = FindObjectOfType<BuildMap>();
		Vector3 DefaultPosition = bdInScene.transform.position;

		float positionX = DefaultPosition.x;
		float positionY = DefaultPosition.y;

		if (buildMapInstance != null && buildMapInstance.correctFactorPosition)
		{
			positionX /= (1 / FactorAmount);
			positionY /= (1 / FactorAmount);
		}

		if (spriteRenderer == null)
		{
			XmlElement BD_element = xml.CreateElement("Object");
			BD_element.SetAttribute("Name", Regex.Replace(bdInScene.name, globalRegex, string.Empty));

			BD_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(positionX));
			BD_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-positionY));

			backdropNode = BD_element;
		}
		else if (spriteRenderer.sprite != null)
		{
			XmlElement BD_element = xml.CreateElement("Image");
			XmlElement propertiesElement = xml.CreateElement("Properties");
			XmlElement staticElement = xml.CreateElement("Static");
			XmlElement matrixElement = xml.CreateElement("Matrix");
			Color color = spriteRenderer.color;
			Vector3 scale = bdInScene.transform.localScale;

			float imagePosX = positionX * 100;
			float imagePosY = -positionY * 100;

			// Get rotation degrees (Z-axis)
			// Normalize to 0-360 degrees
			float rotationAngle = bdInScene.transform.eulerAngles.z % 360;

			// Flipping
			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY)
			{
				rotationAngle = -rotationAngle;
			}

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY)
			{
				// Bounding box size in world space
				Bounds bounds = spriteRenderer.bounds;
				float worldWidth = bounds.size.x * 100;
				float worldHeight = bounds.size.y * 100;

				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;

				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * bdInScene.transform.localScale.x;
				float imageHeight = nativeHeight * bdInScene.transform.localScale.y;

				// Matrix values
				float A = imageWidth, B = 0, C = 0, D = imageHeight;

				// Cases
				if (rotationAngle == 90)
				{
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				}
				else if (rotationAngle == 180)
				{
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				}
				else if (rotationAngle == 270)
				{
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				}
				else
				{
					// Formula for free rotation
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (spriteRenderer.flipX)
				{
					A = -A;
					C = -C;
				}
				if (spriteRenderer.flipY)
				{
					B = -B;
					D = -D;
				}

				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				float Tx = imagePosX - topLeftX;
				float Ty = imagePosY - topLeftY;

				// Apply Attributes
				BD_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(topLeftX));
				BD_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(topLeftY));
				BD_element.SetAttribute("ClassName", Regex.Replace(bdInScene.name, globalRegex, string.Empty));
				BD_element.SetAttribute("Width", (worldWidth * scale.x).ToString());
				BD_element.SetAttribute("Height", (worldHeight * scale.y).ToString());
				BD_element.SetAttribute("NativeX", nativeWidth.ToString());
				BD_element.SetAttribute("NativeY", nativeHeight.ToString());

				// Matrix
				matrixElement.SetAttribute("A", A.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("B", B.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("C", C.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("D", D.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Tx", Tx.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Ty", Ty.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				BD_element.AppendChild(propertiesElement);
			}
			else
			{
				Bounds bounds = spriteRenderer.sprite.bounds;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				BD_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(positionX));
				BD_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-positionY));

				BD_element.SetAttribute("ClassName", Regex.Replace(bdInScene.name, globalRegex, string.Empty));
				BD_element.SetAttribute("Width", (width * scale.x).ToString());
				BD_element.SetAttribute("Height", (height * scale.y).ToString());
				BD_element.SetAttribute("NativeX", width.ToString());
				BD_element.SetAttribute("NativeY", height.ToString());
			}

			// Color
			if (color.r != 1.000 || color.g != 1.000 || color.b != 1.000 || color.a != 1.000)
			{
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				staticElement.AppendChild(colorElement);
				propertiesElement.AppendChild(staticElement);
				BD_element.AppendChild(propertiesElement);
			}

			// Dynamic Color Component
			if (dynamicColor != null)
			{
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement colorElement = xml.CreateElement("Color");

				// Transformation Name
				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				// Set ColorStart (StartColor) and ColorFinish (EndColor)
				string startColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.StartColor) + Mathf.RoundToInt(dynamicColor.StartColor.a * 255).ToString("X2");
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.EndColor) + Mathf.RoundToInt(dynamicColor.EndColor.a * 255).ToString("X2");

				colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

				// Calculate Frames (Duration * 60) or 1 if Duration is 0
				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElement.SetAttribute("Frames", frames.ToString());

				transformationElement.AppendChild(colorElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				BD_element.AppendChild(propertiesElement);
			}
			backdropNode = BD_element;
		}
	}

	void ConvertToImage(XmlNode node, XmlDocument xml, GameObject imageInScene, out XmlElement imageNode, bool localPosition)
	{
		imageNode = null;

		if (imageInScene.name == "Camera")
		{
			return;
		}

		if (debugObjectWriting) {
			Debug.Log("Writing object : " + Regex.Replace(imageInScene.name, globalRegex, string.Empty));
		}

		XmlElement ielement = xml.CreateElement("Image");
		XmlElement transformationElement = xml.CreateElement("Transformation");
		XmlElement propertiesElement = xml.CreateElement("Properties");
		XmlElement staticElement = xml.CreateElement("Static");
		XmlElement matrixElement = xml.CreateElement("Matrix");

		SpriteRenderer spriteRenderer = imageInScene.GetComponent<SpriteRenderer>();
		DynamicColor dynamicColor = imageInScene.GetComponent<DynamicColor>();
		Color color = spriteRenderer.color;

		// Check if SpriteRenderer exists
		if (spriteRenderer != null && spriteRenderer.sprite != null)
		{
			// Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
			float imagePosX;
			float imagePosY;

			var position = localPosition ? imageInScene.transform.localPosition : imageInScene.transform.position;

			imagePosX = position.x * 100;
			imagePosY = -position.y * 100;

			// Get rotation degrees (Z-axis)
			// Normalize to 0-360 degrees
			float rotationAngle = imageInScene.transform.eulerAngles.z % 360;

			// Flipping
			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY)
			{
				rotationAngle = -rotationAngle;
			}

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY)
			{
				// Bounding box size in world space
				Bounds bounds = spriteRenderer.bounds;
				float worldWidth = bounds.size.x * 100;
				float worldHeight = bounds.size.y * 100;

				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;

				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * imageInScene.transform.localScale.x;
				float imageHeight = nativeHeight * imageInScene.transform.localScale.y;

				// Matrix values
				float A = imageWidth, B = 0, C = 0, D = imageHeight;

				// Cases
				if (rotationAngle == 90)
				{
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				}
				else if (rotationAngle == 180)
				{
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				}
				else if (rotationAngle == 270)
				{
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				}
				else
				{
					// Formula for free rotation
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (spriteRenderer.flipX)
				{
					A = -A;
					C = -C;
				}
				if (spriteRenderer.flipY)
				{
					B = -B;
					D = -D;
				}

				// Calculate bounding box top-left position
				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				// Calculate Tx and Ty
				float Tx = imagePosX - topLeftX;
				float Ty = imagePosY - topLeftY;

				// Setting all of the attributes
				ielement.SetAttribute("X", Math.Round(topLeftX).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Y", Math.Round(topLeftY).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Width", worldWidth.ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Height", worldHeight.ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("NativeX", nativeWidth.ToString());
				ielement.SetAttribute("NativeY", nativeHeight.ToString());

				// Apply transformation matrix
				matrixElement.SetAttribute("A", A.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("B", B.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("C", C.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("D", D.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Tx", Tx.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
				matrixElement.SetAttribute("Ty", Ty.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				ielement.AppendChild(propertiesElement);
			}
			else
			{
				Bounds bounds = spriteRenderer.sprite.bounds;
				Vector3 scale = imageInScene.transform.localScale;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;
				ielement.SetAttribute("X", Math.Round(imagePosX).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Y", Math.Round(imagePosY).ToString(CultureInfo.InvariantCulture));
				ielement.SetAttribute("Width", (width * scale.x).ToString());
				ielement.SetAttribute("Height", (height * scale.y).ToString());
				ielement.SetAttribute("NativeX", width.ToString());
				ielement.SetAttribute("NativeY", height.ToString());
			}

			string objRegex = Regex.Replace(imageInScene.name, globalRegex, string.Empty);
			string objName = (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : objRegex;
			ielement.SetAttribute("ClassName", objName);

			// Color of the Image
			if (color.r != 1.000 || color.g != 1.000 || color.b != 1.000 || color.a != 1.000)
			{
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				staticElement.AppendChild(colorElement);
				propertiesElement.AppendChild(staticElement);
				ielement.AppendChild(propertiesElement);
			}

			// Dynamic Color Component
			if (dynamicColor != null)
			{
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement colorElement = xml.CreateElement("Color");

				// Transformation Name
				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				// Set ColorStart (StartColor) and ColorFinish (EndColor)
				string startColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.StartColor) + Mathf.RoundToInt(dynamicColor.StartColor.a * 255).ToString("X2");
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.EndColor) + Mathf.RoundToInt(dynamicColor.EndColor.a * 255).ToString("X2");

				colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

				// Calculate Frames (Duration * 60) or 1 if Duration is 0
				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElement.SetAttribute("Frames", frames.ToString());

				transformationElement.AppendChild(colorElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				ielement.AppendChild(propertiesElement);
			}
		}
		imageNode = ielement;
	}

	void ConvertToModel(XmlNode node, XmlDocument xml, GameObject modelInScene, out XmlElement modelNode, bool localPosition)
	{
		modelNode = null;
		if (modelInScene.name == "Camera")
		{
			return;
		}

		ModelProperties modelProperties = modelInScene.GetComponent<ModelProperties>();

		// Create a new node from scratch
		XmlElement Melement = xml.CreateElement("Model");

		var position = localPosition ? modelInScene.transform.localPosition : modelInScene.transform.position;

		Melement.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
		Melement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

		Melement.SetAttribute("Type", "1");

		// Add a name
		Melement.SetAttribute("ClassName", Regex.Replace(modelInScene.name, globalRegex, string.Empty));

		if (modelProperties.UseLifeTime)
		{
			// Add a name
			Melement.SetAttribute("LifeTime", modelProperties.LifeTime);
		}

		modelNode = Melement;
	}

	void ConvertToObject(XmlNode node, XmlDocument xml, GameObject objectInScene, out XmlElement objectNode, bool localPosition)
	{
		objectNode = null;

		// Debug in log every object it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(objectInScene.name, globalRegex, string.Empty));

		if (objectInScene.name != "Camera")
		{
			string name = objectInScene.name;

			// Create a new node from scratch
			XmlElement element = xml.CreateElement("Object");
			if (name != string.Empty)
			{
				// Add a name
				element.SetAttribute("Name", Regex.Replace(name, globalRegex, string.Empty));
			}

			var position = localPosition ? objectInScene.transform.localPosition : objectInScene.transform.position;

			element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			XmlElement contentElement = xml.CreateElement("Content");

			List<GameObject> ImageObjects = new List<GameObject>();

			// Add image to the list
			foreach (UnityEngine.Transform child in objectInScene.transform)
			{
				if (child.gameObject.CompareTag("Image"))
				{
					ImageObjects.Add(child.gameObject);
				}
			}

			// Sort the list based on order in layer
			ImageObjects.Sort((a, b) =>
			{
				SpriteRenderer rendererA = a.GetComponent<SpriteRenderer>();
				SpriteRenderer rendererB = b.GetComponent<SpriteRenderer>();

				// Handle cases where SpriteRenderer might be null
				int orderA = rendererA != null ? rendererA.sortingOrder : 0;
				int orderB = rendererB != null ? rendererB.sortingOrder : 0;

				return orderA.CompareTo(orderB);
			});

			foreach (GameObject imageObject in ImageObjects)
			{
				XmlElement imageNode;
				ConvertToImage(node, xml, imageObject.gameObject, out imageNode, true);
				contentElement.AppendChild(imageNode);
			}

			foreach (UnityEngine.Transform childObject in objectInScene.transform)
			{
				if (childObject.gameObject.CompareTag("Untagged"))
				{
					continue;
				}

				if (childObject.gameObject.CompareTag("Unused"))
				{
					continue;
				}

				// Check if the gameobject has specific tag

				if (childObject.gameObject.CompareTag("Object"))
				{
					XmlElement objectNode1;
					ConvertToObject(node, xml, childObject.gameObject, out objectNode1, true);
					contentElement.AppendChild(objectNode1);
				}

				if (childObject.gameObject.CompareTag("Item"))
				{
					XmlElement itemNode;
					ConvertToItem(node, xml, childObject.gameObject, out itemNode, true);
					contentElement.AppendChild(itemNode);
				}
				else if (childObject.gameObject.CompareTag("Spawn"))
				{
					XmlElement spawnNode;
					ConvertToSpawn(node, xml, childObject.gameObject, out spawnNode, true);
					contentElement.AppendChild(spawnNode);

				}
				else if (childObject.gameObject.CompareTag("Platform"))
				{
					XmlElement platformNode;
					ConvertToPlatform(node, xml, childObject.gameObject, out platformNode, true);
					contentElement.AppendChild(platformNode);

				}
				else if (childObject.gameObject.CompareTag("Trapezoid"))
				{
					XmlElement trapezoidNode;
					ConvertToTrapezoid(node, xml, childObject.gameObject, out trapezoidNode, true);
					contentElement.AppendChild(trapezoidNode);

				}
				else if (childObject.gameObject.CompareTag("Area"))
				{
					XmlElement areaNode;
					ConvertToArea(node, xml, childObject.gameObject, out areaNode, true);
					contentElement.AppendChild(areaNode);
				}

				else if (childObject.gameObject.CompareTag("Trigger"))
				{
					XmlElement triggerNode;
					ConvertToTrigger(node, xml, childObject.gameObject, out triggerNode, true);
					contentElement.AppendChild(triggerNode);

				}

				else if (childObject.gameObject.CompareTag("Model"))
				{
					XmlElement modelNode;
					ConvertToModel(node, xml, childObject.gameObject, out modelNode, true);
					contentElement.AppendChild(modelNode);
				}

				else if (childObject.gameObject.CompareTag("Animation"))
				{
					XmlElement animNode;
					ConvertToAnimation(node, xml, childObject.gameObject, out animNode, true);
					contentElement.AppendChild(animNode);
				}

				// Add content to the object
				element.AppendChild(contentElement);
			}
			objectNode = element;
		}
		else if (objectInScene.name == "Camera")
		{
			// Create a new node from scratch
			XmlElement element = xml.CreateElement("Camera");

			element.SetAttribute("X", Engine.Math.GameUnits.Multiply(objectInScene.transform.position.x));
			element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-objectInScene.transform.position.y));

			objectNode = element;
		}
	}

	// Platform Collision (Invisible block that is collide-able)
	void ConvertToPlatform(XmlNode node, XmlDocument xml, GameObject platformInScene, out XmlElement platformNode, bool localPosition)
	{
		platformNode = null;

		// Use a texture called "collision" which should come with this buildmap update folder.
		if (platformInScene.name == "Camera")
		{
			return;
		}

		// Debug in log every platform it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(platformInScene.name, globalRegex, string.Empty));
		
		// Create a new node from scratch
		XmlElement P_element = xml.CreateElement("Platform");

		var position = localPosition ? platformInScene.transform.localPosition : platformInScene.transform.position;

		P_element.SetAttribute("X", Mathf.Round(position.x * 100f).ToString("F2", CultureInfo.InvariantCulture));
		P_element.SetAttribute("Y", Mathf.Round(-position.y * 100f).ToString("F2", CultureInfo.InvariantCulture));

		// Get the Sprite Size in Width and Height
		SpriteRenderer spriteRenderer = platformInScene.GetComponent<SpriteRenderer>();

		if (spriteRenderer != null && spriteRenderer.sprite != null)
		{
			// Get the bounds of the sprite
			Bounds bounds = spriteRenderer.sprite.bounds;

			// Get the GameObject scale
			Vector3 scale = platformInScene.transform.localScale;

			// Retrieve the image resolution of the sprite
			float width = bounds.size.x * 100;
			float height = bounds.size.y * 100;

			// Round the width and height to the nearest integer
			int finalWidth = Mathf.RoundToInt(width * scale.x);
			int finalHeight = Mathf.RoundToInt(height * scale.y);

			// Set the width and height accordingly to the scale in the editor

			// Width of the Collision
			P_element.SetAttribute("Width", finalWidth.ToString());

			// Height of the Collision
			P_element.SetAttribute("Height", finalHeight.ToString());
		}

		platformNode = P_element;
	}

	// Trapezoid Collision (Slope)
	void ConvertToTrapezoid(XmlNode node, XmlDocument xml, GameObject trapezoidInScene, out XmlElement trapezoidNode, bool localPosition)
	{
		trapezoidNode = null;
		// Debug in log every platform it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(trapezoidInScene.name, globalRegex, string.Empty));

		// Slope Default
		if (Regex.Replace(trapezoidInScene.name, globalRegex, string.Empty) == "trapezoid_type1")
		{
			// Create a new node from scratch
			XmlElement T_element = xml.CreateElement("Trapezoid");

			var position = localPosition ? trapezoidInScene.transform.localPosition : trapezoidInScene.transform.position;

			T_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			T_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			SpriteRenderer spriteRenderer = trapezoidInScene.GetComponent<SpriteRenderer>();
			// Get the Sprite Size in Width and Height
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = trapezoidInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				// Width of the Trapezoid
				T_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Trapezoid
				T_element.SetAttribute("Height", "1");

				// Height1 of the Trapezoid
				T_element.SetAttribute("Height1", (height * scale.y + 1).ToString());
			}

			// Type of the Trapezoid
			T_element.SetAttribute("Type", "1");

			trapezoidNode = T_element;
		}
		else if (Regex.Replace(trapezoidInScene.name, globalRegex, string.Empty) == "trapezoid_type2")
		{
			// Create a new node from scratch
			XmlElement T_element = xml.CreateElement("Trapezoid");

			var position = localPosition ? trapezoidInScene.transform.localPosition : trapezoidInScene.transform.position;

			T_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			T_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			// Get the Sprite Size in Width and Height
			SpriteRenderer spriteRenderer = trapezoidInScene.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;
				
				// Get the GameObject scale
				Vector3 scale = trapezoidInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				
				// Width of the Trapezoid
				T_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Trapezoid
				T_element.SetAttribute("Height", (height * scale.y + 1).ToString());

				// Height1 of the Trapezoid
				T_element.SetAttribute("Height1", "1");

			}

			// Type of the Trapezoid
			T_element.SetAttribute("Type", "2");

			trapezoidNode = T_element;
		}
	}

	void ConvertToTrigger(XmlNode node, XmlDocument xml, GameObject triggerInScene, out XmlElement triggerNode, bool localPosition)
	{
		triggerNode = null;

		// Debug in log every trigger it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(triggerInScene.name, globalRegex, string.Empty));

		if (triggerInScene.name == "Camera")
		{
			return;
		}

		DynamicTrigger dynamicTrigger = triggerInScene.GetComponent<DynamicTrigger>();
		TriggerSettings triggerSettings = triggerInScene.GetComponent<TriggerSettings>();

		if (triggerSettings && dynamicTrigger)
		{
			Debug.LogError($"GameObject '{triggerInScene.name}' cannot contain both TriggerSetting and DynamicTrigger.");
			return;
		}
		else if (!triggerSettings && !dynamicTrigger)
		{
			Debug.LogError($"GameObject '{triggerInScene.name}' must contain at least TriggerSetting or DynamicTrigger.");
			return;
		}

		if (dynamicTrigger != null)
		{
			// Create a new node from scratch
			XmlElement T_element = xml.CreateElement("Trigger");

			// Add an name
			T_element.SetAttribute("Name", Regex.Replace(triggerInScene.name, globalRegex, string.Empty));

			var position = localPosition ? triggerInScene.transform.localPosition : triggerInScene.transform.position;

			// Set X and Y attributes (Refit into the Vector units)
			T_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			T_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			// Get the Sprite Size in Width and Height
			SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;
				
				// Get the GameObject scale
				Vector3 scale = triggerInScene.transform.localScale;

				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Width of the Image
				T_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				T_element.SetAttribute("Height", (height * scale.y).ToString());
			}

			XmlElement initElement = xml.CreateElement("Init");

			// Add SetVariable elements to Init
			XmlElement setVariable1 = xml.CreateElement("SetVariable");
			setVariable1.SetAttribute("Name", "$Active");
			setVariable1.SetAttribute("Value", "1");
			initElement.AppendChild(setVariable1);

			XmlElement setVariable2 = xml.CreateElement("SetVariable");
			setVariable2.SetAttribute("Name", "$AI");
			setVariable2.SetAttribute("Value", dynamicTrigger.AIAllowed.ToString());
			initElement.AppendChild(setVariable2);

			XmlElement setVariable3 = xml.CreateElement("SetVariable");
			setVariable3.SetAttribute("Name", "$Node");
			setVariable3.SetAttribute("Value", string.IsNullOrEmpty(dynamicTrigger.modelNode) ? "COM" : dynamicTrigger.modelNode);
			initElement.AppendChild(setVariable3);

			if (dynamicTrigger.PlaySound)
			{
				XmlElement setVariable4 = xml.CreateElement("SetVariable");
				setVariable4.SetAttribute("Name", "Sound");
				setVariable4.SetAttribute("Value", dynamicTrigger.Sound);
				initElement.AppendChild(setVariable4);
			}

			XmlElement setVariable5 = xml.CreateElement("SetVariable");
			setVariable5.SetAttribute("Name", "Flag1");
			setVariable5.SetAttribute("Value", "0");
			initElement.AppendChild(setVariable5);

			XmlElement triggerContentElement = xml.CreateElement("Content");
			triggerContentElement.AppendChild(initElement);

			XmlElement loopElement = xml.CreateElement("Loop");

			// Create Events element and EventBlock element
			XmlElement eventsElement = xml.CreateElement("Events");
			XmlElement eventBlockElement = xml.CreateElement("EventBlock");
			eventBlockElement.SetAttribute("Template", "FreqUsed." + dynamicTrigger.EventType.ToString());
			eventsElement.AppendChild(eventBlockElement);

			// Append Events to Loop
			loopElement.AppendChild(eventsElement);

			// Create Actions element and ActionBlock element
			XmlElement actionsElement = xml.CreateElement("Actions");

			if (!dynamicTrigger.Reusable) {
				XmlElement actionBlockElement = xml.CreateElement("ActionBlock");
				actionBlockElement.SetAttribute("Template", "FreqUsed.SwitchOff");
				actionsElement.AppendChild(actionBlockElement);
			}

			if (dynamicTrigger.MultipleTransformation)
			{
				XmlElement chooseElement = xml.CreateElement("Choose");
				chooseElement.SetAttribute("Order", dynamicTrigger.Order.ToString());
				chooseElement.SetAttribute("Set", dynamicTrigger.Set.ToString());

				foreach (string transformationName in dynamicTrigger.TransformationNames)
				{
					XmlElement transformElement = xml.CreateElement("Transform");
					transformElement.SetAttribute("Name", transformationName);
					chooseElement.AppendChild(transformElement);
				}

				actionsElement.AppendChild(chooseElement);
			}
			else
			{
				XmlElement transformElement = xml.CreateElement("Transform");
				transformElement.SetAttribute("Name", dynamicTrigger.TriggerTransformName);
				actionsElement.AppendChild(transformElement);
			}

			if (dynamicTrigger.PlaySound)
			{
				// Create Actionsblock sound
				XmlElement actionBlockSoundElement = xml.CreateElement("ActionBlock");
				actionBlockSoundElement.SetAttribute("Template", "CommonLib.Sound");
				actionsElement.AppendChild(actionBlockSoundElement);
			}

			// Append Actions to Loop
			loopElement.AppendChild(actionsElement);

			// Append Loop to Trigger
			triggerContentElement.AppendChild(loopElement);

			// Append Content to Trigger
			T_element.AppendChild(triggerContentElement);

			triggerNode = T_element;
		}

		// Checks if the trigger has a setting component
		if (triggerSettings != null)
		{
			// Create a new node from scratch
			XmlElement T_element = xml.CreateElement("Trigger");

			// Add an name
			T_element.SetAttribute("Name", Regex.Replace(triggerInScene.name, globalRegex, string.Empty));

			var position = localPosition ? triggerInScene.transform.localPosition : triggerInScene.transform.position;

			T_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			T_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			// Get the Sprite Size in Width and Height
			SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = triggerInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				// Width of the Image
				T_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				T_element.SetAttribute("Height", (height * scale.y).ToString());

				// Create the content node and add it to the trigger node
				XmlElement contentElement = xml.CreateElement("Content");

				// XML doesn't format correctly so we load them into a separate doc
				XmlDocument tempDoc = new XmlDocument();
				tempDoc.LoadXml("<Content>" + triggerSettings.Content + "</Content>");
				foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes)
				{
					XmlNode importedNode = xml.ImportNode(childNode, true);
					contentElement.AppendChild(importedNode);
				}

				T_element.AppendChild(contentElement);

				// Place it into the Object node
				triggerNode = T_element;
			}
		}
	}

	void ConvertToItem(XmlNode node, XmlDocument xml, GameObject itemInScene, out XmlElement itemNode, bool localPosition)
	{
		itemNode = null;

		if (debugObjectWriting)
		{
			Debug.Log("Writing object : " + Regex.Replace(itemInScene.name, globalRegex, string.Empty));
		}

		if (itemInScene.name == "Camera")
		{
			return;
		}

		ItemProperties itemProperties = itemInScene.GetComponent<ItemProperties>();
		XmlElement I_element = xml.CreateElement("Item");

		// Determine the position (local or world)
		float xPosition = localPosition ? itemInScene.transform.localPosition.x : itemInScene.transform.position.x;
		float yPosition = localPosition ? itemInScene.transform.localPosition.y : itemInScene.transform.position.y;

		// Add X and Y positions (Refit into the Vector units, negative Y for world upside down)
		I_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(xPosition));
		I_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-yPosition));

		if (itemProperties != null)
		{
			// Set properties from ItemProperties
			I_element.SetAttribute("Type", itemProperties.Type.ToString());
			I_element.SetAttribute("Score", itemProperties.Score.ToString());
			I_element.SetAttribute("Radius", itemProperties.Radius.ToString());

			if (itemProperties.GroupId != 0)
			{
				I_element.SetAttribute("GroupId", itemProperties.GroupId.ToString());
			}
		}
		else
		{
			// Set default properties if no ItemProperties component exists
			I_element.SetAttribute("Type", "1");
			I_element.SetAttribute("Score", "10");
			I_element.SetAttribute("Radius", "80");
			I_element.SetAttribute("GroupId", "1");
		}

		// Assign the final XmlElement to itemNode
		itemNode = I_element;
	}

	void ConvertToArea(XmlNode node, XmlDocument xml, GameObject areaInScene, out XmlElement areaNode, bool localPosition)
	{
		areaNode = null;

		if (areaInScene.name == "Camera")
		{
			return;
		}

		// Debug in log every Area it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(areaInScene.name, globalRegex, string.Empty));

		if (areaInScene.name == "TriggerCatch" || areaInScene.name == "TriggerCatchFront")
		{
			// Create a new node from scratch
			XmlElement A_element = xml.CreateElement("Area");
				
			// Add an name
			A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, globalRegex, string.Empty));

			var position = localPosition ? areaInScene.transform.localPosition : areaInScene.transform.position;

			A_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			A_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			// Get the Sprite Size in Width and Height
			SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();

			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = areaInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				// Width of the Image
				A_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				A_element.SetAttribute("Height", (height * scale.y).ToString());
			}

			A_element.SetAttribute("Type", "Catch");
			A_element.SetAttribute("Distance", Hunter.hunterCatchDistance.ToString(CultureInfo.InvariantCulture));
			areaNode = A_element;
		}
		else if (areaInScene.name == "TriggerCatchFast")
		{
			// Create a new node from scratch
			XmlElement A_element = xml.CreateElement("Area");

			// Add an name
			A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, globalRegex, string.Empty));

			var position = localPosition ? areaInScene.transform.localPosition : areaInScene.transform.position;

			A_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			A_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			// Get the Sprite Size in Width and Height
			SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = areaInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				// Width of the Image
				A_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				A_element.SetAttribute("Height", (height * scale.y).ToString());

			}
			// Type="Catch"/>
			A_element.SetAttribute("Type", "Catch");

			// Distance="0"/>
			A_element.SetAttribute("Distance", "0");
			areaNode = A_element;
		}
		else
		{
			// Create a new node from scratch
			XmlElement A_element = xml.CreateElement("Area");

			// Add an name
			A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, globalRegex, string.Empty));

			var position = localPosition ? areaInScene.transform.localPosition : areaInScene.transform.position;

			A_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(position.x));
			A_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-position.y));

			SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();
			// Get the Sprite Size in Width and Height
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = areaInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// Set the width and height accordingly to the scale in the editor
				// Width of the Image
				A_element.SetAttribute("Width", (width * scale.x).ToString());

				// Height of the Image
				A_element.SetAttribute("Height", (height * scale.y).ToString());
			}

			// Type="Catch"/>
			A_element.SetAttribute("Type", "Animation");
			areaNode = A_element;
		}
	}

	void ConvertToCamera(XmlNode node, XmlDocument xml, GameObject camInScene, out XmlElement cameraNode, bool localPosition)
	{
		// Important Note: If the specific TriggerZoom already exists in the object.xml, no need to tag those as Camera. Instead, tag it as an object!
		cameraNode = null;

		if (camInScene.name == "Camera")
		{
			return;
		}

		// Debug in log every Area it writes
		if (debugObjectWriting)
			Debug.Log("Writing object : " + Regex.Replace(camInScene.name, globalRegex, string.Empty));

		SpriteRenderer spriteRenderer = camInScene.GetComponent<SpriteRenderer>();

		// Zoom value from object with tag "Camera" that have CustomZoom component
		CustomZoom customZoomValue = camInScene.GetComponent<CustomZoom>();

		// Get the bounds of the sprite
		Bounds bounds = spriteRenderer.sprite.bounds;

		// Get the GameObject scale
		Vector3 scale = camInScene.transform.localScale;

		// Retrieve the image resolution of the sprite
		float width = bounds.size.x * 100;
		float height = bounds.size.y * 100;

		// Trigger Childs
		XmlElement contentElement = xml.CreateElement("Content");
		XmlElement initElement = xml.CreateElement("Init");

		// Trigger variable
		string[] variableNames = { "$Active", "$Node", "Zoom", "$AI", "Flag1" };
		string[] variableValues = { "1", "COM", customZoomValue.ZoomAmount.ToString(), "0", "0" };

		XmlElement triggerElement = xml.CreateElement("Trigger");
		triggerElement.SetAttribute("Name", Regex.Replace(camInScene.name, globalRegex, string.Empty));
		Vector3 pos = localPosition ? camInScene.transform.localPosition : camInScene.transform.position;
		
		triggerElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(pos.x));
		triggerElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-pos.y));

		// Width of the Image
		triggerElement.SetAttribute("Width", (width * scale.x).ToString());

		// Height of the Image
		triggerElement.SetAttribute("Height", (height * scale.y).ToString());

		// Write <content> and <init> under the trigger node
		for (int i = 0; i < variableNames.Length; i++)
		{
			XmlElement setVariableElement = xml.CreateElement("SetVariable");
			setVariableElement.SetAttribute("Name", variableNames[i]);
			setVariableElement.SetAttribute("Value", variableValues[i]);
			initElement.AppendChild(setVariableElement);
		}

		XmlElement templateElement = xml.CreateElement("Template");
		templateElement.SetAttribute("Name", "CameraZoom");

		// Append elements
		contentElement.AppendChild(initElement);
		contentElement.AppendChild(templateElement);
		triggerElement.AppendChild(contentElement);

		// Append the Trigger element to the XmlDocument
		cameraNode = triggerElement;
	}

	void ConvertToParticle(XmlNode node, XmlDocument xml, GameObject particleInScene, out XmlElement particleNode, bool localPosition)
	{
		particleNode = null;
		if (particleInScene.name == "Camera")
		{
			return;
		}

		ParticleProperties particleProperties = particleInScene.GetComponent<ParticleProperties>();

		// Create a new node from scratch
		XmlElement Pelement = xml.CreateElement("Particle");
		Vector3 pos = localPosition ? particleInScene.transform.localPosition : particleInScene.transform.position;

		Pelement.SetAttribute("X", Engine.Math.GameUnits.Multiply(pos.x));
		Pelement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-pos.y));

		Pelement.SetAttribute("Width", particleProperties.Width.ToString());
		Pelement.SetAttribute("Height", particleProperties.Height.ToString());
		Pelement.SetAttribute("Frame", particleProperties.Frame.ToString());
		Pelement.SetAttribute("Type", particleProperties.Type.ToString());
		Pelement.SetAttribute("ClassName", Regex.Replace(particleInScene.name, globalRegex, string.Empty));

		particleNode = Pelement;
	}

	void ConvertToDynamic(XmlNode node, XmlDocument xml, GameObject dynamicInScene, UnityEngine.Transform dynamicInSceneTransform, out XmlElement dynamicNode)
	{
		BuildMap buildMap = FindObjectOfType<BuildMap>();
		dynamicNode = null;

		// Get all Dynamic components
		Dynamic[] dynamicComponents = dynamicInScene.GetComponents<Dynamic>();
		DynamicColor dynamicColorParent = dynamicInScene.GetComponent<DynamicColor>();

		// Object
		XmlElement objectElement = xml.CreateElement("Object");
		objectElement.SetAttribute("X", "0");
		objectElement.SetAttribute("Y", "0");

		// Properties
		XmlElement propertiesElement = xml.CreateElement("Properties");

		// Dynamic
		XmlElement dynamicElement = xml.CreateElement("Dynamic");

		foreach (var dynamicComponent in dynamicComponents)
		{
			XmlElement transformationElement = xml.CreateElement("Transformation");
			transformationElement.SetAttribute("Name", dynamicComponent.TransformationName);

			XmlElement moveElement = xml.CreateElement("Move");

			// Original MoveIntervals
			List<XmlElement> originalMoveIntervals = new List<XmlElement>();

			// Handle Move Intervals (1 to 16)
			for (int i = 1; i <= 16; i++)
			{
				var movementUsage = dynamicComponent.MovementUsage;
				var moveInterval = GetMoveInterval(dynamicComponent, i); // Get MoveInterval by index

				if (movementUsage != null && movementUsage.UseMovement(i) && moveInterval != null)
				{
					XmlElement moveIntervalElement = xml.CreateElement("MoveInterval");
					moveIntervalElement.SetAttribute("Number", i.ToString());

					int framesToMove = Mathf.Max(1, Mathf.RoundToInt(moveInterval.MoveDuration * 60));
					int delayFrames = Mathf.RoundToInt(moveInterval.Delay * 60);
					moveIntervalElement.SetAttribute("FramesToMove", framesToMove.ToString());
					moveIntervalElement.SetAttribute("Delay", delayFrames.ToString());

					// Create Points (Start, Support, Finish)
					XmlElement startPointElement = CreatePointElement(xml, "Start", 0, 0);
					XmlElement supportPointElement = CreatePointElement(xml, "Support", moveInterval.SupportXAxis * 100, -moveInterval.SupportYAxis * 100);
					supportPointElement.SetAttribute("Number", i.ToString());
					XmlElement finishPointElement = CreatePointElement(xml, "Finish", moveInterval.MoveXAxis * 100, -moveInterval.MoveYAxis * 100);

					// Append points to MoveInterval
					moveIntervalElement.AppendChild(startPointElement);
					moveIntervalElement.AppendChild(supportPointElement);
					moveIntervalElement.AppendChild(finishPointElement);

					// Store the original MoveInterval
					originalMoveIntervals.Add(moveIntervalElement);
				}
			}

			// Check if looping is enabled
			int totalIntervals = dynamicComponent.Loop ? 32 : originalMoveIntervals.Count;

			for (int i = 0; i < totalIntervals; i++)
			{
				XmlElement repeatedMoveInterval = (XmlElement)originalMoveIntervals[i % originalMoveIntervals.Count].CloneNode(true);
				repeatedMoveInterval.SetAttribute("Number", (i + 1).ToString());
				moveElement.AppendChild(repeatedMoveInterval);
			}

			transformationElement.AppendChild(moveElement);
			dynamicElement.AppendChild(transformationElement);
		}

		// DynamicColor 
		if (dynamicColorParent != null)
		{
			XmlElement transformationElement = xml.CreateElement("Transformation");
			transformationElement.SetAttribute("Name", dynamicColorParent.TransformationName);

			XmlElement colorElement = xml.CreateElement("Color");

			// Set ColorStart (StartColor) and ColorFinish (EndColor)
			string startColorHex = ColorUtility.ToHtmlStringRGB(dynamicColorParent.StartColor) + Mathf.RoundToInt(dynamicColorParent.StartColor.a * 255).ToString("X2");
			string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColorParent.EndColor) + Mathf.RoundToInt(dynamicColorParent.EndColor.a * 255).ToString("X2");

			colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
			colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

			// Calculate Frames (Duration * 60) or 1 if Duration is 0
			int frames = dynamicColorParent.Duration > 0 ? Mathf.CeilToInt(dynamicColorParent.Duration * 60) : 1;
			colorElement.SetAttribute("Frames", frames.ToString());

			transformationElement.AppendChild(colorElement);
			dynamicElement.AppendChild(transformationElement);
		}

		propertiesElement.AppendChild(dynamicElement);
		objectElement.AppendChild(propertiesElement);

		// Create Content element
		XmlElement contentElement = xml.CreateElement("Content");

		// Image list for the dynamic
		List<GameObject> ImageObjects = new List<GameObject>();

		// Add image to the list
		foreach (UnityEngine.Transform child in dynamicInSceneTransform)
		{
			if (!BuildMap.IsVisible(child.gameObject))
			{
				continue;
			};

			if (child.gameObject.CompareTag("Image"))
			{
				ImageObjects.Add(child.gameObject);
			}
		}

		// Sort the list based on order in layer
		ImageObjects.Sort((a, b) =>
		{
			SpriteRenderer rendererA = a.GetComponent<SpriteRenderer>();
			SpriteRenderer rendererB = b.GetComponent<SpriteRenderer>();

			// Handle cases where SpriteRenderer might be null
			int orderA = rendererA != null ? rendererA.sortingOrder : 0;
			int orderB = rendererB != null ? rendererB.sortingOrder : 0;

			return orderA.CompareTo(orderB);
		});


		foreach (GameObject imageObject in ImageObjects)
		{

			XmlElement ielement = xml.CreateElement("Image");
			XmlElement transformationElement = xml.CreateElement("Transformation");
			XmlElement imagePropertiesElement = xml.CreateElement("Properties");
			XmlElement staticElement = xml.CreateElement("Static");
			XmlElement matrixElement = xml.CreateElement("Matrix");
			SpriteRenderer spriteRenderer = imageObject.GetComponent<SpriteRenderer>();
			DynamicColor dynamicColor = imageObject.GetComponent<DynamicColor>();
			Color color = spriteRenderer.color;


			// Check if SpriteRenderer exists
			if (spriteRenderer != null && spriteRenderer.sprite != null)
			{
				// Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
				float imagePosX = imageObject.transform.position.x * 100;
				float imagePosY = -imageObject.transform.position.y * 100;

				// Get rotation degrees (Z-axis)

				// Normalize to 0-360 degrees
				float rotationAngle = imageObject.transform.eulerAngles.z % 360;

				// Flipping
				bool flipX = spriteRenderer.flipX;
				bool flipY = spriteRenderer.flipY;

				// If exactly one of them is flipped, negate the angle
				if (flipX ^ flipY)
				{
					rotationAngle = -rotationAngle;
				}

				if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY)
				{
					// Bounding box size in world space
					Bounds bounds = spriteRenderer.bounds;
					float worldWidth = bounds.size.x * 100;
					float worldHeight = bounds.size.y * 100;

					// Get Native resolution of the sprite
					int nativeWidth = spriteRenderer.sprite.texture.width;
					int nativeHeight = spriteRenderer.sprite.texture.height;

					// Get the image scale using sprite resolution * local scale
					float imageWidth = nativeWidth * imageObject.transform.localScale.x;
					float imageHeight = nativeHeight * imageObject.transform.localScale.y;

					// Matrix values
					float A = imageWidth, B = 0, C = 0, D = imageHeight;

					// Cases
					if (rotationAngle == 90)
					{
						A = 0;
						B = -imageWidth;
						C = imageHeight;
						D = 0;
					}
					else if (rotationAngle == 180)
					{
						A = -imageWidth;
						B = 0;
						C = 0;
						D = -imageHeight;
					}
					else if (rotationAngle == 270)
					{
						A = 0;
						B = imageWidth;
						C = -imageHeight;
						D = 0;
					}
					else
					{
						// formula for free rotation
						float radians = rotationAngle * Mathf.Deg2Rad;
						float cosTheta = Mathf.Cos(radians);
						float sinTheta = Mathf.Sin(radians);

						A = imageWidth * cosTheta;
						B = -imageWidth * sinTheta;
						C = imageHeight * sinTheta;
						D = imageHeight * cosTheta;
					}

					// Apply FlipX and FlipY
					if (spriteRenderer.flipX)
					{
						A = -A;
						C = -C;
					}
					if (spriteRenderer.flipY)
					{
						B = -B;
						D = -D;
					}

					// Calculate bounding box top-left position
					float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
					float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

					// Calculate Tx and Ty
					float Tx = imagePosX - topLeftX;
					float Ty = imagePosY - topLeftY;

					// Setting all of the attributes
					ielement.SetAttribute("X", Math.Round(topLeftX).ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("Y", Math.Round(topLeftY).ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("ClassName", Regex.Replace(imageObject.name, globalRegex, string.Empty));
					ielement.SetAttribute("Width", worldWidth.ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("Height", worldHeight.ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("NativeX", nativeWidth.ToString());
					ielement.SetAttribute("NativeY", nativeHeight.ToString());


					// Apply transformation matrix
					matrixElement.SetAttribute("A", A.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
					matrixElement.SetAttribute("B", B.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
					matrixElement.SetAttribute("C", C.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
					matrixElement.SetAttribute("D", D.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
					matrixElement.SetAttribute("Tx", Tx.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));
					matrixElement.SetAttribute("Ty", Ty.ToString("F6", CultureInfo.InvariantCulture).Replace(',', '.'));

					staticElement.AppendChild(matrixElement);
					imagePropertiesElement.AppendChild(staticElement);
					ielement.AppendChild(imagePropertiesElement);
				}
				else
				{
					Bounds bounds = spriteRenderer.sprite.bounds;
					Vector3 scale = imageObject.transform.localScale;
					float width = bounds.size.x * 100;
					float height = bounds.size.y * 100;
					ielement.SetAttribute("X", Math.Round(imagePosX).ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("Y", Math.Round(imagePosY).ToString(CultureInfo.InvariantCulture));
					ielement.SetAttribute("Width", (width * scale.x).ToString());
					ielement.SetAttribute("Height", (height * scale.y).ToString());
					ielement.SetAttribute("NativeX", width.ToString());
					ielement.SetAttribute("NativeY", height.ToString());
				}

				string objRegex = Regex.Replace(imageObject.name, globalRegex, string.Empty);
				string objName = (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : objRegex;
				ielement.SetAttribute("ClassName", objName);

				// Color of the Image
				if (color.r != 1.000 || color.g != 1.000 || color.b != 1.000 || color.a != 1.000)
				{
					string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
					string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

					XmlElement colorElement = xml.CreateElement("StartColor");
					colorElement.SetAttribute("Color", $"#{rgbaColor}");

					staticElement.AppendChild(colorElement);
					imagePropertiesElement.AppendChild(staticElement);
					ielement.AppendChild(imagePropertiesElement);
				}

				// Dynamic Color Component
				if (dynamicColor != null)
				{
					XmlElement dynamicElementImage = xml.CreateElement("Dynamic");
					XmlElement colorElement = xml.CreateElement("Color");

					// Transformation Name
					transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

					// Set ColorStart (StartColor) and ColorFinish (EndColor)
					string startColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.StartColor) + Mathf.RoundToInt(dynamicColor.StartColor.a * 255).ToString("X2");
					string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.EndColor) + Mathf.RoundToInt(dynamicColor.EndColor.a * 255).ToString("X2");

					colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
					colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

					// Calculate Frames (Duration * 60) or 1 if Duration is 0
					int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
					colorElement.SetAttribute("Frames", frames.ToString());

					transformationElement.AppendChild(colorElement);
					dynamicElement.AppendChild(transformationElement);
					imagePropertiesElement.AppendChild(dynamicElement);
					ielement.AppendChild(imagePropertiesElement);
				}
			}
			contentElement.AppendChild(ielement);
		}

		foreach (UnityEngine.Transform childObject in dynamicInSceneTransform)
		{
			// Check if the gameobject has specific tag
			if (childObject.gameObject.CompareTag("Dynamic"))
			{
				XmlElement dynamicNode1;
				ConvertToDynamic(node, xml, childObject.gameObject, childObject.gameObject.transform, out dynamicNode1);
				contentElement.AppendChild(dynamicNode1);
			}

			if (childObject.gameObject.CompareTag("Object"))
			{
				XmlElement objectNode;
				ConvertToObject(node, xml, childObject.gameObject, out objectNode, false);
				contentElement.AppendChild(objectNode);

			}
			if (childObject.gameObject.CompareTag("Item"))
			{
				XmlElement itemNode;
				ConvertToItem(node, xml, childObject.gameObject, out itemNode, true);
				contentElement.AppendChild(itemNode);
			}
			else if (childObject.gameObject.CompareTag("Platform"))
			{
				XmlElement platformNode;
				ConvertToPlatform(node, xml, childObject.gameObject, out platformNode, false);
				contentElement.AppendChild(platformNode);

			}
			else if (childObject.gameObject.CompareTag("Trapezoid"))
			{
				XmlElement trapezoidNode;
				ConvertToTrapezoid(node, xml, childObject.gameObject, out trapezoidNode, false);
				contentElement.AppendChild(trapezoidNode);
			}
			else if (childObject.gameObject.CompareTag("Area"))
			{
				XmlElement areaNode;
				ConvertToArea(node, xml, childObject.gameObject, out areaNode, false);
				contentElement.AppendChild(areaNode);
			}
			else if (childObject.gameObject.CompareTag("Trigger"))
			{
				XmlElement triggerNode;
				ConvertToTrigger(node, xml, childObject.gameObject, out triggerNode, false);
				contentElement.AppendChild(triggerNode);
			}
			else if (childObject.gameObject.CompareTag("Model"))
			{

				XmlElement modelNode;
				ConvertToModel(node, xml, childObject.gameObject, out modelNode, false);
				contentElement.AppendChild(modelNode);
			}
			else if (childObject.gameObject.CompareTag("Animation"))
			{
				XmlElement animNode;
				ConvertToAnimation(node, xml, childObject.gameObject, out animNode, false);
				contentElement.AppendChild(animNode);
			}
			else if (childObject.gameObject.CompareTag("Particle"))
			{
				XmlElement particleNode;
				ConvertToParticle(node, xml, childObject.gameObject, out particleNode, false);
				contentElement.AppendChild(particleNode);
			}

			// Add content to the object
			objectElement.AppendChild(contentElement);
		}

		dynamicNode = objectElement;
	}

	// Helper Method: Create a Point element
	XmlElement CreatePointElement(XmlDocument xml, string name, float x, float y)
	{
		XmlElement pointElement = xml.CreateElement("Point");
		pointElement.SetAttribute("Name", name);
		pointElement.SetAttribute("X", x.ToString("0.0"));
		pointElement.SetAttribute("Y", y.ToString("0.0"));
		return pointElement;
	}

	// Helper Method: Get MoveInterval by index
	Dynamic.Movement GetMoveInterval(Dynamic dynamicComponent, int index)
	{
		switch (index)
		{
			case 1: return dynamicComponent.MoveInterval1;
			case 2: return dynamicComponent.MoveInterval2;
			case 3: return dynamicComponent.MoveInterval3;
			case 4: return dynamicComponent.MoveInterval4;
			case 5: return dynamicComponent.MoveInterval5;
			case 6: return dynamicComponent.MoveInterval6;
			case 7: return dynamicComponent.MoveInterval7;
			case 8: return dynamicComponent.MoveInterval8;
			case 9: return dynamicComponent.MoveInterval9;
			case 10: return dynamicComponent.MoveInterval10;
			case 11: return dynamicComponent.MoveInterval11;
			case 12: return dynamicComponent.MoveInterval12;
			case 13: return dynamicComponent.MoveInterval13;
			case 14: return dynamicComponent.MoveInterval14;
			case 15: return dynamicComponent.MoveInterval15;
			case 16: return dynamicComponent.MoveInterval16;
			default: return null;
		}
	}
}