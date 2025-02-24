using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

public class ShowMap : MonoBehaviour
{
    [Tooltip(@"Level basename, accordingly to the ""level_xml"" directory")]
    public string levelName;

    [Tooltip(@"Determines whether tracks should be loaded from backup directory")]
    public bool fromBackupDirectory = true;

    [Tooltip(@"Determines whether the new GameObject with BuildMap script containing inherited custom properties should be created")]
    public bool addBuildMap = true;

    [Tooltip(@"Log objects when rendering")]
    public bool debugObjectFound;

    string levelNameDisplay;

    GameObject lastContent;
    GameObject actualObject;
    GameObject dummyObject;
    GameObject lv;
    int orderInLayer = 0;
    List<string> buildings = new List<string>();
    List<string> objects = new List<string>();
    List<XmlDocument> xmls = new List<XmlDocument>();

    static string PrettyXml(string xml, bool newLineOnAttr)
    {
        var stringBuilder = new StringBuilder();
        XDocument document = null;

        try
        {
            // Wrap XML in a <root> element to ensure it's well-formed
            xml = "<root>" + xml + "</root>";

            // Parse the wrapped XML into an XDocument
            document = XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            // If parsing fails, handle the exception
            Console.WriteLine("Error parsing XML: " + ex.Message);
            return string.Empty;
        }

        // Format the XML with indentation and tab characters
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "\t",  // Set indentation character to tab
            NewLineOnAttributes = newLineOnAttr,
            ConformanceLevel = ConformanceLevel.Fragment  // Set ConformanceLevel to Fragment
        };

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            // Iterate over each element in the root (which contains the actual XML nodes)
            foreach (var element in document.Root.Elements())
            {
                element.WriteTo(xmlWriter); // Directly write the element to the XmlWriter
            }
        }

        return stringBuilder.ToString();
    }

    [MenuItem("Vectorier/Render Map")]
    public static void RenderMap()
    {
        var showMap = GameObject.FindObjectOfType<ShowMap>();

        showMap.levelName = showMap.levelName.TrimEnd(".xml".ToCharArray()) + ".xml";
        showMap.levelNameDisplay = Path.GetFileNameWithoutExtension(showMap.levelName);
        showMap.lv = new GameObject(showMap.levelNameDisplay);

        string xmlDir = Path.Combine(Application.dataPath, "XML", "dzip");
        xmlDir = Path.Combine(xmlDir, "level_xml" + (showMap.fromBackupDirectory ? "_backup" : ""));

        showMap.xmls.Clear();
        showMap.objects.Clear();
        showMap.buildings.Clear();

        // xml reference

        Debug.Log($@"Rendering level: ""{showMap.levelNameDisplay}""");
        XmlDocument level = new XmlDocument();
        level.Load(Path.Combine(xmlDir, showMap.levelName));

        if (showMap.addBuildMap)
        {
            BuildMap build = showMap.lv.AddComponent<BuildMap>();

            build.mapToOverride = showMap.levelNameDisplay;
            build.correctFactorPosition = false;
            build.useCustomProperties = true;
            //build.CustomModelProperties = level.DocumentElement.SelectSingleNode("/Root/Models").InnerXml;
            build.CustomModelProperties = PrettyXml(level.DocumentElement.SelectSingleNode("/Root/Models").InnerXml, true);
        }

        foreach (XmlNode node in level.DocumentElement.SelectSingleNode("/Root/Sets"))
        {
            if (node.Name == "City")
            {
                showMap.buildings.Add(node.Attributes.GetNamedItem("FileName").Value);
                XmlDocument building = new XmlDocument();
                building.Load(Path.Combine(xmlDir, node.Attributes.GetNamedItem("FileName").Value));

                foreach (XmlNode b_node in building.DocumentElement.SelectSingleNode("/Root/Sets"))
                {
                    showMap.objects.Add(b_node.Attributes.GetNamedItem("FileName").Value);
                }
            }
        }
        foreach (string document in showMap.buildings)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(Path.Combine(xmlDir, document));

            showMap.xmls.Add(xmlDoc);
        }
        foreach (string document in showMap.objects)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(Path.Combine(xmlDir, document));

            showMap.xmls.Add(xmlDoc);
        }

        foreach (XmlNode lvnode in level.DocumentElement.SelectSingleNode("/Root/Track"))
        {
            if (lvnode.Name == "Object")
            {
                if (lvnode.Attributes["Factor"] != null)
                {
                    GameObject factor = new GameObject("Object Factor " + lvnode.Attributes.GetNamedItem("Factor").Value);
                    factor.transform.SetParent(showMap.lv.transform, false);
                    if (lvnode["Content"] != null)
                    {
                        showMap.orderInLayer = 0;
                        foreach (XmlNode objectNode in lvnode.FirstChild)
                        {
                            showMap.InstantiateObject(objectNode, true, factor, true, false, float.Parse(lvnode.Attributes.GetNamedItem("Factor").Value, CultureInfo.InvariantCulture.NumberFormat));
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        showMap.xmls.Clear();
        showMap.objects.Clear();
        showMap.buildings.Clear();
    }

    void InstantiateObject(XmlNode content, bool usesParent, GameObject parent, bool useTag, bool fromReferenced, float factor)
    {

        if (debugObjectFound)
        {
            Debug.Log($"Found {content.Name}: {content.Attributes.GetNamedItem("ClassName")?.Value ?? content.Attributes.GetNamedItem("Name").Value}");
        }

        // Replace variable placeholders with actual values

        // Object
        if (content.Name == "Object")
        {

            XmlAttribute referencedObjectName = content.Attributes["Name"];

            // Check if the object has child nodes (nested Content)
            if (referencedObjectName != null)
            {
                if (content.SelectSingleNode("Content") != null)
                {
                    XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value);

                    Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

                    GameObject nestedObject = new GameObject(referencedObjectName.Value);
                    nestedObject.transform.position = position;
                    if (usesParent)
                    {
                        nestedObject.transform.SetParent(parent.transform, false);
                    }
                    else
                    {
                        nestedObject.transform.parent = actualObject.transform;

                    }
                    if (useTag)
                    {

                        nestedObject.tag = "Object";

                    }
                    if (factor < 1)
                    {
                        nestedObject.tag = "Untagged";
                    }
                    XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

                    if (selection != null)
                    {
                        if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                        {
                            nestedObject.tag = "Unused";
                        }
                    }

                    if (referencedObjectNode != null)
                    {
                        // Recursively instantiate nested objects
                        foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
                        {
                            InstantiateObject(nestedContent, true, nestedObject, false, true, factor);
                        }
                    }

                    // Recursively instantiate nested objects
                    foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
                    {
                        if (fromReferenced)
                        {
                            InstantiateObject(nestedContent, true, nestedObject, false, true, factor);
                        }
                        else
                        {
                            InstantiateObject(nestedContent, true, nestedObject, true, false, factor);
                        }
                    }

                }
                else
                {
                    // This is a simple one-line object, find its definition in the XML and instantiate
                    XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value);
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
                        if (usesParent)
                        {
                            nestedObject.transform.SetParent(parent.transform, false);
                        }
                        else
                        {
                            nestedObject.transform.parent = actualObject.transform;

                        }
                        if (useTag)
                        {

                            nestedObject.tag = "Object";

                        }
                        if (factor < 1)
                        {
                            nestedObject.tag = "Untagged";
                        }
                        XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

                        if (selection != null)
                        {
                            if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                            {
                                nestedObject.tag = "Unused";
                                nestedObject.name = nestedObject.name + " (HunterMode)";
                            }
                        }

                        // Recursively instantiate nested objects
                        foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*"))
                        {
                            InstantiateObject(nestedContent, true, nestedObject, false, true, factor);
                        }

                        // Reset actualObject position after instantiating the nested objects
                    }
                    else
                    {
                        Debug.LogError($"Referenced object '{referencedObjectName}' not found in the XML file.");
                    }
                }
                return;
            }
            else if (referencedObjectName == null)
            {
                Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

                GameObject nestedObject = new GameObject(string.Empty);
                nestedObject.transform.position = position;
                if (usesParent)
                {
                    nestedObject.transform.SetParent(parent.transform, false);
                }
                else
                {
                    nestedObject.transform.parent = actualObject.transform;

                }
                if (useTag)
                {
                    nestedObject.tag = "Object";

                }
                if (factor < 1)
                {
                    nestedObject.tag = "Untagged";
                }
                XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

                if (selection != null)
                {
                    if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                    {
                        nestedObject.tag = "Unused";
                        nestedObject.name = nestedObject.name + " (HunterMode)";
                    }
                }


                // Recursively instantiate nested objects
                foreach (XmlNode nestedContent in content.SelectNodes("Content/*"))
                {
                    if (fromReferenced)
                    {
                        InstantiateObject(nestedContent, true, nestedObject, false, true, factor);
                    }
                    else
                    {
                        InstantiateObject(nestedContent, true, nestedObject, true, false, factor);
                    }
                }
            }
        }
        if (content.Name == "Camera")
        {
            Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

            GameObject nestedObject = new GameObject("Camera");

            Camera cameraComponent = nestedObject.AddComponent<Camera>();
            cameraComponent.orthographic = false; // Set projection to Perspective
            cameraComponent.fieldOfView = 154.188f; // Set field of view

            Rect viewportRect = cameraComponent.rect;
            viewportRect.x = 0.875f; // Set rect X
            viewportRect.y = 1.5f; // Set rect Y
            cameraComponent.rect = viewportRect;

            nestedObject.transform.position = position;
            if (usesParent)
            {
                nestedObject.transform.SetParent(parent.transform, false);
            }
            else
            {
                nestedObject.transform.parent = actualObject.transform;

            }
            if (useTag)
            {
                nestedObject.tag = "Object";

            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    nestedObject.tag = "Unused";
                    nestedObject.name = nestedObject.name + " (HunterMode)";
                }
            }
        }

        if (content.Name == "Spawn")
        {
            Vector3 position = new Vector3(
                        float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
                        -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
                        0
                    );

            GameObject nestedObject = new GameObject(content.Attributes.GetNamedItem("Name").Value);
            nestedObject.transform.position = position;

            Spawn spawn = nestedObject.AddComponent<Spawn>();

            spawn.SpawnName = content.Attributes["Name"].Value;

            spawn.SpawnAnimation = content.Attributes["Animation"].Value;

            if (usesParent)
            {
                nestedObject.transform.SetParent(parent.transform, false);
            }
            else
            {
                nestedObject.transform.parent = actualObject.transform;

            }
            if (useTag)
            {
                {
                    nestedObject.tag = "Spawn";
                }

            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    nestedObject.tag = "Unused";
                    nestedObject.name = nestedObject.name + " (HunterMode)";
                }
            }
        }


        // Images
        if (content.Name == "Image")
        {

            HandleImageNode(content);

            if (useTag)
            {

                lastContent.tag = "Image";

            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            if (factor == 0.5)
            {
                lastContent.tag = "Backdrop";
                SpriteRenderer sprite = lastContent.GetComponent<SpriteRenderer>();

                sprite.sortingOrder = -1;
            }
            if (factor < 0.5)
            {
                SpriteRenderer sprite = lastContent.GetComponent<SpriteRenderer>();

                sprite.sortingOrder = -2;
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }



        }
        // Platform
        else if (content.Name == "Platform")
        {

            HandlePlatformNode(content);

            if (useTag)
            {

                lastContent.tag = "Platform";
            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }



        }

        else if (content.Name == "Item")
        {

            HandleItemNode(content);

            if (useTag)
            {

                lastContent.tag = "Item";
            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }

        }

        else if (content.Name == "Trapezoid")
        {
            // Trapezoid
            HandleTrapezoidNode(content);

            if (useTag)
            {


                lastContent.tag = "Trapezoid";
            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }

        }

        // Trigger
        else if (content.Name == "Trigger")
        {

            HandleTriggerNode(content);

            if (useTag)
            {


                lastContent.tag = "Trigger";
            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }

        }

        // Area
        else if (content.Name == "Area")
        {
            HandleAreaNode(content);

            if (useTag)
            {


                lastContent.tag = "Area";

            }
            else if (fromReferenced)
            {
                lastContent.tag = "Unused";
            }
            XmlNode selection = content.SelectSingleNode("Properties/Static/Selection");

            if (selection != null)
            {
                if (selection.Attributes.GetNamedItem("Variant").Value == "HunterMode")
                {
                    lastContent.tag = "Unused";
                    lastContent.name = lastContent.name + " (HunterMode)";
                }
            }


        }
        else
        {
            return;
        }


        // Remove "(Clone)" from object's name
        lastContent.name = lastContent.name.Replace("(Clone)", string.Empty);

        if (usesParent)
        {
            lastContent.transform.SetParent(parent.transform, false);
        }
        else
        {
            lastContent.transform.parent = actualObject.transform;
        }
        DestroyImmediate(dummyObject);
    }

    XmlNode FindObjectNodeByName(string objectName)
    {

        foreach (XmlDocument document in xmls)
        {

            foreach (XmlNode node in document.SelectSingleNode("/Root/Objects"))
            {
                if (node.Name == "Object" && node.Attributes.GetNamedItem("Name").Value == objectName)
                {
                    return node;
                }
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


    // Images
    void HandleImageNode(XmlNode content)
    {

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
                if (debugObjectFound)
                {
                    Debug.Log($"Matrix node found. Attempting to apply transformation.");
                }
                ConvertFromMarmaladeMatrix(matrixNode, lastContent.transform, spriteRenderer, width, height, lastContent, className);
            }
        }
    }


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
        spriteRenderer.sortingOrder = 1000;


        float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
        float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

        float originalWidth = spriteRenderer.sprite.texture.width;
        float originalHeight = spriteRenderer.sprite.texture.height;

        Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
        lastContent.transform.localScale = scale;
    }

    void HandleItemNode(XmlNode content)
    {
        Vector3 position = new Vector3(
            float.Parse(content.Attributes.GetNamedItem("X").Value) / 100,
            -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100,
            0
        );

        lastContent = Instantiate(
            dummyObject = new GameObject("Item"),
            position,
            Quaternion.identity
        );


        ItemProperties itemProperties = lastContent.AddComponent<ItemProperties>();

        itemProperties.Type = float.Parse(content.Attributes["Type"].Value);
        itemProperties.Radius = float.Parse(content.Attributes["Radius"].Value);
        itemProperties.Score = float.Parse(content.Attributes["Score"].Value);
        if (content.Attributes["GroupId"] != null)
        {
            itemProperties.GroupId = float.Parse(content.Attributes["GroupId"].Value);
        }
        if (content.Attributes["Type"].Value == "0")
        {
            SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Textures/pill");
            spriteRenderer.sortingOrder = 999;
        }
        if (content.Attributes["Type"].Value == "1")
        {
            SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Textures/credit");
            spriteRenderer.sortingOrder = 999;
        }
    }

    // Trigger
    void HandleTriggerNode(XmlNode content)
    {
        float pos_x = content.Attributes.GetNamedItem("X") != null
            ? float.Parse(content.Attributes.GetNamedItem("X").Value)
            : 0;

        float pos_y = content.Attributes.GetNamedItem("Y") != null
            ? float.Parse(content.Attributes.GetNamedItem("Y").Value)
            : 0;

        Vector3 position = new Vector3(pos_x / 100, -pos_y / 100, 0);

        lastContent = Instantiate(
            dummyObject = new GameObject(),
            position,
            Quaternion.identity
        );
        XmlAttribute name = content.Attributes["Name"];
        if (name != null)
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
        spriteRenderer.sortingOrder = 1002;

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
            triggerSettings.Content = PrettyXml(contentNode.InnerXml, false);
        }
    }




    // Area
    void HandleAreaNode(XmlNode content)
    {
        string type = content.Attributes["Type"].Value;
        string name = content.Attributes.GetNamedItem("Name").Value;
        float x = float.Parse(content.Attributes.GetNamedItem("X").Value) / 100;
        float y = -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100;
        Vector3 position = new Vector3(x, y, 0);

        if (content is XmlElement node)
        {
            string[] attrs = { "Type", "Description" };
            foreach (string attr in attrs)
            {
                if (content.Attributes[attr] != null)
                {
                    node.SetAttribute(attr, content.Attributes[attr].Value);
                }
            }
        }

        lastContent = Instantiate(
            dummyObject = new GameObject(name),
            position,
            Quaternion.identity
        );

        SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();

        if (type == "Trick")
        {
            string itemName = content.Attributes["ItemName"].Value;
            spriteRenderer.sprite = Resources.Load<Sprite>($"Textures/tricks/TRACK_{itemName}");
            spriteRenderer.sortingOrder = 1003;
        }
        else
        {
            spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trick");
            spriteRenderer.sortingOrder = 1001;

            float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
            float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);
            float originalWidth = spriteRenderer.sprite.texture.width;
            float originalHeight = spriteRenderer.sprite.texture.height;

            Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
            lastContent.transform.localScale = scale;
        }
    }


    // Calculation
    void ConvertFromMarmaladeMatrix(XmlNode matrixNode, UnityEngine.Transform transform, SpriteRenderer spriteRenderer, float xmlWidth, float xmlHeight, GameObject image, string className)
    {
        _DefautPosition = transform.localPosition;

        _Transformation[0, 0] = float.Parse(matrixNode.Attributes["A"].Value, CultureInfo.InvariantCulture.NumberFormat);
        _Transformation[0, 1] = -float.Parse(matrixNode.Attributes["B"].Value, CultureInfo.InvariantCulture.NumberFormat);
        _Transformation[1, 0] = -float.Parse(matrixNode.Attributes["C"].Value, CultureInfo.InvariantCulture.NumberFormat);
        _Transformation[1, 1] = float.Parse(matrixNode.Attributes["D"].Value, CultureInfo.InvariantCulture.NumberFormat);
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
