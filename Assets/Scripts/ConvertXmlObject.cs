using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// -=-=-=- //

public class ConvertXmlObject : EditorWindow
{
    private string filePath = "Assets/XML/dzip/level_xml/objects.xml";
    private string objectToConvert = "";
    private bool autoTag = true;
    private bool debugObjectFound = false;

    // Fields from the original ConvertXmlObject script
    private int orderInLayer = 0;
    private GameObject lastContent;
    private GameObject actualObject;
    private GameObject dummyObject;

    [MenuItem("Vectorier/Convert XML object")]
    public static void ShowWindow()
    {
        ConvertXmlObject window = GetWindow<ConvertXmlObject>("Convert XML Object");
        window.minSize = new Vector2(350, 210);
    }

    private void OnGUI()
    {
        // File Path Input
        GUILayout.BeginHorizontal();
        GUILayout.Label("File Path", GUILayout.Width(150));

        filePath = GUILayout.TextField(filePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedPath = EditorUtility.OpenFilePanel("Select XML File", "Assets/XML/dzip/level_xml", "xml");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Make sure we strip the absolute path and leave only the relative path
                string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);

                // Set the filePath to the relative path
                filePath = relativePath;
            }
        }
        GUILayout.EndHorizontal();

        // Object to Convert Input
        GUILayout.BeginHorizontal();
        GUILayout.Label("Object to Convert", GUILayout.Width(150));
        objectToConvert = GUILayout.TextField(objectToConvert);
        GUILayout.EndHorizontal();

        // Warning label about case-sensitivity
        EditorGUILayout.HelpBox("Case-sensitive.", MessageType.Warning);

        GUILayout.Label("");

        // Auto Tag Checkbox
        autoTag = EditorGUILayout.Toggle("Auto Tag", autoTag);

        // Debug Object Found Checkbox
        debugObjectFound = EditorGUILayout.Toggle("Debug Object Found", debugObjectFound);

        GUILayout.Label("");

        // Convert Button
        if (GUILayout.Button("Convert", GUILayout.Height(50)))
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(objectToConvert))
            {
                Debug.LogError("Please provide both the file path and object to convert");
            }
            else
            {
                ConvertXmlToObject(filePath, objectToConvert);
            }
        }
    }

	private void ConvertXmlToObject(string path, string objectToConvert)
	{
		Debug.Log("Converting..");

		if (string.IsNullOrEmpty(path))
		{
			Debug.LogError("No XML file selected!");
			return;
		}

		XmlDocument obj = new XmlDocument();
		try
		{
			obj.Load(path);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to load XML file: {e.Message}");
			return;
		}

		bool objectFound = false;
		int docNum = 0;

		while (!objectFound && docNum < 3)
		{
			foreach (XmlNode node in obj.DocumentElement.SelectSingleNode("/Root/Objects"))
			{
				if (node.Name == "Object" && node.Attributes["Name"].Value == objectToConvert)
				{
					objectFound = true;
					Debug.Log("Object found and processed!");
					orderInLayer = 0;

					// Extract variables
					Dictionary<string, float> variables = ExtractVariables(node);

					// --- CREATE THE PARENT OBJECT ---
					DestroyImmediate(GameObject.Find(objectToConvert));
					actualObject = new GameObject(objectToConvert);
					actualObject.transform.position = Vector3.zero;
					actualObject.name = objectToConvert;

					// --- CHECK IF OBJECT IS DYNAMIC ---
					XmlNode parentDynamicNode = node.SelectSingleNode("Properties/Dynamic");
					bool isDynamic = parentDynamicNode != null;

					// --- TAG LOGIC ---
					if (isDynamic)
					{
						if (autoTag)
							actualObject.tag = "Dynamic";
						else
							actualObject.tag = "Dynamic"; // always Dynamic even without autoTag
					}
					else
					{
						if (autoTag)
							actualObject.tag = "Object";
						else
							actualObject.tag = "Object";
					}

					// --- APPLY DYNAMIC COMPONENT (if applicable) ---
					if (isDynamic)
					{
						ApplyDynamicFromXml(parentDynamicNode, actualObject);
					}

					// --- CREATE CONTENT CHILDREN ---
					XmlNode contentNode = node.SelectSingleNode("Content");
					if (contentNode != null)
					{
						foreach (XmlNode content in contentNode.ChildNodes)
						{
							StoreExtraArguments(orderInLayer, autoTag, debugObjectFound);
							InstantiateObject(content, obj, variables, false, null);
						}
					}

					// --- IF NON-DYNAMIC: TAG ALL CHILDREN "Unused" ---
					if (!isDynamic)
					{
						TagAllChildrenAsUnused(actualObject);
					}
				}
			}
			docNum++;
		}

		if (!objectFound)
		{
			Debug.LogError("Object not found in the XML files");
		}
		else
		{
			Debug.Log("Conversion done!");
			actualObject = null;
		}
	}

    private Dictionary<string, float> ExtractVariables(XmlNode objectNode)
    {
        Dictionary<string, float> variables = new Dictionary<string, float>();
        XmlNode propertiesNode = objectNode.SelectSingleNode("Properties/Static/ContentVariable");

        if (propertiesNode != null)
        {
            foreach (XmlNode variableNode in propertiesNode.SelectNodes("Variable"))
            {
                string name = variableNode.Attributes["Name"].Value;
                float value = float.Parse(variableNode.Attributes["Default"].Value);
                variables.Add(name, value);
            }
        }

        return variables;
    }

	private void InstantiateObject(XmlNode content, XmlDocument xmlDoc, Dictionary<string, float> variables, bool useParent, GameObject parent)
	{
		orderInLayer = RetrieveStoredOrderInLayer();
		autoTag = RetrieveStoredAutoTag();
		debugObjectFound = RetrieveStoredDebugObjectFound();

		// Replace variable placeholders with actual values
		ReplacePlaceholders(content, variables);

		if (actualObject == null)
		{
			actualObject = Instantiate(new GameObject(objectToConvert), Vector3.zero, Quaternion.identity);
			DestroyImmediate(GameObject.Find(objectToConvert));
			actualObject.name = objectToConvert;
		}

		if (!autoTag)
		{
			actualObject.tag = "Object";
		}

		// ====================== OBJECT NODE ======================
		if (content.Name == "Object")
		{
			XmlAttribute referencedObjectName = content.Attributes["Name"];

			if (referencedObjectName != null)
			{
				if (debugObjectFound)
					Debug.Log($"Found {content.Name}: {referencedObjectName.Value}");

				// If the object has nested Content
				if (content.SelectSingleNode("Content") != null)
				{
					XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value, xmlDoc);

					Vector3 position = new Vector3(
						float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
						-float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
						0
					);

					GameObject nestedObject = new GameObject(referencedObjectName.Value);
					nestedObject.transform.position = position;
					if (useParent)
						nestedObject.transform.SetParent(parent.transform, false);
					else
						nestedObject.transform.parent = actualObject.transform;

					// 🟢 Inject Dynamic processing
					XmlNode dynamicNode = content.SelectSingleNode("Properties/Dynamic");
					if (dynamicNode != null)
						ApplyDynamicFromXml(dynamicNode, nestedObject);

					// Recursively instantiate referenced content
					if (referencedObjectNode != null)
					{
						foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
							InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
					}

					foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
						InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
				}
				else
				{
					// Object without local content — use referenced definition
					XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value, xmlDoc);
					if (referencedObjectNode != null)
					{
						Vector3 position = new Vector3(
							float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
							-float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
							0
						);

						GameObject nestedObject = new GameObject(referencedObjectName.Value);
						nestedObject.transform.position = position;
						if (useParent)
							nestedObject.transform.SetParent(parent.transform, false);
						else
							nestedObject.transform.parent = actualObject.transform;

						// 🟢 Inject Dynamic processing
						XmlNode dynamicNode = content.SelectSingleNode("Properties/Dynamic");
						if (dynamicNode != null)
							ApplyDynamicFromXml(dynamicNode, nestedObject);

						foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
							InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
					}
					else
					{
						Debug.LogError($"Referenced object '{referencedObjectName.Value}' not found in the XML file");
					}
				}
			}
			else
			{
				// Anonymous inline object
				if (debugObjectFound)
					Debug.Log($"Found {content.Name}");

				Vector3 position = new Vector3(
					float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
					-float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
					0
				);

				GameObject nestedObject = new GameObject(string.Empty);
				nestedObject.transform.position = position;
				if (useParent)
					nestedObject.transform.SetParent(parent.transform, false);
				else
					nestedObject.transform.parent = actualObject.transform;

				// 🟢 Inject Dynamic processing
				XmlNode dynamicNode = content.SelectSingleNode("Properties/Dynamic");
				if (dynamicNode != null)
					ApplyDynamicFromXml(dynamicNode, nestedObject);

				foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
					InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
			}
			return;
		}

		// ====================== IMAGE NODE ======================
		if (content.Name == "Image")
		{
			HandleImageNode(content, variables);
			if (debugObjectFound)
				Debug.Log($"Found {content.Name}: {content.Attributes["ClassName"].Value}");

			if (autoTag) lastContent.tag = "Image";
			else lastContent.tag = "Unused";
		}
		else if (content.Name == "Platform")
		{
			HandlePlatformNode(content);
			if (debugObjectFound)
				Debug.Log($"Found {content.Name}");

			if (autoTag) lastContent.tag = "Platform";
			else lastContent.tag = "Unused";
		}
		else if (content.Name == "Trapezoid")
		{
			if (debugObjectFound)
				Debug.Log($"Found {content.Name}");
			HandleTrapezoidNode(content);

			if (autoTag) lastContent.tag = "Trapezoid";
			else lastContent.tag = "Unused";
		}
		else if (content.Name == "Trigger")
		{
			if (debugObjectFound)
				Debug.Log($"Found {content.Name}: {content.Attributes["Name"].Value}");
			HandleTriggerNode(content);

			if (autoTag) lastContent.tag = "Trigger";
			else lastContent.tag = "Unused";
		}
		else if (content.Name == "Area")
		{
			if (debugObjectFound)
				Debug.Log($"Found {content.Name}: {content.Attributes["Name"].Value}");
			HandleAreaNode(content);

			if (autoTag) lastContent.tag = "Area";
			else lastContent.tag = "Unused";
		}
		else
		{
			return;
		}

		// Finalize
		lastContent.name = lastContent.name.Replace("(Clone)", string.Empty);
		if (useParent)
			lastContent.transform.SetParent(parent.transform, false);
		else
			lastContent.transform.parent = actualObject.transform;

		DestroyImmediate(dummyObject);
	}

    private int storedOrderInLayer;
    private bool storedAutoTag;
    private bool storedDebugObjectFound;

    // Meta

    private void StoreExtraArguments(int orderInLayer, bool autoTag, bool debugObjectFound)
    {
        storedOrderInLayer = orderInLayer;
        storedAutoTag = autoTag;
        storedDebugObjectFound = debugObjectFound;
    }

    private int RetrieveStoredOrderInLayer()
    {
        return storedOrderInLayer;
    }

    private bool RetrieveStoredAutoTag()
    {
        return storedAutoTag;
    }

    private bool RetrieveStoredDebugObjectFound()
    {
        return storedDebugObjectFound;
    }

    static float EvaluateExpression(string expression, Dictionary<string, float> variables)
    {
        // Remove the curly braces if they exist
        if (expression.StartsWith("{") && expression.EndsWith("}"))
        {
            expression = expression.Substring(1, expression.Length - 2);
        }

        // Replace variable placeholders in the expression with actual values
        string evaluatedExpression = Regex.Replace(expression, @"~(\w+)", match => {
            string varName = match.Groups[1].Value;
            return variables.ContainsKey(varName) ? variables[varName].ToString() : "0";
        });

        // Evaluate and return as a float
        return EvaluateSimpleMath(evaluatedExpression);
    }

    static float EvaluateSimpleMath(string expression)
    {
        try
        {
            var dataTable = new System.Data.DataTable();
            var result = dataTable.Compute(expression, "");
            return Convert.ToSingle(result);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to evaluate expression: {expression}. Error: {ex.Message}");
            return 0;
        }
    }

    void ReplacePlaceholders(XmlNode node, Dictionary<string, float> variables)
    {
        foreach (XmlAttribute attribute in node.Attributes)
        {
            if (attribute.Value.StartsWith("~"))
            {
                string variableName = attribute.Value.Substring(1);
                if (variables.ContainsKey(variableName))
                {
                    attribute.Value = variables[variableName].ToString();
                }
                else
                {
                    Debug.LogError($"Variable '{variableName}' not found in the variables dictionary");
                }
            }
        }

        if (node.Name == "Matrix")
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Value.StartsWith("~"))
                {
                    string variableName = attribute.Value.Substring(1);
                    if (variables.ContainsKey(variableName))
                    {
                        attribute.Value = variables[variableName].ToString();
                    }
                    else
                    {
                        Debug.LogError($"Matrix Variable '{variableName}' not found in the variables dictionary");
                    }
                }
            }
        }
    }

    XmlNode FindObjectNodeByName(string objectName, XmlDocument xmlDoc)
    {
        XmlDocument xmlDoc2 = new XmlDocument();
        xmlDoc2.Load(Application.dataPath + "/XML/dzip/level_xml/objects.xml");

        foreach (XmlNode node in xmlDoc2.DocumentElement.SelectSingleNode("/Root/Objects"))
        {
            if (node.Name == "Object" && node.Attributes.GetNamedItem("Name").Value == objectName)
            {
                return node;
            }
        }
        foreach (XmlNode node in xmlDoc.DocumentElement.SelectSingleNode("/Root/Objects"))
        {
            if (node.Name == "Object" && node.Attributes.GetNamedItem("Name").Value == objectName)
            {
                return node;
            }
        }
        return null;
    }

    Sprite LoadSpriteFromResources(string spriteName)
    {
        // Convert spriteName to lowercase
        string lowerCaseSpriteName = spriteName.ToLower();

        Sprite[] allSprites = Resources.LoadAll<Sprite>("Textures");

        return allSprites.FirstOrDefault(sprite => sprite.name.ToLower() == lowerCaseSpriteName);
    }


	private void ApplyDynamicFromXml(XmlNode dynamicNode, GameObject targetObject)
	{
		if (dynamicNode == null || targetObject == null) return;

		foreach (XmlNode transformationNode in dynamicNode.SelectNodes("Transformation"))
		{
			Dynamic dynamicComponent = targetObject.AddComponent<Dynamic>();
			string nameAttr = transformationNode.Attributes["Name"]?.Value ?? "Unnamed";
			dynamicComponent.TransformationName = nameAttr;

			XmlNode moveNode = transformationNode.SelectSingleNode("Move");
			if (moveNode == null) continue;

			// Initialize MovementUsage
			var useCheck = new Dynamic.UseCheck();
			dynamicComponent.MovementUsage = useCheck;

			int index = 1;
			foreach (XmlNode moveInterval in moveNode.SelectNodes("MoveInterval"))
			{
				if (index > 16) break;

				// Enable corresponding movement boolean (UseMovement1..16)
				var useField = typeof(Dynamic.UseCheck).GetField($"UseMovement{index}");
				if (useField != null) useField.SetValue(useCheck, true);

				int framesToMove = 60;
				int delayFrames = 0;
				if (moveInterval.Attributes["FramesToMove"] != null)
					int.TryParse(moveInterval.Attributes["FramesToMove"].Value, out framesToMove);
				if (moveInterval.Attributes["Delay"] != null)
					int.TryParse(moveInterval.Attributes["Delay"].Value, out delayFrames);

				float duration = framesToMove / 60f;
				float delay = delayFrames / 60f;

				XmlNode supportPoint = moveInterval.SelectSingleNode("Point[@Name='Support']");
				XmlNode finishPoint = moveInterval.SelectSingleNode("Point[@Name='Finish']");

				float supportX = 0f;
				float supportY = 0f;
				float moveX = 0f;
				float moveY = 0f;

				if (supportPoint != null)
				{
					float.TryParse(supportPoint.Attributes["X"]?.Value ?? "0", out supportX);
					float.TryParse(supportPoint.Attributes["Y"]?.Value ?? "0", out supportY);
				}
				if (finishPoint != null)
				{
					float.TryParse(finishPoint.Attributes["X"]?.Value ?? "0", out moveX);
					float.TryParse(finishPoint.Attributes["Y"]?.Value ?? "0", out moveY);
				}

				// Convert from XML units (which your project uses *100 and inverted Y)
				supportX = supportX / 100f;
				supportY = -supportY / 100f;
				moveX = moveX / 100f;
				moveY = -moveY / 100f;

				Dynamic.MovementGroup.Movement movement = new Dynamic.MovementGroup.Movement
				{
					MoveDuration = duration,
					Delay = delay,
					SupportXAxis = supportX,
					SupportYAxis = supportY,
					MoveXAxis = moveX,
					MoveYAxis = moveY
				};

				var moveField = typeof(Dynamic).GetField($"MoveInterval{index}");
				if (moveField != null) moveField.SetValue(dynamicComponent, movement);

				index++;
			}

			if (debugObjectFound)
				Debug.Log($"Dynamic added: {dynamicComponent.TransformationName} ({index - 1} intervals) on {targetObject.name}");
		}
	}

	private void TagAllChildrenAsUnused(GameObject parent)
	{
		foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
		{
			if (child == parent.transform) continue; // skip the root
			child.gameObject.tag = "Unused";
		}
	}

    // -=-=-=- //

    // Images
    void HandleImageNode(XmlNode content, Dictionary<string, float> variables)
    {
        ReplacePlaceholders(content, variables);

        string className = content.Attributes.GetNamedItem("ClassName").Value.ToLower();

        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        lastContent = Instantiate(
            dummyObject = new GameObject(className),
            position,
            Quaternion.identity
        );

        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadSpriteFromResources(className);

        // Set order in layer for the image
        spriteRenderer.sortingOrder = orderInLayer;
        orderInLayer++;

        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
        lastContent.transform.localScale = scale;

        // Check if there are Matrix transformations
        if (content.HasChildNodes)
        {
            XmlNode matrixNode = content.SelectSingleNode("Properties/Static/Matrix");
            XmlNode weirdmatrixNode = content.SelectSingleNode("Properties/Static/MatrixTransform");
            if (matrixNode != null)
            {
                Debug.Log($"Matrix node found. Attempting to apply transformation");
                ConvertFromMarmaladeMatrix(matrixNode, lastContent.transform, spriteRenderer, width, height, lastContent, className);
            }
            else if (weirdmatrixNode != null)
            {
                Debug.Log($"Matrix node found. Attempting to apply transformation");
                ConvertFromMarmaladeMatrix(weirdmatrixNode, lastContent.transform, spriteRenderer, width, height, lastContent, className);
            }
        }
    }

    // Trapezoid
    void HandleTrapezoidNode(XmlNode content)
    {
        // Parse position
        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        // Parse trapezoid type
        int t_type = int.Parse(content.Attributes.GetNamedItem("Type").Value);

        // Create the trapezoid object
        lastContent = Instantiate(
            dummyObject = new GameObject("Trapezoid"),
            position,
            Quaternion.identity
        );

        // Add the sprite renderer and load the sprite
        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trapezoid_type" + t_type);

        // Set order in layer
        spriteRenderer.sortingOrder = 1000;

        // Compute and apply size
        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);
        float height1 = float.Parse(content.Attributes.GetNamedItem("Height1").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        float type1height = height1 - height;
        float type2height = height - height1;

        if (t_type == 1)
        {
            Vector3 scale = new Vector3(width / originalWidth, type1height / originalHeight);
            lastContent.transform.localScale = scale;
        }
        else if (t_type == 2)
        {
            Vector3 scale = new Vector3(width / originalWidth, type2height / originalHeight);
            lastContent.transform.localScale = scale;
        }

    }

    // Platforms
    void HandlePlatformNode(XmlNode content)
    {
        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        lastContent = Instantiate(
            dummyObject = new GameObject("Platform"),
            position,
            Quaternion.identity
        );

        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("Textures/collision");

        // Set order in layer to 253 for the platform
        spriteRenderer.sortingOrder = 253;

        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
        lastContent.transform.localScale = scale;
    }

    // Trigger
    void HandleTriggerNode(XmlNode content)
    {
        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        XmlAttribute name = content.Attributes["Name"];

        lastContent = Instantiate(
            dummyObject = new GameObject(),
            position,
            Quaternion.identity
        );
        
        if ( name != null)
        {
            lastContent.name = name.Value;
        }
        else
        {
            lastContent.name = string.Empty;
        }


        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trigger");

        // Set order in layer to 255 for the trigger
        spriteRenderer.sortingOrder = 255;

        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
        lastContent.transform.localScale = scale;

        // Add the TriggerSettings component
        TriggerSettings triggerSettings = lastContent.AddComponent<TriggerSettings>();

        // Copy the content of the <Trigger> node to the TriggerSettings component
		XmlNode contentNode = content.SelectSingleNode("Content");
		if (contentNode != null)
		{
			string innerXml = contentNode.InnerXml;
			triggerSettings.Content = BeautifyXmlContent(innerXml);
		}

    }

	private static string BeautifyXmlContent(string rawXml)
	{
		if (string.IsNullOrWhiteSpace(rawXml)) return rawXml;

		try
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml($"<Root>{rawXml}</Root>");

			var settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineHandling = NewLineHandling.None,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			};

			using (var sw = new StringWriter())
			using (var xw = XmlWriter.Create(sw, settings))
			{
				foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
					node.WriteTo(xw);

				xw.Flush();
				return sw.ToString().Replace(" />", "/>");
			}
		}
		catch
		{
			// Fail gracefully if XML is malformed
			return rawXml;
		}
	}

    // Area
    void HandleAreaNode(XmlNode content)
    {
        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        lastContent = Instantiate(
            dummyObject = new GameObject(content.Attributes.GetNamedItem("Name").Value),
            position,
            Quaternion.identity
        );

        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trick");

        // Set order in layer to 254 for the Area
        spriteRenderer.sortingOrder = 254;

        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
        lastContent.transform.localScale = scale;
    }

    // Calculation
    void ConvertFromMarmaladeMatrix(XmlNode matrixNode, UnityEngine.Transform transform, SpriteRenderer spriteRenderer, float xmlWidth, float xmlHeight, GameObject image, string className)
    {
        _DefautPosition = transform.localPosition;

        _Transformation[0, 0] = float.Parse(matrixNode.Attributes["A"].Value);
        _Transformation[0, 1] = -float.Parse(matrixNode.Attributes["B"].Value);
        _Transformation[1, 0] = -float.Parse(matrixNode.Attributes["C"].Value);
        _Transformation[1, 1] = float.Parse(matrixNode.Attributes["D"].Value);
        _Transformation[2, 2] = 1f;
        _Transformation[3, 3] = 1f;
        _DefautPosition.X += float.Parse(matrixNode.Attributes["Tx"].Value) / 100;
        _DefautPosition.Y += -float.Parse(matrixNode.Attributes["Ty"].Value) / 100;

        Matrix4x4 transpose = this._Transformation.transpose;
        QRDecomposition qrdecomposition = new QRDecomposition(transpose);
        if (qrdecomposition.ContainsSkew())
        {
            int orderinLayer = spriteRenderer.sortingOrder;
            DestroyImmediate(lastContent.GetComponent<SpriteRenderer>());

            _SupportUnityObject = new GameObject
            {
                name = "Support"
            };
            _SupportUnityObject.transform.SetParent(transform, false);
            SpriteRenderer sprite = _SupportUnityObject.AddComponent<SpriteRenderer>();
            sprite.sprite = LoadSpriteFromResources(className);
            sprite.sortingOrder = orderinLayer;
            _SupportUnityObject.tag = "Unused";

            AffineDecomposition affineDecomposition = new AffineDecomposition(this._Transformation);
            _SupportUnityObject.transform.localScale = new Vector3(affineDecomposition.ScaleX1 / sprite.sprite.bounds.size.x / 100, affineDecomposition.ScaleY1 / sprite.sprite.bounds.size.x / 100, 1f);
            _SupportUnityObject.transform.Rotate(0f, 0f, affineDecomposition.Angle1);
            transform.localScale = new Vector3(affineDecomposition.ScaleX2, affineDecomposition.ScaleY2, 1f);
            transform.Rotate(0f, 0f, affineDecomposition.Angle2);

            transform.localPosition = _DefautPosition;
        }
        else
        {
            Matrix4x4 rotation = qrdecomposition.Rotation;
            Quaternion localRotation = default(Quaternion);
            int num = (rotation[0, 0] * rotation[1, 1] - rotation[0, 1] * rotation[1, 0] <= 0f) ? -1 : 1;
            if (num < 0)
            {
                localRotation = Quaternion.LookRotation(-rotation.GetColumn(2), rotation.GetColumn(1));
            }
            else
            {
                localRotation = Quaternion.LookRotation(rotation.GetColumn(2), rotation.GetColumn(1));
            }
            transform.localRotation = localRotation;
            transform.localScale = new Vector3(qrdecomposition.ScaleX / spriteRenderer.sprite.bounds.size.x / 100, qrdecomposition.ScaleY / spriteRenderer.sprite.bounds.size.y / 100, 1f);
            transform.localPosition = _DefautPosition;
        }

    }

    protected Vector3f _DefautPosition = new Vector3f(0f, 0f, -800f);
    protected Matrix4x4 _Transformation = Matrix4x4.identity;
    protected GameObject _SupportUnityObject;
}

