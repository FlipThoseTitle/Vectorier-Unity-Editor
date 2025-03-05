using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;


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

    [Tooltip("Level that will get overridden.")]
    public string mapToOverride = "DOWNTOWN_STORY_02";

    [Tooltip("Music that will be played on the level.")]
    public string levelMusic = "music_dinamic";

    [Tooltip("Volume of the music.")]
    public string MusicVolume = "0.3";

    [Tooltip("Background Image")]
    public string customBackground = "v_bg";

    [Tooltip("Background Width")]
    public string bg_Width = "2121";

    [Tooltip("Background Height")]
    public string bg_Height = "1116";

    // Gameplay
    [Serializable]
    public class PlayerSettings
    {
        public string playerModelName = "Player";
        [Tooltip("Player's Spawn Name")] public string playerSpawnName = "PlayerSpawn";

        [Tooltip("Duration until the player appears.")]
        public float playerSpawnTime;

        [Tooltip("Player Appearance (Default: 1)")]
        public string playerSkin = "1";
    }
    [Serializable]
    public class HunterSettings
    {
        public string hunterModelName = "Hunter";

        [Tooltip("Hunter's Spawn Name")]
        public string hunterSpawnName = "DefaultSpawn";

        [Tooltip("Time it takes for the hunter to spawn in.")]
        public float hunterSpawnTime;

        [Tooltip("Hunter Respawn Name")]
        public string hunterAllowedSpawn = "Respawn";

        [Tooltip("Hunter Appearance (Default: hunter)")]
        public string hunterSkin = "hunter";

        [Tooltip("Hunter is able do to tricks")]
        public bool hunterTrickAllowed;

        [Tooltip("Shows hunter icon or not")]
        public bool hunterIcon = true;

        [Tooltip("Ai Number (Default: 1)")]
        public int hunterAIType = 1;
    }

    [Header("Gameplay")]
    [SerializeField]
    private PlayerSettings Player;

    [SerializeField]
    private HunterSettings Hunter;

    [Tooltip("Uses custom properties instead of prefixed (Will ignore the settings for player and hunter above.)")]
    public bool useCustomProperties;

    [TextArea(5, 20)]
    public string CustomModelProperties = @"<Model Name=""Player""
		Type=""1""
		Color=""0""
		BirthSpawn=""PlayerSpawn""
		AI=""0""
		Time=""0""
		Respawns=""Hunter""
		ForceBlasts=""Hunter""
		Trick=""1""
		Item=""1""
		Victory=""1""
		Lose=""1""
	/>

	<Model Name=""Hunter""
		Type=""0""
		Color=""0""
		BirthSpawn=""DefaultSpawn""
		AI=""1""
		Time=""0.8""
		AllowedSpawns=""Respawn""
		Skins=""hunter""
		Murders=""Player""
		Arrests=""Player""
		Icon=""1""
	/>";


    // Miscellaneous
    [Header("Miscellaneous")]
    public bool debugObjectWriting;
    public bool hunterPlaced;

    [Tooltip("Divide Gameobject's position by object factor.")]
    public bool correctFactorPosition = false;


    // -=-=-=- //


    [MenuItem("Vectorier/BuildMap")]
    public static void BuildDZ() { Build(true, true); }

    [MenuItem("Vectorier/BuildMap (Fast) #&B")]
    public static void BuildZlib() { Build(false, true); }

    [MenuItem("Vectorier/BuildMap XML Only")]
    public static void BuildXml() { Build(false, false); }


    // -=-=-=- //

	public static bool IsVisible(GameObject obj)
	{
		return !obj.CompareTag("EditorOnly") && 
			!obj.CompareTag("Unused") && 
			!SceneVisibilityManager.instance.IsHidden(obj) && 
			obj.activeInHierarchy;
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

        //Erase last build
        File.Delete(Application.dataPath + "/XML/build-map.xml");
        File.Copy(Application.dataPath + "/XML/empty-map-DONT-MODIFY.xml", Application.dataPath + "/XML/build-map.xml");

        //Open the object.xml
        XmlDocument xml = new XmlDocument();
        xml.Load(Application.dataPath + "/XML/build-map.xml");

        XmlNode rootNode = xml.DocumentElement.SelectSingleNode("/Root/Track");

        //Search for the selected object in the object.xml
        foreach (XmlNode node in rootNode)
        {

            string factorValue = node.Attributes.GetNamedItem("Factor").Value;

            if (factorValue == "1")
            {

                //Set the properties into the level
                buildMap.SetLevelProperties(xml, node);

                // Get all GameObjects with tag "Image", then arrange them based on sorting order
                GameObject[] imagesInScene = GameObject.FindGameObjectsWithTag("Image")
                                            .OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
                                            .ToArray();

                //Write every GameObject with tag "Object", "Image", "Platform", "Area" and "Trigger" in the build-map.xml
                foreach (GameObject spawnInScene in GameObject.FindGameObjectsWithTag("Spawn"))
                {
                    UnityEngine.Transform parent = spawnInScene.transform.parent;
					if (parent != null && parent.CompareTag("Object") && !IsVisible(spawnInScene))
					{
						continue;
					}

                    XmlElement spawnNode;
                    buildMap.ConvertToSpawn(node, xml, spawnInScene, out spawnNode, false);
                    if (spawnNode != null) // Check if spawnNode is not null before appending
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
                    node.FirstChild.AppendChild(imageNode); //Place it into the Object node
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}

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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}

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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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

                    //apply the modification to the build-map.xml with proper format
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
                }

                // Camera
                foreach (GameObject camInScene in GameObject.FindGameObjectsWithTag("Camera"))
                {
					if (!IsVisible(camInScene))
					{
						continue;
					}

                    //Note: This is actually a trigger, but with camera zoom properties
                    XmlElement cameraNode;
                    buildMap.ConvertToCamera(node, xml, camInScene, out cameraNode, false);
                    node.FirstChild.AppendChild(cameraNode);
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}

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

            //Write every GameObject with tag "Backdrop" in the build-map.xml
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
                        xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                        xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                        xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                        xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
                    xml.Save(Application.dataPath + "/XML/dzip/level_xml/" + buildMap.mapToOverride + ".xml"); //Apply the modification to the build-map.xml file}
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
        WorkingDirectory = batchDirectory // Set working directory
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
        // Find all object
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
                foreach (XmlNode imageNode in imageNodes)
                {
                    imageNode.Attributes["ClassName"].Value = customBackground;
                    imageNode.Attributes["Width"].Value = bg_Width;
                    imageNode.Attributes["Height"].Value = bg_Height;
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
        foreach (GameObject allObjects in allObj) //loop to see if the object has buildmap component under it
        {
            BuildMap buildMap = allObjects.GetComponent<BuildMap>();
            if (useCustomProperties) //if use custom properties is true
            {
                foreach (XmlNode modelsNode in rootNode)
                {
                    if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode") //search for the models node
                    {
                        while (modelsNode.HasChildNodes) //if there is child node then remove it
                        {
                            modelsNode.RemoveChild(modelsNode.FirstChild); //im not gonna lie, just trying to remove childnode took me solid 2 hours
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

            else if (!useCustomProperties) // if use custom properties is false
            {
                foreach (XmlNode modelsNode in rootNode)
                {
                    if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode")
                    {
                        foreach (XmlNode modelNode in modelsNode.ChildNodes)
                        {
                            if (modelNode.Attributes["Name"].Value == "Player" || modelNode.Attributes["Name"].Value == Player.playerModelName)
                            {
                                //player model name
                                modelNode.Attributes["Name"].Value = Player.playerModelName;

                                //spawn time
                                modelNode.Attributes["Time"].Value = Player.playerSpawnTime.ToString();

                                //spawn name
                                modelNode.Attributes["BirthSpawn"].Value = Player.playerSpawnName;


                                //skin
                                XmlAttribute playerskin = xml.CreateAttribute("Skins");

                                if (!string.IsNullOrEmpty(Player.playerSkin)) //check if playerskin is specified
                                {
                                    playerskin.Value = Player.playerSkin;
                                }
                                else
                                {
                                    playerskin.Value = "1";
                                    Debug.LogWarning("Player skin isn't specified, setting to default..");
                                }
                                modelNode.Attributes.Append(playerskin);

                            }
                            if (modelNode.Attributes["Name"].Value == "Hunter" || modelNode.Attributes["Name"].Value == Hunter.hunterModelName)
                            {
                                //hunter model name
                                modelNode.Attributes["Name"].Value = Hunter.hunterModelName;

                                //spawn time
                                modelNode.Attributes["Time"].Value = Hunter.hunterSpawnTime.ToString();

                                //spawn name
                                modelNode.Attributes["BirthSpawn"].Value = Hunter.hunterSpawnName;

                                //ai number
                                modelNode.Attributes["AI"].Value = Hunter.hunterAIType.ToString();

                                //huntericon
                                if (Hunter.hunterIcon)
                                {
                                    modelNode.Attributes["Icon"].Value = "1";
                                }
                                else
                                {
                                    modelNode.Attributes["Icon"].Value = "0";
                                }

                                //skin
                                XmlAttribute hunterskin = xml.CreateAttribute("Skins");

                                if (!string.IsNullOrEmpty(Hunter.hunterSkin)) //check if hunterskin is specified
                                {
                                    hunterskin.Value = Hunter.hunterSkin;
                                }
                                else
                                {
                                    hunterskin.Value = "hunter";
                                    Debug.LogWarning("Hunter skin isn't specified, setting to default..");
                                }
                                modelNode.Attributes.Append(hunterskin);

                                //trick

                                if (Hunter.hunterTrickAllowed) //check if hunter is allowed to do trick
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
		animationElement.SetAttribute("X", Math.Round(pos.x * 100).ToString("0.#####", CultureInfo.InvariantCulture));
		animationElement.SetAttribute("Y", Math.Round(-pos.y * 100).ToString("0.#####", CultureInfo.InvariantCulture));

		animationElement.SetAttribute("Width", AnimationComponent.Width);
		animationElement.SetAttribute("Height", AnimationComponent.Height);
		animationElement.SetAttribute("Type", AnimationComponent.Type); // default is 1


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

        animationElement.SetAttribute("ClassName", Regex.Replace(animationInScene.name, @" ?\((.*?)\)| ?\[.*?\]", string.Empty));
        animNode = animationElement;
    }


    void ConvertToTopImage(XmlNode node, XmlDocument xml, GameObject frontimageInScene, out XmlElement topimgNode)
    {
        topimgNode = null;

        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(frontimageInScene.name, @" \((.*?)\)", string.Empty));

        if (frontimageInScene.name != "Camera")
        {
            XmlElement ielement = xml.CreateElement("Image");
            XmlElement transformationElement = xml.CreateElement("Transformation");
            XmlElement propertiesElement = xml.CreateElement("Properties");
            XmlElement staticElement = xml.CreateElement("Static");
            XmlElement matrixElement = xml.CreateElement("Matrix");
            SpriteRenderer spriteRenderer = frontimageInScene.GetComponent<SpriteRenderer>();
            DynamicColor dynamicColor = frontimageInScene.GetComponent<DynamicColor>();
            Color color = spriteRenderer.color;


            if (spriteRenderer != null && spriteRenderer.sprite != null) // Check if SpriteRenderer exists
            {
                // Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
                float imagePosX = frontimageInScene.transform.position.x * 100;
                float imagePosY = -frontimageInScene.transform.position.y * 100;

                // Get rotation degrees (Z-axis)
                float rotationAngle = frontimageInScene.transform.eulerAngles.z % 360; // Normalize to 0-360 degrees

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
                    // bounding box size in world space
                    Bounds bounds = spriteRenderer.bounds;
                    float worldWidth = bounds.size.x * 100;
                    float worldHeight = bounds.size.y * 100;

                    // Get Native resolution of the sprite
                    int nativeWidth = spriteRenderer.sprite.texture.width;
                    int nativeHeight = spriteRenderer.sprite.texture.height;

                    // Get the image scale using sprite resolution * local scale
                    float imageWidth = nativeWidth * frontimageInScene.transform.localScale.x;
                    float imageHeight = nativeHeight * frontimageInScene.transform.localScale.y;

                    // matrix values
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
                    ielement.SetAttribute("X", Math.Round(topLeftX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(topLeftY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(frontimageInScene.name, @" \((.*?)\)", string.Empty));
                    ielement.SetAttribute("Width", worldWidth.ToString().Replace(',', '.'));
                    ielement.SetAttribute("Height", worldHeight.ToString().Replace(',', '.'));
                    ielement.SetAttribute("NativeX", nativeWidth.ToString());
                    ielement.SetAttribute("NativeY", nativeHeight.ToString());


                    // Apply transformation matrix
                    matrixElement.SetAttribute("A", A.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("B", B.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("C", C.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("D", D.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Tx", Tx.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Ty", Ty.ToString("F6").Replace(',', '.'));

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
                    ielement.SetAttribute("X", Math.Round(imagePosX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(imagePosY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(frontimageInScene.name, @" \((.*?)\)", string.Empty));
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
    }

    void ConvertToSpawn(XmlNode node, XmlDocument xml, GameObject spawnInScene, out XmlElement spawnNode, bool localPosition)
    {
        spawnNode = null;
        Respawn RespawnComponent = spawnInScene.GetComponent<Respawn>(); //Respawn component
        Spawn Spawn = spawnInScene.GetComponent<Spawn>(); //spawn component
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
                GameObject gameObjwithSpawnComponent = spawns.gameObject; //check every game object that has the spawn component

                if (RespawnComponent.RespawnName == gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName)
                {
                    if (gameObjwithSpawnComponent.GetComponent<Spawn>().RefersToRespawn)
                    {
                        // spawn element
                        XmlElement spawnInsideElement = xml.CreateElement("Spawn");
                        if (localPosition)
                        {
                            spawnInsideElement.SetAttribute("X", Math.Round(gameObjwithSpawnComponent.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                            spawnInsideElement.SetAttribute("Y", Math.Round(-gameObjwithSpawnComponent.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                        }
                        else
                        {
                            spawnInsideElement.SetAttribute("X", Math.Round(gameObjwithSpawnComponent.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                            spawnInsideElement.SetAttribute("Y", Math.Round(-gameObjwithSpawnComponent.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                        }
                        spawnInsideElement.SetAttribute("Name", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName);
                        spawnInsideElement.SetAttribute("Animation", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnAnimation);
                        contentElement.AppendChild(spawnInsideElement);
                    }
                }
            }

            //Trigger element
            XmlElement triggerElement = xml.CreateElement("Trigger");
            triggerElement.SetAttribute("Name", RespawnComponent.TriggerName);
            if (localPosition)
            {
                triggerElement.SetAttribute("X", Math.Round(spawnInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                triggerElement.SetAttribute("Y", Math.Round(-spawnInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                triggerElement.SetAttribute("X", Math.Round(spawnInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                triggerElement.SetAttribute("Y", Math.Round(-spawnInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }



            SpriteRenderer spriteRenderer = spawnInScene.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
            {

                Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                Vector3 scale = spawnInScene.transform.localScale; // Get the GameObject scale

                // Retrieve the image resolution of the sprite
                float width = bounds.size.x * 100;
                float height = bounds.size.y * 100;

                // Set the width and height accordingly to the scale in the editor
                triggerElement.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                triggerElement.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image
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

            XmlElement triggerContentElement = xml.CreateElement("Content"); // create content element inside trigger element
            XmlElement initElement = xml.CreateElement("Init"); // create the init element and its child setVariable element

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

            // add each setVariable element to the init element
            foreach (var setVariable in setVariables)
            {
                XmlElement setVariableElement = xml.CreateElement("SetVariable");
                setVariableElement.SetAttribute(setVariable[0], setVariable[1]);
                setVariableElement.SetAttribute(setVariable[2], setVariable[3]);
                initElement.AppendChild(setVariableElement);
            }

            triggerContentElement.AppendChild(initElement);

            // create template element inside content element
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
                if (localPosition)
                {
                    spawnElement.SetAttribute("X", Math.Round(spawnInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    spawnElement.SetAttribute("Y", Math.Round(-spawnInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    spawnElement.SetAttribute("X", Math.Round(spawnInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    spawnElement.SetAttribute("Y", Math.Round(-spawnInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                spawnElement.SetAttribute("Name", Spawn.SpawnName); // name in the spawn component
                spawnElement.SetAttribute("Animation", Spawn.SpawnAnimation); // spawnanim in spawn component

                spawnNode = spawnElement;
            }

        }
    }

    void ConvertToBackdrop(XmlNode node, XmlDocument xml, GameObject bdInScene, float FactorAmount, out XmlElement backdropNode)
    {
        backdropNode = null;

        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(bdInScene.name, @" \((.*?)\)", string.Empty));

        SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();
        DynamicColor dynamicColor = bdInScene.GetComponent<DynamicColor>();
        XmlElement transformationElement = xml.CreateElement("Transformation");

        if (bdInScene.name != "Camera")
        {
            // Alternative backdrops scaling

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
                BD_element.SetAttribute("Name", Regex.Replace(bdInScene.name, @" \((.*?)\)", string.Empty));
                BD_element.SetAttribute("X", Math.Round(positionX * 100).ToString().Replace(',', '.'));
                BD_element.SetAttribute("Y", Math.Round(-positionY * 100).ToString().Replace(',', '.'));
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
                float rotationAngle = bdInScene.transform.eulerAngles.z % 360; // Normalize to 0-360 degrees

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
                    // bounding box size in world space
                    Bounds bounds = spriteRenderer.bounds;
                    float worldWidth = bounds.size.x * 100;
                    float worldHeight = bounds.size.y * 100;

                    // Get Native resolution of the sprite
                    int nativeWidth = spriteRenderer.sprite.texture.width;
                    int nativeHeight = spriteRenderer.sprite.texture.height;

                    // Get the image scale using sprite resolution * local scale
                    float imageWidth = nativeWidth * bdInScene.transform.localScale.x;
                    float imageHeight = nativeHeight * bdInScene.transform.localScale.y;

                    // matrix values
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

                    float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
                    float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

                    float Tx = imagePosX - topLeftX;
                    float Ty = imagePosY - topLeftY;

                    // Apply Attributes
                    BD_element.SetAttribute("X", Math.Round(topLeftX).ToString().Replace(',', '.'));
                    BD_element.SetAttribute("Y", Math.Round(topLeftY).ToString().Replace(',', '.'));
                    BD_element.SetAttribute("ClassName", Regex.Replace(bdInScene.name, @" \((.*?)\)", string.Empty));
                    BD_element.SetAttribute("Width", (worldWidth * scale.x).ToString());
                    BD_element.SetAttribute("Height", (worldHeight * scale.y).ToString());
                    BD_element.SetAttribute("NativeX", nativeWidth.ToString());
                    BD_element.SetAttribute("NativeY", nativeHeight.ToString());

                    // Matrix
                    matrixElement.SetAttribute("A", A.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("B", B.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("C", C.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("D", D.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Tx", Tx.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Ty", Ty.ToString("F6").Replace(',', '.'));

                    staticElement.AppendChild(matrixElement);
                    propertiesElement.AppendChild(staticElement);
                    BD_element.AppendChild(propertiesElement);
                }
                else
                {
                    Bounds bounds = spriteRenderer.sprite.bounds;
                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;
                    BD_element.SetAttribute("X", Math.Round(positionX * 100).ToString().Replace(',', '.'));
                    BD_element.SetAttribute("Y", Math.Round(-positionY * 100).ToString().Replace(',', '.'));
                    BD_element.SetAttribute("ClassName", Regex.Replace(bdInScene.name, @" \((.*?)\)", string.Empty));
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
    }

    void ConvertToImage(XmlNode node, XmlDocument xml, GameObject imageInScene, out XmlElement imageNode, bool localPosition)
    {
        imageNode = null;

        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(imageInScene.name, @" \((.*?)\)", string.Empty));

        if (imageInScene.name != "Camera")
        {
            XmlElement ielement = xml.CreateElement("Image");
            XmlElement transformationElement = xml.CreateElement("Transformation");
            XmlElement propertiesElement = xml.CreateElement("Properties");
            XmlElement staticElement = xml.CreateElement("Static");
            XmlElement matrixElement = xml.CreateElement("Matrix");
            SpriteRenderer spriteRenderer = imageInScene.GetComponent<SpriteRenderer>();
            DynamicColor dynamicColor = imageInScene.GetComponent<DynamicColor>();
            Color color = spriteRenderer.color;


            if (spriteRenderer != null && spriteRenderer.sprite != null) // Check if SpriteRenderer exists
            {
                // Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
                float imagePosX;
                float imagePosY;
                if (localPosition)
                {
                    imagePosX = imageInScene.transform.localPosition.x * 100;
                    imagePosY = -imageInScene.transform.localPosition.y * 100;
                }
                else
                {
                    imagePosX = imageInScene.transform.position.x * 100;
                    imagePosY = -imageInScene.transform.position.y * 100;
                }

                // Get rotation degrees (Z-axis)
                float rotationAngle = imageInScene.transform.eulerAngles.z % 360; // Normalize to 0-360 degrees

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
                    // bounding box size in world space
                    Bounds bounds = spriteRenderer.bounds;
                    float worldWidth = bounds.size.x * 100;
                    float worldHeight = bounds.size.y * 100;

                    // Get Native resolution of the sprite
                    int nativeWidth = spriteRenderer.sprite.texture.width;
                    int nativeHeight = spriteRenderer.sprite.texture.height;

                    // Get the image scale using sprite resolution * local scale
                    float imageWidth = nativeWidth * imageInScene.transform.localScale.x;
                    float imageHeight = nativeHeight * imageInScene.transform.localScale.y;

                    // matrix values
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
                    ielement.SetAttribute("X", Math.Round(topLeftX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(topLeftY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(imageInScene.name, @" \((.*?)\)", string.Empty));
                    ielement.SetAttribute("Width", worldWidth.ToString().Replace(',', '.'));
                    ielement.SetAttribute("Height", worldHeight.ToString().Replace(',', '.'));
                    ielement.SetAttribute("NativeX", nativeWidth.ToString());
                    ielement.SetAttribute("NativeY", nativeHeight.ToString());


                    // Apply transformation matrix
                    matrixElement.SetAttribute("A", A.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("B", B.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("C", C.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("D", D.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Tx", Tx.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Ty", Ty.ToString("F6").Replace(',', '.'));

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
                    ielement.SetAttribute("X", Math.Round(imagePosX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(imagePosY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(imageInScene.name, @" \((.*?)\)", string.Empty));
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
            imageNode = ielement;
        }
    }

    void ConvertToModel(XmlNode node, XmlDocument xml, GameObject modelInScene, out XmlElement modelNode, bool localPosition)
    {
        modelNode = null;
        if (modelInScene.name != "Camera")
        {
            ModelProperties modelProperties = modelInScene.GetComponent<ModelProperties>();

            XmlElement Melement = xml.CreateElement("Model"); //Create a new node from scratch
            if (localPosition)
            {
                Melement.SetAttribute("X", Math.Round(modelInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                Melement.SetAttribute("Y", Math.Round(-modelInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                Melement.SetAttribute("X", Math.Round(modelInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                Melement.SetAttribute("Y", Math.Round(-modelInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            Melement.SetAttribute("Type", modelProperties.Type.ToString()); //Add an name
            Melement.SetAttribute("ClassName", Regex.Replace(modelInScene.name, @" \((.*?)\)", string.Empty)); //Add an name

            if (modelProperties.UseLifeTime)
            {
                Melement.SetAttribute("LifeTime", modelProperties.LifeTime); //Add an name
            }

            modelNode = Melement;
        }
    }

    void ConvertToObject(XmlNode node, XmlDocument xml, GameObject objectInScene, out XmlElement objectNode, bool localPosition)
    {
        objectNode = null;
        //Debug in log every object it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(objectInScene.name, @" \((.*?)\)", string.Empty));

        if (objectInScene.name != "Camera")
        {
            string name = objectInScene.name;
            XmlElement element = xml.CreateElement("Object"); //Create a new node from scratch
            if (name != string.Empty)
            {
                element.SetAttribute("Name", Regex.Replace(name, @" \((.*?)\)", string.Empty)); //Add an name
            }

            if (localPosition)
            {
                element.SetAttribute("X", Math.Round(objectInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                element.SetAttribute("Y", Math.Round(-objectInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)

            }
            else
            {
                element.SetAttribute("X", Math.Round(objectInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                element.SetAttribute("Y", Math.Round(-objectInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            XmlElement contentElement = xml.CreateElement("Content");

            List<GameObject> ImageObjects = new List<GameObject>();

            // add image to the list
            foreach (UnityEngine.Transform child in objectInScene.transform)
            {
                if (child.gameObject.CompareTag("Image"))
                {
                    ImageObjects.Add(child.gameObject);
                }
            }

            // sort the list based on order in layer
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
                //check if the gameobject has specific tag

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
                    //Platform

                    XmlElement spawnNode;
                    ConvertToSpawn(node, xml, childObject.gameObject, out spawnNode, true);
                    contentElement.AppendChild(spawnNode);

                }

                else if (childObject.gameObject.CompareTag("Platform"))
                {
                    //Platform

                    XmlElement platformNode;
                    ConvertToPlatform(node, xml, childObject.gameObject, out platformNode, true);
                    contentElement.AppendChild(platformNode);

                }

                else if (childObject.gameObject.CompareTag("Trapezoid"))
                {
                    // Trapezoid
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
            XmlElement element = xml.CreateElement("Camera"); //Create a new node from scratch
            element.SetAttribute("X", (objectInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
            element.SetAttribute("Y", (-objectInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            objectNode = element;
        }

    }

    void ConvertToPlatform(XmlNode node, XmlDocument xml, GameObject platformInScene, out XmlElement platformNode, bool localPosition) // Platform Collision (Invisible block that is collide-able)
    {
        platformNode = null;
        //Debug in log every platform it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(platformInScene.name, @" \((.*?)\)", string.Empty));

        if (platformInScene.name != "Camera") // Use a texture called "collision" which should come with this buildmap update folder.
        {
            XmlElement P_element = xml.CreateElement("Platform"); //Create a new node from scratch
            if (localPosition)
            {
                P_element.SetAttribute("X", (Mathf.Round(platformInScene.transform.localPosition.x * 100f)).ToString("F2", CultureInfo.InvariantCulture)); //Add X position (Refit into the Vector units)
                P_element.SetAttribute("Y", (Mathf.Round(-platformInScene.transform.localPosition.y * 100f)).ToString("F2", CultureInfo.InvariantCulture)); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                P_element.SetAttribute("X", (Mathf.Round(platformInScene.transform.position.x * 100f)).ToString("F2", CultureInfo.InvariantCulture)); //Add X position (Refit into the Vector units)
                P_element.SetAttribute("Y", (Mathf.Round(-platformInScene.transform.position.y * 100f)).ToString("F2", CultureInfo.InvariantCulture)); // Add Y position (Negative because Vector see the world upside down)
            }


            SpriteRenderer spriteRenderer = platformInScene.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
            {

                Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                Vector3 scale = platformInScene.transform.localScale; // Get the GameObject scale

                // Retrieve the image resolution of the sprite
                float width = bounds.size.x * 100;
                float height = bounds.size.y * 100;

                // Round the width and height to the nearest integer
                int finalWidth = Mathf.RoundToInt(width * scale.x);
                int finalHeight = Mathf.RoundToInt(height * scale.y);

                // Set the width and height accordingly to the scale in the editor
                P_element.SetAttribute("Width", finalWidth.ToString()); // Width of the Collision
                P_element.SetAttribute("Height", finalHeight.ToString()); // Height of the Collision

            }
            platformNode = P_element;
        }
    }

    void ConvertToTrapezoid(XmlNode node, XmlDocument xml, GameObject trapezoidInScene, out XmlElement trapezoidNode, bool localPosition) // Trapezoid Collision (Slope)
    {
        trapezoidNode = null;
        //Debug in log every platform it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(trapezoidInScene.name, @" \((.*?)\)", string.Empty));

        if (Regex.Replace(trapezoidInScene.name, @" \((.*?)\)", string.Empty) == "trapezoid_type1") // Slope Default
        {
            XmlElement T_element = xml.CreateElement("Trapezoid"); //Create a new node from scratch
            if (localPosition)
            {
                T_element.SetAttribute("X", Math.Round(trapezoidInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                T_element.SetAttribute("Y", Math.Round(-trapezoidInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                T_element.SetAttribute("X", Math.Round(trapezoidInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                T_element.SetAttribute("Y", Math.Round(-trapezoidInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }

            SpriteRenderer spriteRenderer = trapezoidInScene.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
            {

                Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                Vector3 scale = trapezoidInScene.transform.localScale; // Get the GameObject scale

                // Retrieve the image resolution of the sprite
                float width = bounds.size.x * 100;
                float height = bounds.size.y * 100;

                // Set the width and height accordingly to the scale in the editor
                T_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Trapezoid
                T_element.SetAttribute("Height", "1"); //Height of the Trapezoid
                T_element.SetAttribute("Height1", (height * scale.y + 1).ToString()); //Height1 of the Trapezoid

            }
            T_element.SetAttribute("Type", "1"); //Type of the Trapezoid

            trapezoidNode = T_element;
        }

        else if (Regex.Replace(trapezoidInScene.name, @" \((.*?)\)", string.Empty) == "trapezoid_type2") // Slope Mirrored
        {
            XmlElement T_element = xml.CreateElement("Trapezoid"); //Create a new node from scratch
            if (localPosition)
            {
                T_element.SetAttribute("X", Math.Round(trapezoidInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                T_element.SetAttribute("Y", Math.Round(-trapezoidInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                T_element.SetAttribute("X", Math.Round(trapezoidInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                T_element.SetAttribute("Y", Math.Round(-trapezoidInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }

            SpriteRenderer spriteRenderer = trapezoidInScene.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
            {

                Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                Vector3 scale = trapezoidInScene.transform.localScale; // Get the GameObject scale

                // Retrieve the image resolution of the sprite
                float width = bounds.size.x * 100;
                float height = bounds.size.y * 100;

                // Set the width and height accordingly to the scale in the editor
                T_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Trapezoid
                T_element.SetAttribute("Height", (height * scale.y + 1).ToString()); //Height of the Trapezoid
                T_element.SetAttribute("Height1", "1"); //Height1 of the Trapezoid

            }
            T_element.SetAttribute("Type", "2"); //Type of the Trapezoid

            trapezoidNode = T_element;
        }
    }

    void ConvertToTrigger(XmlNode node, XmlDocument xml, GameObject triggerInScene, out XmlElement triggerNode, bool localPosition)
    {
        triggerNode = null;
        //Debug in log every trigger it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(triggerInScene.name, @" \((.*?)\)", string.Empty));

        if (triggerInScene.name != "Camera")
        {
            DynamicTrigger dynamicTrigger = triggerInScene.GetComponent<DynamicTrigger>();
            TriggerSettings triggerSettings = triggerInScene.GetComponent<TriggerSettings>(); //Trigger Settings.cs

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
                XmlElement T_element = xml.CreateElement("Trigger"); //Create a new node from scratch
                T_element.SetAttribute("Name", Regex.Replace(triggerInScene.name, @" \((.*?)\)", string.Empty)); //Add an name
                if (localPosition)
                {
                    T_element.SetAttribute("X", Math.Round(triggerInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    T_element.SetAttribute("Y", Math.Round(-triggerInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    T_element.SetAttribute("X", Math.Round(triggerInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    T_element.SetAttribute("Y", Math.Round(-triggerInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }

                SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
                {

                    Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                    Vector3 scale = triggerInScene.transform.localScale; // Get the GameObject scale

                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;

                    T_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                    T_element.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image
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
                setVariable3.SetAttribute("Value", "COM");
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
                eventBlockElement.SetAttribute("Template", "FreqUsed.Enter");
                eventsElement.AppendChild(eventBlockElement);

                // Append Events to Loop
                loopElement.AppendChild(eventsElement);

                // Create Actions element and ActionBlock element
                XmlElement actionsElement = xml.CreateElement("Actions");
                XmlElement actionBlockElement = xml.CreateElement("ActionBlock");
                actionBlockElement.SetAttribute("Template", "FreqUsed.SwitchOff");
                actionsElement.AppendChild(actionBlockElement);

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

            if (triggerSettings != null) //Checks if the trigger has a setting component
            {
                XmlElement T_element = xml.CreateElement("Trigger"); //Create a new node from scratch
                T_element.SetAttribute("Name", Regex.Replace(triggerInScene.name, @" \((.*?)\)", string.Empty)); //Add an name
                if (localPosition)
                {
                    T_element.SetAttribute("X", Math.Round(triggerInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    T_element.SetAttribute("Y", Math.Round(-triggerInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    T_element.SetAttribute("X", Math.Round(triggerInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    T_element.SetAttribute("Y", Math.Round(-triggerInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }

                SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
                {

                    Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                    Vector3 scale = triggerInScene.transform.localScale; // Get the GameObject scale

                    // Retrieve the image resolution of the sprite
                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;

                    // Set the width and height accordingly to the scale in the editor
                    T_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                    T_element.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image

                    // Create the content node and add it to the trigger node
                    XmlElement contentElement = xml.CreateElement("Content");

                    //xml doesn't format correctly so we load them into a separate doc
                    XmlDocument tempDoc = new XmlDocument();
                    tempDoc.LoadXml("<Content>" + triggerSettings.Content + "</Content>");
                    foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes)
                    {
                        XmlNode importedNode = xml.ImportNode(childNode, true);
                        contentElement.AppendChild(importedNode);
                    }

                    T_element.AppendChild(contentElement);

                    triggerNode = T_element; //Place it into the Object node

                }
            }

        }
    }

    void ConvertToItem(XmlNode node, XmlDocument xml, GameObject itemInScene, out XmlElement itemNode, bool localPosition)
    {
        itemNode = null;

        if (debugObjectWriting)
        {
            Debug.Log("Writing object : " + Regex.Replace(itemInScene.name, @" \((.*?)\)", string.Empty));
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
        I_element.SetAttribute("X", Math.Round(xPosition * 100).ToString().Replace(',', '.'));
        I_element.SetAttribute("Y", Math.Round(-yPosition * 100).ToString().Replace(',', '.'));

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
        //Debug in log every Area it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(areaInScene.name, @" \((.*?)\)", string.Empty));

        if (areaInScene.name != "Camera")
        {
            if (areaInScene.name == "TriggerCatch" || areaInScene.name == "TriggerCatchFront")
            {
                XmlElement A_element = xml.CreateElement("Area"); //Create a new node from scratch
                A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, @" \((.*?)\)", string.Empty)); //Add an name
                if (localPosition)
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }

                SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
                {

                    Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                    Vector3 scale = areaInScene.transform.localScale; // Get the GameObject scale

                    // Retrieve the image resolution of the sprite
                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;

                    // Set the width and height accordingly to the scale in the editor
                    A_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                    A_element.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image

                }
                A_element.SetAttribute("Type", "Catch"); //Type="Catch"/>
                A_element.SetAttribute("Distance", "300"); //Distance="300"/>
                areaNode = A_element;
            }
            else if (areaInScene.name == "TriggerCatchFast")
            {
                XmlElement A_element = xml.CreateElement("Area"); //Create a new node from scratch
                A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, @" \((.*?)\)", string.Empty)); //Add an name
                if (localPosition)
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }

                SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
                {

                    Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                    Vector3 scale = areaInScene.transform.localScale; // Get the GameObject scale

                    // Retrieve the image resolution of the sprite
                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;

                    // Set the width and height accordingly to the scale in the editor
                    A_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                    A_element.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image

                }
                A_element.SetAttribute("Type", "Catch"); //Type="Catch"/>
                A_element.SetAttribute("Distance", "0"); //Distance="0"/>
                areaNode = A_element;
            }
            else
            {
                XmlElement A_element = xml.CreateElement("Area"); //Create a new node from scratch
                A_element.SetAttribute("Name", Regex.Replace(areaInScene.name, @" \((.*?)\)", string.Empty)); //Add an name
                if (localPosition)
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }
                else
                {
                    A_element.SetAttribute("X", Math.Round(areaInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                    A_element.SetAttribute("Y", Math.Round(-areaInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
                }

                SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null) //Get the Sprite Size in Width and Height
                {

                    Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
                    Vector3 scale = areaInScene.transform.localScale; // Get the GameObject scale

                    // Retrieve the image resolution of the sprite
                    float width = bounds.size.x * 100;
                    float height = bounds.size.y * 100;

                    // Set the width and height accordingly to the scale in the editor
                    A_element.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
                    A_element.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image

                }
                A_element.SetAttribute("Type", "Animation"); //Type="Catch"/>
                areaNode = A_element;
            }



        }
    }

    void ConvertToCamera(XmlNode node, XmlDocument xml, GameObject camInScene, out XmlElement cameraNode, bool localPosition)
    {

        cameraNode = null;
        //Important Note: If the specific TriggerZoom already exists in the object.xml, no need to tag those as Camera. Instead, tag it as an object!



        // Debug in log every Area it writes
        if (debugObjectWriting)
            Debug.Log("Writing object : " + Regex.Replace(camInScene.name, @" \((.*?)\)", string.Empty));


        if (camInScene.name != "Camera") //kinda ironic
        {
            SpriteRenderer spriteRenderer = camInScene.GetComponent<SpriteRenderer>();
            CustomZoom customZoomValue = camInScene.GetComponent<CustomZoom>(); //Zoom value from object with tag "Camera" that have CustomZoom component
            Bounds bounds = spriteRenderer.sprite.bounds;// Get the bounds of the sprite
            Vector3 scale = camInScene.transform.localScale; // Get the GameObject scale
            // Retrieve the image resolution of the sprite
            float width = bounds.size.x * 100;
            float height = bounds.size.y * 100;

            //Trigger Childs
            XmlElement contentElement = xml.CreateElement("Content");
            XmlElement initElement = xml.CreateElement("Init");

            //trigger variable
            string[] variableNames = { "$Active", "$Node", "Zoom", "$AI", "Flag1" };
            string[] variableValues = { "1", "COM", customZoomValue.ZoomAmount.ToString(), "0", "0" };


            XmlElement triggerElement = xml.CreateElement("Trigger");
            triggerElement.SetAttribute("Name", Regex.Replace(camInScene.name, @" \((.*?)\)", string.Empty));
            if (localPosition)
            {
                triggerElement.SetAttribute("X", Math.Round(camInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                triggerElement.SetAttribute("Y", Math.Round(-camInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                triggerElement.SetAttribute("X", Math.Round(camInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                triggerElement.SetAttribute("Y", Math.Round(-camInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }

            triggerElement.SetAttribute("Width", (width * scale.x).ToString()); //Width of the Image
            triggerElement.SetAttribute("Height", (height * scale.y).ToString()); //Height of the Image

            //writes <content> and <init> under the trigger node
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
    }

    void ConvertToParticle(XmlNode node, XmlDocument xml, GameObject particleInScene, out XmlElement particleNode, bool localPosition)
    {
        particleNode = null;
        if (particleInScene.name != "Camera")
        {
            ParticleProperties particleProperties = particleInScene.GetComponent<ParticleProperties>();

            XmlElement Pelement = xml.CreateElement("Particle"); //Create a new node from scratch
            if (localPosition)
            {
                Pelement.SetAttribute("X", Math.Round(particleInScene.transform.localPosition.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                Pelement.SetAttribute("Y", Math.Round(-particleInScene.transform.localPosition.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            else
            {
                Pelement.SetAttribute("X", Math.Round(particleInScene.transform.position.x * 100).ToString().Replace(',', '.')); //Add X position (Refit into the Vector units)
                Pelement.SetAttribute("Y", Math.Round(-particleInScene.transform.position.y * 100).ToString().Replace(',', '.')); // Add Y position (Negative because Vector see the world upside down)
            }
            Pelement.SetAttribute("Width", particleProperties.Width.ToString());
            Pelement.SetAttribute("Height", particleProperties.Height.ToString());
            Pelement.SetAttribute("Frame", particleProperties.Frame.ToString());
            Pelement.SetAttribute("Type", particleProperties.Type.ToString());
            Pelement.SetAttribute("ClassName", Regex.Replace(particleInScene.name, @" \((.*?)\)", string.Empty)); //Add an name

            particleNode = Pelement;
        }
    }

    void ConvertToDynamic(XmlNode node, XmlDocument xml, GameObject dynamicInScene, UnityEngine.Transform dynamicInSceneTransform, out XmlElement dynamicNode)
    {
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

            // original MoveIntervals
            List<XmlElement> originalMoveIntervals = new List<XmlElement>();

            // Handle Move Intervals (1 to 5)
            for (int i = 1; i <= 5; i++)
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

        // image list for the dynamic
        List<GameObject> ImageObjects = new List<GameObject>();

        // add image to the list
        foreach (UnityEngine.Transform child in dynamicInSceneTransform)
        {
            if (child.gameObject.CompareTag("Image"))
            {
                ImageObjects.Add(child.gameObject);
            }
        }

        // sort the list based on order in layer
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


            if (spriteRenderer != null && spriteRenderer.sprite != null) // Check if SpriteRenderer exists
            {
                // Get Image Position (Multiply by 100 for Vector's Unit and Negate Y because Vector sees the world upside down)
                float imagePosX = imageObject.transform.position.x * 100;
                float imagePosY = -imageObject.transform.position.y * 100;

                // Get rotation degrees (Z-axis)
                float rotationAngle = imageObject.transform.eulerAngles.z % 360; // Normalize to 0-360 degrees

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
                    // bounding box size in world space
                    Bounds bounds = spriteRenderer.bounds;
                    float worldWidth = bounds.size.x * 100;
                    float worldHeight = bounds.size.y * 100;

                    // Get Native resolution of the sprite
                    int nativeWidth = spriteRenderer.sprite.texture.width;
                    int nativeHeight = spriteRenderer.sprite.texture.height;

                    // Get the image scale using sprite resolution * local scale
                    float imageWidth = nativeWidth * imageObject.transform.localScale.x;
                    float imageHeight = nativeHeight * imageObject.transform.localScale.y;

                    // matrix values
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
                    ielement.SetAttribute("X", Math.Round(topLeftX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(topLeftY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(imageObject.name, @" \((.*?)\)", string.Empty));
                    ielement.SetAttribute("Width", worldWidth.ToString().Replace(',', '.'));
                    ielement.SetAttribute("Height", worldHeight.ToString().Replace(',', '.'));
                    ielement.SetAttribute("NativeX", nativeWidth.ToString());
                    ielement.SetAttribute("NativeY", nativeHeight.ToString());


                    // Apply transformation matrix
                    matrixElement.SetAttribute("A", A.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("B", B.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("C", C.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("D", D.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Tx", Tx.ToString("F6").Replace(',', '.'));
                    matrixElement.SetAttribute("Ty", Ty.ToString("F6").Replace(',', '.'));

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
                    ielement.SetAttribute("X", Math.Round(imagePosX).ToString().Replace(',', '.'));
                    ielement.SetAttribute("Y", Math.Round(imagePosY).ToString().Replace(',', '.'));
                    ielement.SetAttribute("ClassName", Regex.Replace(imageObject.name, @" \((.*?)\)", string.Empty));
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
            //check if the gameobject has specific tag
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
                //Platform

                XmlElement platformNode;
                ConvertToPlatform(node, xml, childObject.gameObject, out platformNode, false);
                contentElement.AppendChild(platformNode);

            }

            else if (childObject.gameObject.CompareTag("Trapezoid"))
            {
                // Trapezoid
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
            default: return null;
        }
    }

}

