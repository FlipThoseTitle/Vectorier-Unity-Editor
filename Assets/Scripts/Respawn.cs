using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TriggerSettings;

[AddComponentMenu("Vectorier/Respawn")]
public class Respawn : MonoBehaviour
{

    [Tooltip("The respawn name has to be the same name as the spawn that has set refers to respawn to true.")]
    public string RespawnName = "Respawn";
    public float RespawnSecond = 1.5f;
    public string TriggerName = "TriggerRespawn";
    [Tooltip("Which model to be respawn.")]
    public string Spawnmodel = "Hunter";
    [Tooltip("Respawn on screen directly")]
    public bool RespawnOnScreen = true;

}
