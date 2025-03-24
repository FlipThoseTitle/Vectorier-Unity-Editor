using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Vectorier/Dynamic Trigger")]
public class DynamicTrigger : MonoBehaviour
{
    [Tooltip("Which transformation to trigger")]
	public string TriggerTransformName = "Transform_name";

    [Tooltip("Which AI is allowed to trigger")]
	public int AIAllowed = -1;

	[Tooltip(@"Which node activates the trigger. Default is ""COM"".")]
	public string modelNode = "COM";

	[Tooltip(@"Determines whether trigger is activated if model node enters or leaves the trigger bounds.")]
	public EventTypes EventType = EventTypes.Enter;

	// -=-=-=- //
	[Header("Sound settings")]
    public bool PlaySound = false;

	[Tooltip(@"File inside ""sound.dz"" archive to be played.")]
	public string Sound = "";

	/*
	[Tooltip(@"Delay in seconds after which ""Sound"" will be played.")]
	public float Latency = 0f;
	*/

	// -=-=-=- //
	[Header("Miscellaneous")]

	[Tooltip("Allows trigger to be used multiple times.")]
	public bool Reusable = false;

    [Tooltip("Use multiple transformations")]
	public bool MultipleTransformation = false;

    [Tooltip("List of transformation names to use if using multiple transformation")]
    public List<string> TransformationNames = new List<string>();

    [Tooltip("Order of transformations, Random will choose a random transformation from the list, Sync will play at the same time")]
    public OrderType Order = OrderType.Sync;

    [Tooltip("Set value, This indicates how much transformations to choose from the list if order is random. Set this to 0 for Sync order")]
    public int Set = 1;

	// -=-=-=- //

	public enum EventTypes
	{
		Enter,
		Exit
	}

    public enum OrderType
    {
        Random,
        Sync
    }
}
