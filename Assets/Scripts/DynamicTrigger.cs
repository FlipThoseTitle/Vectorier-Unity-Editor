using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Vectorier/Dynamic Trigger")]
public class DynamicTrigger : MonoBehaviour
{
    [Tooltip("Which transformation to trigger")] public string TriggerTransformName = "Transform_Name";
    [Tooltip("Which AI is allowed to trigger")] public float AIAllowed = -1f;

    public bool PlaySound = false;
    public string Sound = "";

    [Tooltip("Use multiple transformations")] public bool MultipleTransformation = false;

    [Tooltip("List of transformation names to use if using multiple transformation")]
    public List<string> TransformationNames = new List<string>();

    [Tooltip("Order of transformations, Random will choose a random transformation from the list, Sync will play at the same time")]
    public OrderType Order = OrderType.Sync;

    [Tooltip("Set value, This indicates how much transformations to choose from the list if order is random. Set this to 0 for Sync order")]
    public int Set = 1;

    public enum OrderType
    {
        Random,
        Sync
    }
}
