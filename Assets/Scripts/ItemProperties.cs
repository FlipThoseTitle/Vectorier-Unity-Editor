using UnityEngine;

[AddComponentMenu("Vectorier/Item Properties")]
public class ItemProperties : MonoBehaviour
{
    [Tooltip("Type 0 is bonus")]
    public float Type;

    public float Score;

    [Tooltip("Default is 80")]
    public float Radius;

    [Tooltip("Unused on type 0")]
    public float GroupId;




}
