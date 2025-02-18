using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Vectorier/Spawn")]
public class Spawn : MonoBehaviour
{
    public string SpawnName = "PlayerSpawn";
    public string SpawnAnimation = "JumpOff|18";
    [Tooltip("Add the spawn inside the respawn object")] public bool RefersToRespawn = false;

}
