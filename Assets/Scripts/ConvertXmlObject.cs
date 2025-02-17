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
                    // Reset orderInLayer
                    orderInLayer = 0;

                    // Extract variables and expressions from the Properties section
                    Dictionary<string, float> variables = ExtractVariables(node);

                    foreach (XmlNode content in node["Content"])
                    {
                        StoreExtraArguments(orderInLayer, autoTag, debugObjectFound);
                        InstantiateObject(content, obj, variables, false, null);
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
        // Use class fields directly, no redeclarations
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

        // Object
        if (content.Name == "Object")
        {

            XmlAttribute referencedObjectName = content.Attributes["Name"];
            if (referencedObjectName != null)
            {
                if (debugObjectFound)
                {
                    Debug.Log($"Found {content.Name}: {referencedObjectName.Value}");
                }

                // Check if the object has child nodes (nested Content)
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
                    {
                        nestedObject.transform.SetParent(parent.transform, false);
                    }
                    else
                    {
                        nestedObject.transform.parent = actualObject.transform;
                    }

                    if (referencedObjectNode != null)
                    {

                        // Recursively instantiate nested objects
                        foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
                        {
                            InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
                        }
                    }

                    // Recursively instantiate nested objects
                    foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
                    {
                        InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
                    }
                }
                else
                {
                    // This is a simple one-line object, find its definition in the XML and instantiate
                    XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value, xmlDoc);
                    if (referencedObjectNode != null)
                    {
                        Vector3 position = new Vector3(
                            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                            0
                        );

                        // Temporarily set the position of the actualObject to match the reference position
                        GameObject nestedObject = new GameObject(referencedObjectName.Value);
                        nestedObject.transform.position = position;
                        if (useParent)
                        {
                            nestedObject.transform.SetParent(parent.transform, false);
                        }
                        else
                        {
                            nestedObject.transform.parent = actualObject.transform;
                        }

                        // Recursively instantiate nested objects
                        foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
                        {
                            InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Referenced object '{referencedObjectName.Value}' not found in the XML file");
                    }
                }
            }
            else
            {
                if (debugObjectFound)
                {
                    Debug.Log($"Found {content.Name}");
                }
                Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

                GameObject nestedObject = new GameObject(string.Empty);
                nestedObject.transform.position = position;
                if (useParent)
                {
                    nestedObject.transform.SetParent(parent.transform, false);
                }
                else
                {
                    nestedObject.transform.parent = actualObject.transform;
                }

                // Recursively instantiate nested objects
                foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
                {
                    InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject);
                }
            }
            return;
        }

        if (content.Name == "Image")
        {
            // Images
            HandleImageNode(content, variables);

            if (debugObjectFound)
            {
                Debug.Log($"Found {content.Name}: {content.Attributes["ClassName"].Value}");
            }

            if (autoTag)
            {
                lastContent.tag = "Image";
            }
            else
            {
                lastContent.tag = "Unused";
            }
        }
        else if (content.Name == "Platform")
        {
            // Platform
            HandlePlatformNode(content);

            if (debugObjectFound)
            {
                Debug.Log($"Found {content.Name}");
            }

            if (autoTag)
            {
                lastContent.tag = "Platform";
            }
            else
            {
                lastContent.tag = "Unused";
            }
        }
        else if (content.Name == "Trapezoid")
        {
            if (debugObjectFound)
            {
                Debug.Log($"Found {content.Name}");
            }
            // Trapezoid
            HandleTrapezoidNode(content);

            if (autoTag)
            {
                lastContent.tag = "Trapezoid";
            }
            else
            {
                lastContent.tag = "Unused";
            }
        }
        else if (content.Name == "Trigger")
        {
            if (debugObjectFound)
            {
                Debug.Log($"Found {content.Name}: {content.Attributes["Name"].Value}");
            }
            // Trigger
            HandleTriggerNode(content);

            if (autoTag)
            {
                lastContent.tag = "Trigger";
            }
            else
            {
                lastContent.tag = "Unused";
            }
        }
        else if (content.Name == "Area")
        {
            if (debugObjectFound)
            {
                Debug.Log($"Found {content.Name}: {content.Attributes["Name"].Value}");
            }
            // Area
            HandleAreaNode(content);

            if (autoTag)
            {
                lastContent.tag = "Area";
            }
            else
            {
                lastContent.tag = "Unused";
            }
        }
        else
        {
            return;
        }

        // Remove "(Clone)" from object's name
        lastContent.name = lastContent.name.Replace("(Clone)", string.Empty);
        if (useParent)
        {
            lastContent.transform.SetParent(parent.transform, false);
        }
        else
        {
            lastContent.transform.parent = actualObject.transform;
        }
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
            triggerSettings.Content = contentNode.InnerXml;
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
