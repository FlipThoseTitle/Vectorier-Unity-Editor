using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;


// Important Note: Some of these method are used for Vector 2 xml files. (EXPERIMENTAL)


public class ConvertXmlObject : MonoBehaviour
{
    public string objectToConvert;

    [Tooltip("if autotag is true, then the imported object will be automatically tagged accordingly to their own tag.")]
    public bool autoTag;

    public bool debugObjectFound;
    GameObject lastContent;
    GameObject actualObject;
    GameObject dummyObject;
    int orderInLayer = 0;

    [MenuItem("Vectorier/Import Object from .XML")]
    public static void ConvertXmlToObject()
    {
        Debug.Log("Converting...");

        string path = EditorUtility.OpenFilePanel("Select XML File", Application.dataPath, "xml");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("No XML file selected!");
            return;
        }

        // Load the selected XML file
        XmlDocument obj = new XmlDocument();
        obj.Load(path);

        bool objectFound = false;
        int doc_num = 0;

        // Search for the selected object in the object XMLs
        while (!objectFound && doc_num < 3)
        {
            foreach (XmlNode node in obj.DocumentElement.SelectSingleNode("/Root/Objects"))
            {
                // Check if the object has the correct name
                if (node.Name == "Object" && node.Attributes.GetNamedItem("Name").Value == GameObject.FindObjectOfType<ConvertXmlObject>().objectToConvert)
                {
                    objectFound = true;

                    // reset orderInLayer
                    GameObject.FindObjectOfType<ConvertXmlObject>().orderInLayer = 0;

                    // Extract variables and expressions from the Properties section
                    Dictionary<string, float> variables = ExtractVariables(node);

                    foreach (XmlNode content in node.FirstChild)
                    {
                        GameObject.FindObjectOfType<ConvertXmlObject>().InstantiateObject(content, obj, variables);
                    }
                }
            }
            doc_num += 1;
        }

        if (!objectFound)
        {
            Debug.LogError("Object not found in the XML files.");
        }
        else
        {
            GameObject.FindObjectOfType<ConvertXmlObject>().actualObject = null;
            Debug.Log("Convert done!");
        }
    }


    static Dictionary<string, float> ExtractVariables(XmlNode objectNode)
    {
        Dictionary<string, float> variables = new Dictionary<string, float>();

        XmlNode propertiesNode = objectNode.SelectSingleNode("Properties/Static/ContentVariable");

        if (propertiesNode != null)
        {
            // Extract variables
            foreach (XmlNode variableNode in propertiesNode.SelectNodes("Variable"))
            {
                string name = variableNode.Attributes["Name"].Value;
                float value = float.Parse(variableNode.Attributes["Default"].Value);
                variables.Add(name, value);
            }

            // Extract and evaluate expressions
            foreach (XmlNode expressionNode in propertiesNode.SelectNodes("Expression"))
            {
                string name = expressionNode.Attributes["Name"].Value;
                string expression = expressionNode.Attributes["Value"].Value;

                // Replace variable placeholders in the expression with actual values
                string evaluatedExpression = Regex.Replace(expression, @"~(\w+)", match =>
                {
                    string varName = match.Groups[1].Value;
                    return variables.ContainsKey(varName) ? variables[varName].ToString() : "0";
                });

                // Evaluate the expression and update the dictionary
                float result = EvaluateExpression(evaluatedExpression, variables);
                variables[name] = result;
            }
        }

        return variables;
    }






    // -=-=-=- //




    static float EvaluateExpression(string expression, Dictionary<string, float> variables)
    {
        // Remove the curly braces if they exist
        if (expression.StartsWith("{") && expression.EndsWith("}"))
        {
            expression = expression.Substring(1, expression.Length - 2);
        }

        // Replace variable placeholders in the expression with actual values
        string evaluatedExpression = Regex.Replace(expression, @"~(\w+)", match =>
        {
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


    void InstantiateObject(XmlNode content, XmlDocument xmlDoc, Dictionary<string, float> variables)
    {

        if (debugObjectFound)
        {
            Debug.Log($"Found {content.Name}: {content.Attributes.GetNamedItem("ClassName")?.Value ?? content.Attributes.GetNamedItem("Name").Value}");
        }

        // Replace variable placeholders with actual values
        ReplacePlaceholders(content, variables);

        if (actualObject == null)
        {
            actualObject = Instantiate(new GameObject(objectToConvert), Vector3.zero, Quaternion.identity);
            DestroyImmediate(GameObject.Find(objectToConvert));
            actualObject.name = objectToConvert;
        }

        // Object
        if (content.Name == "Object")
        {

            string referencedObjectName = content.Attributes.GetNamedItem("Name").Value;

            // Check if the object has child nodes (nested Content)
            if (content.SelectSingleNode("Content") != null)
            {
                Vector3 position = new Vector3(
                    float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                    -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                    0
                );

                GameObject nestedObject = new GameObject(referencedObjectName);
                nestedObject.transform.position = position;
                nestedObject.transform.parent = actualObject.transform;

                // Recursively instantiate nested objects
                foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
                {
                    InstantiateObject(nestedContent, xmlDoc, variables);
                }
            }
            else
            {
                // This is a simple one-line object, find its definition in the XML and instantiate
                XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName, xmlDoc);
                if (referencedObjectNode != null)
                {
                    Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

                    // Temporarily set the position of the actualObject to match the reference position
                    actualObject.transform.position = position;

                    // Recursively instantiate nested objects
                    foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
                    {
                        InstantiateObject(nestedContent, xmlDoc, variables);
                    }

                    // Reset actualObject position after instantiating the nested objects
                    actualObject.transform.position = Vector3.zero;
                }
                else
                {
                    Debug.LogError($"Referenced object '{referencedObjectName}' not found in the XML file.");
                }
            }
            return;
        }

        // Images
        if (content.Name == "Image")
        {
            HandleImageNode(content, variables);

            if (autoTag)
            {
                lastContent.tag = "Image";
            }

        }
        // Platform
        else if (content.Name == "Platform")
        {

            HandlePlatformNode(content);

            if (autoTag)
            {
                lastContent.tag = "Platform";
            }
        }

        // Trigger
        else if (content.Name == "Trigger")
        {

            HandleTriggerNode(content);

            if (autoTag)
            {
                lastContent.tag = "Trigger";
            }

        }

        // Area
        else if (content.Name == "Area")
        {
            HandleAreaNode(content);

            if (autoTag)
            {
                lastContent.tag = "Area";
            }
        }


        // Remove "(Clone)" from object's name
        lastContent.name = lastContent.name.Replace("(Clone)", string.Empty);

        lastContent.transform.parent = actualObject.transform;
        DestroyImmediate(dummyObject);
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
                    Debug.LogError($"Variable '{variableName}' not found in the variables dictionary.");
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
                        Debug.LogError($"Matrix Variable '{variableName}' not found in the variables dictionary.");
                    }
                }
            }
        }
    }

    XmlNode FindObjectNodeByName(string objectName, XmlDocument xmlDoc)
    {
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
            if (matrixNode != null)
            {
                Debug.Log($"Matrix node found. Attempting to apply transformation.");
                ConvertFromMarmaladeMatrix(matrixNode, lastContent.transform, spriteRenderer, width, height);
            }
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

        lastContent = Instantiate(
            dummyObject = new GameObject(content.Attributes.GetNamedItem("Name").Value),
            position,
            Quaternion.identity
        );


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
    void ConvertFromMarmaladeMatrix(XmlNode matrixNode, UnityEngine.Transform transform, SpriteRenderer spriteRenderer, float xmlWidth, float xmlHeight)
    {
        string aValue = matrixNode.Attributes.GetNamedItem("A")?.Value;
        string dValue = matrixNode.Attributes.GetNamedItem("D")?.Value;
        string txValue = matrixNode.Attributes.GetNamedItem("Tx")?.Value;
        string tyValue = matrixNode.Attributes.GetNamedItem("Ty")?.Value;


        Debug.Log($"A: {aValue}, D: {dValue}, Tx: {txValue}, Ty: {tyValue}");

        if (string.IsNullOrEmpty(aValue) || string.IsNullOrEmpty(dValue) || string.IsNullOrEmpty(txValue) || string.IsNullOrEmpty(tyValue))
        {
            Debug.LogError("One of the matrix values is null or empty.");
            return;
        }

        try
        {
            float A = float.Parse(aValue);
            float D = float.Parse(dValue);
            float Tx = float.Parse(txValue) / 100;
            float Ty = -float.Parse(tyValue) / 100;

            float originalWidth = spriteRenderer.sprite.bounds.size.x * 100f;
            float originalHeight = spriteRenderer.sprite.bounds.size.y * 100f;

            float scaleX = A / originalWidth;
            float scaleY = D / originalHeight;

            float expectedWidth = Mathf.Abs(scaleX * originalWidth);
            float expectedHeight = Mathf.Abs(scaleY * originalHeight);

            if (Mathf.Abs(expectedWidth - xmlWidth) > Mathf.Epsilon)
            {
                scaleX *= xmlWidth / expectedWidth;
            }

            if (Mathf.Abs(expectedHeight - xmlHeight) > Mathf.Epsilon)
            {
                scaleY *= xmlHeight / expectedHeight;
            }

            bool flipX = scaleX < 0;
            bool flipY = scaleY < 0;

            scaleX = Mathf.Abs(scaleX);
            scaleY = Mathf.Abs(scaleY);

            if (float.IsNaN(scaleX) || float.IsNaN(scaleY))
            {
                Debug.LogError("Final calculated scale is invalid.");
            }

            transform.localScale = new Vector3(scaleX, scaleY, 1);
            spriteRenderer.flipX = flipX;
            spriteRenderer.flipY = flipY;

            transform.position += new Vector3(Tx, Ty, 0);
        }
        catch (FormatException ex)
        {
            Debug.LogError($"FormatException in ConvertFromMarmaladeMatrix: {ex.Message}");
        }
    }
}

