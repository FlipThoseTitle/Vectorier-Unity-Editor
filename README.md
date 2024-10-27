# Vectorier Unity Editor
Vectorier-Unity-Editor is an level editor for the game Vector using the Unity Engine.

A Discord server is open for the project : https://discord.com/invite/pVRuFBVwC2

![image](https://github.com/user-attachments/assets/d5edd083-b504-49aa-a39b-9839373dd1f7)


# Features
 * Object importer
 * Level importer
 * Level edition

# Installation
 * Download Unity [2020.3.35f1](https://download.unity3d.com/download_unity/18e4db7a9996/Windows64EditorInstaller/UnitySetup64-2020.3.35f1.exe) at the [official Unity Website](https://unity3d.com/get-unity/download/archive)
 * Download the [Vectorier Unity Editor project](https://github.com/FlipThoseTitle/Vectorier-Unity-Editor/archive/refs/heads/main.zip)
 * Extract the zip file
 * Open the project in Unity Hub
 
# Usage
 * First, open any unity scene
 * In the hierarchy tab, click on the "ScriptManager" gameobject
 - **If you want to import objects in your level :**
    * In the inspector, enter in the "Object To Convert" textbox you wish to convert (The list of object is located in *Assets* > *XML* > *objects.xml*)
    * Then go to *Vectorier* > *Convert from object.xml*
    * **NOTE :** Most of the Vector's necessary assets are already pre-configured, located in *Resources Folder*.
    
 - **If you want to import a map :**
    * (If you don't see a script called "Show Map" in the inspector, add the script located at *Assets* > *Scripts* > *ShowMap* on the "ScriptManager" gameobject)
    * In the inspector, simply enter the name of the level you wish to import
       *EX.* CONSTRUCTION_BONUS_01
    * Then go to *Vectorier* > *Render object sequence*
    
 - **If you want to build a map :**
    * In the project settings, head to the Vectorier tab and enter your file path in the "Game Directory" box to the Vector's directory (where the executable game is located)
    * Addtionally for non-steam version, you can also make a game shortcut, and then enter your file path in the "Game Executable Path" to the shortcut. (eg. C:\Program Files (x86)\Steam\steamapps\common\Vector\Vector.exe - Shortcut)
    * Modify the preference to your likings.
    - Head to Vectorier menutab, there will be a different method of Building your Map, choose one from your preference.
      * BuildMap - Slow, Small File Size
      * BuildMap (Fast) - Fast, Big File Size
      * BuildMap XML Only

# Packages
These are the quality of life tool included to make mapping more easier.
 * Life Easer 1.5 from Thelastcube
 * Cutscene Pack
 * Movement Visualizer and reversed version from kubinka0505
