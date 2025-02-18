using System.Linq;
using System.Xml;
using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShowMap : MonoBehaviour
{
    public string level_name;
    public bool AutoTag;
    int layer;
    GameObject actualObject;
    GameObject lastContent;
    GameObject lv;
    GameObject part;

    [MenuItem("Vectorier/Render Object Sequence")]
    public static void RenderMap()
    {
        var showMap = GameObject.FindObjectOfType<ShowMap>();

        // xml reference
        List<string> buildings = new List<string>();
        List<string> objects = new List<string>();

        Debug.Log("Rendering level " + showMap.level_name);
        showMap.lv = new GameObject(showMap.level_name);
        showMap.lv.name = showMap.level_name;

        XmlDocument level = new XmlDocument();
        level.Load(Application.dataPath + "/XML/" + showMap.level_name);

        foreach (XmlNode node in level.DocumentElement.SelectSingleNode("/Root/Sets"))
        {
            buildings.Add(node.Attributes.GetNamedItem("FileName").Value);
            XmlDocument building = new XmlDocument();
            building.Load(Application.dataPath + "/XML/" + node.Attributes.GetNamedItem("FileName").Value);

            foreach (XmlNode b_node in building.DocumentElement.SelectSingleNode("/Root/Sets"))
            {
                objects.Add(b_node.Attributes.GetNamedItem("FileName").Value);
            }
        }

        foreach (XmlNode node in level.DocumentElement.SelectSingleNode("/Root/Track"))
        {
            if (node.Name == "Object")
            {
                if (node.HasChildNodes)
                {
                    if (node.FirstChild.FirstChild.Attributes["Name"] != null)
                    {
                        foreach (XmlNode content in node.FirstChild)
                        {
                            if (content.Name == "Object")
                            {
                                showMap.layer += 1;
                                bool foundInBuildings = false;
                                foreach (string building_name in buildings)
                                {
                                    XmlDocument building = new XmlDocument();
                                    building.Load(Application.dataPath + "/XML/" + building_name);
                                    foreach (XmlNode b_node in building.DocumentElement.SelectSingleNode("/Root/Objects"))
                                    {
                                        //Check if the object has the correct name
                                        if (b_node.Name == "Object")
                                        {
                                            if (b_node.Attributes["Name"] != null)
                                            {
                                                if (b_node.Attributes.GetNamedItem("Name").Value == content.Attributes.GetNamedItem("Name").Value)
                                                {
                                                    foundInBuildings = true;
                                                    RenderSequence(content.Attributes.GetNamedItem("Name").Value, building_name, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (foundInBuildings == false)
                                {
                                    showMap.layer += 1;
                                    ConvertXmlToObject(content.Attributes.GetNamedItem("Name").Value, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (XmlNode empty_object in node.FirstChild)
                        {
                            foreach (XmlNode empty_object_content in empty_object.FirstChild)
                            {
                                if (empty_object_content.Name == "Object" && empty_object_content.Attributes["Name"] != null)
                                {
                                    showMap.layer += 1;
                                    bool foundInBuildings = false;
                                    foreach (string building_name in buildings)
                                    {
                                        XmlDocument building = new XmlDocument();
                                        building.Load(Application.dataPath + "/XML/" + building_name);
                                        foreach (XmlNode b_node in building.DocumentElement.SelectSingleNode("/Root/Objects"))
                                        {
                                            //Check if the object has the correct name
                                            if (b_node.Name == "Object")
                                            {
                                                if (b_node.Attributes["Name"] != null)
                                                {
                                                    if (b_node.Attributes.GetNamedItem("Name").Value == empty_object_content.Attributes.GetNamedItem("Name").Value)
                                                    {
                                                        foundInBuildings = true;
                                                        RenderSequence(empty_object_content.Attributes.GetNamedItem("Name").Value, building_name, empty_object_content.Attributes.GetNamedItem("X").Value, empty_object_content.Attributes.GetNamedItem("Y").Value);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (foundInBuildings == false)
                                    {
                                        showMap.layer += 1;
                                        ConvertXmlToObject(empty_object_content.Attributes.GetNamedItem("Name").Value, empty_object_content.Attributes.GetNamedItem("X").Value, empty_object_content.Attributes.GetNamedItem("Y").Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    static void RenderSequence(string seq_name, string building_name, string x, string y)
    {
        var showMap = GameObject.FindObjectOfType<ShowMap>();

        XmlDocument building = new XmlDocument();
        building.Load(Application.dataPath + "/XML/" + building_name);
        foreach (XmlNode node in building.DocumentElement.SelectSingleNode("/Root/Objects"))
        {
            // Check if the object has the correct name
            if (node.Name == "Object")
            {
                if (node.Attributes.GetNamedItem("Name").Value == seq_name)
                {
                    showMap.part = new GameObject(seq_name);
                    showMap.part.name = seq_name;
                    showMap.part.transform.SetParent(showMap.lv.transform);
                    showMap.part.transform.localPosition = new Vector3(float.Parse(x) / 100, -float.Parse(y) / 100, 0);

                    // Search for each node in the object 
                    foreach (XmlNode content in node.FirstChild)
                    {
                        if (content.Name == "Object")
                        {
                            showMap.layer += 1;
                            if (content.Attributes["Name"] != null)
                            {
                                ConvertXmlToObject(content.Attributes.GetNamedItem("Name").Value, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                            }
                            else
                            {
                                foreach (XmlNode child in content.FirstChild)
                                {
                                    if (child.Name == "Image")
                                        showMap.InstantiateObject(child, "Unnamed-object", content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                    else if (child.Name == "Trigger")
                                        showMap.InstantiateObject(child, "Unnamed-object", content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                    else if (child.Name == "Area")
                                        showMap.InstantiateObject(child, "Unnamed-object", content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                    else if (child.Name == "Object")
                                        showMap.InstantiateObject(child, "Unnamed-object", content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                    else if (child.Name == "Platform")
                                        showMap.InstantiateObject(child, "Unnamed-object", content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                                }
                            }
                        }

                        else if (content.Name == "Image")
                        {
                            showMap.layer += 1;
                            showMap.actualObject = new GameObject(content.Attributes.GetNamedItem("ClassName").Value);
                            showMap.actualObject.name = content.Attributes.GetNamedItem("ClassName").Value;
                            showMap.actualObject.transform.SetParent(showMap.part.transform);
                            showMap.actualObject.transform.localPosition = new Vector3(0, 0, 0);
                            showMap.InstantiateObject(content, content.Attributes.GetNamedItem("ClassName").Value, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                            showMap.actualObject = null;
                        }

                        else if (content.Name == "Trigger" || content.Name == "Area")
                        {
                            showMap.layer += 1;
                            showMap.actualObject = new GameObject(content.Attributes.GetNamedItem("Name").Value);
                            showMap.actualObject.name = content.Attributes.GetNamedItem("Name").Value;
                            showMap.actualObject.transform.SetParent(showMap.part.transform);
                            showMap.actualObject.transform.localPosition = new Vector3(0, 0, 0);
                            showMap.InstantiateObject(content, content.Attributes.GetNamedItem("Name").Value, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                            showMap.actualObject = null;
                        }
                        else if (content.Name == "Platform")
                        {
                            showMap.layer += 1;
                            showMap.actualObject = new GameObject(content.Attributes.GetNamedItem("X").Value); //not sure why did i do this but it works.
                            showMap.actualObject.name = "Platform";
                            showMap.actualObject.transform.SetParent(showMap.part.transform);
                            showMap.actualObject.transform.localPosition = new Vector3(0, 0, 0);
                            showMap.InstantiateObject(content, content.Name, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
                            showMap.actualObject = null;
                        }
                    }
                }
            }
        }
        showMap.part = null;
    }

    static void ConvertXmlToObject(string object_name, string x, string y)
    {
        var showMap = GameObject.FindObjectOfType<ShowMap>();

        // Load all object XMLs
        XmlDocument obj = new XmlDocument();
        obj.Load(Application.dataPath + "/XML/objects.xml");
        bool objectFound = false;
        int doc_num = 0;

        // Search for the selected object in the object XMLs
        while (objectFound == false & doc_num < 3)
        {
            if (doc_num == 0)
                obj.Load(Application.dataPath + "/XML/objects.xml");
            else if (doc_num == 1)
                obj.Load(Application.dataPath + "/XML/objects_downtown.xml");
            else if (doc_num == 2)
                obj.Load(Application.dataPath + "/XML/objects_construction.xml");
            foreach (XmlNode node in obj.DocumentElement.SelectSingleNode("/Root/Objects"))
            {
                // Check if the object has the correct name
                if (node.Name == "Object")
                {
                    if (node.Attributes.GetNamedItem("Name").Value == object_name)
                    {
                        objectFound = true;

                        // Search for each node in the object 
                        foreach (XmlNode content in node.FirstChild)
                        {
                            if (content.Name == "Image")
                                showMap.InstantiateObject(content, object_name, x, y);
                            else if (content.Name == "Trigger")
                                showMap.InstantiateObject(content, object_name, x, y);
                            else if (content.Name == "Area")
                                showMap.InstantiateObject(content, object_name, x, y);
                            else if (content.Name == "Object")
                                showMap.InstantiateObject(content, object_name, x, y);
                            else if (content.Name == "Platform")
                                showMap.InstantiateObject(content, object_name, x, y);
                        }
                    }
                }
            }

            doc_num += 1;
        }

        showMap.actualObject = null;
    }

    void InstantiateObject(XmlNode content, string object_name, string x, string y)
    {
        // Place the image using every information the xml provide (X, Y, Width, Height, ClassName)
        if (content.Name == "Object" && !content.Attributes.GetNamedItem("Name").Value.Contains("Trigger"))
        {
            ConvertXmlToObject(content.Attributes.GetNamedItem("Name").Value, content.Attributes.GetNamedItem("X").Value, content.Attributes.GetNamedItem("Y").Value);
        }
        else
        {
            if (actualObject == null)
            {
                // Create a new GameObject with the selected object
                actualObject = new GameObject(object_name);

                if (part != null)
                    actualObject.transform.SetParent(part.transform);
                else
                    actualObject.transform.SetParent(lv.transform);

                actualObject.transform.localPosition = new Vector3(float.Parse(x) / 100, -float.Parse(y) / 100, 0);
                actualObject.name = object_name; // Name it correctly
            }

            // vv  Image  vv
            if (content.Name == "Image")
            {
                lastContent = new GameObject(content.Attributes.GetNamedItem("ClassName").Value); // Usage of ClassName value (To name the new object)
                lastContent.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Textures/" + content.Attributes.GetNamedItem("ClassName").Value); // REUsage of ClassName value (To place the texture)

                if (AutoTag)
                {
                    lastContent.tag = "Image";
                }

                // Check if the image is rotated (by checking if there is a Matrix node)
                if (content.HasChildNodes)
                {
                    // Get into the Matrix node
                    foreach (XmlNode matrixNode in content.LastChild.FirstChild)
                    {
                        if (matrixNode.Name == "Matrix" && matrixNode.Attributes.GetNamedItem("A").Value != content.Attributes.GetNamedItem("Width").Value)
                        {
                            if (matrixNode.Attributes.GetNamedItem("A").Value != "")
                                lastContent.transform.rotation = Quaternion.Euler(0, float.Parse(content.Attributes.GetNamedItem("Width").Value, CultureInfo.InvariantCulture) / float.Parse(matrixNode.Attributes.GetNamedItem("A").Value, CultureInfo.InvariantCulture) * 180f, 0);
                        }
                    }
                }
            }

            // vv  Trigger  vv
            else if (content.Name == "Trigger")
            {
                if (content.Attributes["Name"] != null)
                    lastContent = new GameObject(content.Attributes.GetNamedItem("Name").Value);
                else
                    lastContent = new GameObject("Trigger-" + object_name);

                lastContent.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Textures/trigger");

                // Add TriggerSettings component and set the Content string
                var triggerSettings = lastContent.AddComponent<TriggerSettings>();
                XmlNode contentNode = content.SelectSingleNode("Content");
                if (contentNode != null)
                {
                    triggerSettings.Content = contentNode.InnerXml;
                }

                if (AutoTag)
                {
                    lastContent.tag = "Trigger";
                }
            }

            // vv  Trick  vv
            else if (content.Name == "Area" || content.Name == "Object" && content.OuterXml.Contains("Trigger"))
            {
                lastContent = new GameObject(content.Attributes.GetNamedItem("Name").Value); // Usage of Name value (To name the new object)

                if (content.Attributes.GetNamedItem("Name").Value.Contains("Trigger") && content.Attributes["ItemName"] != null)
                    lastContent.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Textures/tricks/TRACK_" + content.Attributes.GetNamedItem("ItemName").Value); // REUsage of ClassName value (To place the texture)
                else
                    lastContent.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Textures/trigger"); // REUsage of ClassName value (To place the texture)
            }

            // vv  Platform  vv
            else if (content.Name == "Platform")
            {
                lastContent = new GameObject("Platform-" + object_name);
                lastContent.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Textures/collision");

                if (AutoTag)
                {
                    lastContent.tag = "Platform";
                }
            }

            // vv  Universal action  vv
            lastContent.GetComponent<SpriteRenderer>().transform.SetParent(actualObject.transform); // Place the new image into the selected object

            if (content.Name != "Object")
                lastContent.transform.localPosition = new Vector3(float.Parse(content.Attributes.GetNamedItem("X").Value) / 100 + Math.Abs(lastContent.transform.rotation.y) * float.Parse(content.Attributes.GetNamedItem("Width").Value) / 100, -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100, 0);
            else
                lastContent.transform.localPosition = new Vector3(float.Parse(content.Attributes.GetNamedItem("X").Value) / 100, -float.Parse(content.Attributes.GetNamedItem("Y").Value) / 100, 0);

            if (lastContent.GetComponent<SpriteRenderer>().sprite.name.Contains("TRICK"))
                lastContent.transform.localScale = new Vector3(1, 1, 0);
            else if (content.Attributes["Width"] != null)
                lastContent.transform.localScale = new Vector3(float.Parse(content.Attributes.GetNamedItem("Width").Value) / lastContent.GetComponent<SpriteRenderer>().sprite.texture.width, float.Parse(content.Attributes.GetNamedItem("Height").Value) / lastContent.GetComponent<SpriteRenderer>().sprite.texture.height, 0); //Usage of Width and Height value

            // actualObject.tag = "Object"; //VERY IMPORTANT : Every GameObject with the tag "Object" will be counted in the final build, else ignored.
            lastContent.GetComponent<SpriteRenderer>().sortingOrder = layer;
        }
    }
}