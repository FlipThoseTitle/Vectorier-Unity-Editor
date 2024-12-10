using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Vectorier/Dynamic")]
public class Dynamic : MonoBehaviour
{
    [Tooltip("Transformation name of the dynamic object")] public string TransformationName = "Transform_name";
    [Tooltip("Whether or not to add the red visual line while setting up")] public bool AddVisualLines = true;
    [Tooltip("Whether or not to automatically reset after the preview is finished")] public bool ResetAfterPreviewFinish = true;

    [Serializable]
    public class UseCheck
    {
        public bool UseMovement1 = true;
        public bool UseMovement2 = false;
        public bool UseMovement3 = false;
        public bool UseMovement4 = false;
        public bool UseMovement5 = false;

        public bool UseMovement(int index)
        {
            switch (index)
            {
                case 1: return UseMovement1;
                case 2: return UseMovement2;
                case 3: return UseMovement3;
                case 4: return UseMovement4;
                case 5: return UseMovement5;
                default: return false; // Out of range
            }
        }
    }


    [Serializable]
    public class Movement
    {
        [Tooltip("Move Duration in Second")] public float MoveDuration = 1.5f;
        [Tooltip("Move Delay in Second")] public float Delay = 0f;
        [Tooltip("Easing Value on the X Axis (Divide by 2 for linear easing)")] public float SupportXAxis = 0.0f;
        [Tooltip("Easing Value on the Y Axis (Divide by 2 for linear easing)")] public float SupportYAxis = 0.0f;
        [Tooltip("How much to move on X Axis")] public float MoveXAxis = 0.0f;
        [Tooltip("How much to move on Y Axis")] public float MoveYAxis = 0.0f;
    }

    [SerializeField] public UseCheck MovementUsage;
    [SerializeField] public Movement MoveInterval1;
    [SerializeField] public Movement MoveInterval2;
    [SerializeField] public Movement MoveInterval3;
    [SerializeField] public Movement MoveInterval4;
    [SerializeField] public Movement MoveInterval5;

    private Vector2 lastSpecificTopLeft;

    private const float checkInterval = 0.5f;

    private Dictionary<string, EditorApplication.CallbackFunction> monitoringActions = new Dictionary<string, EditorApplication.CallbackFunction>();
    private Dictionary<string, bool> isLoopActive = new Dictionary<string, bool>();
    private Dictionary<string, float> nextCheckTimes = new Dictionary<string, float>();

    private bool isImageDynamic = false;

    public void DuplicateChildrenPreview(string newParentName)
    {
        // Ignore List
        string[] excludedNames = { "MovePreview1", "MovePreview2", "MovePreview3", "MovePreview4", "MovePreview5", "VisualPointParent", "VisualLineParent" };
        string triggerTag = "Trigger";

        // In case of Image
        if (transform.CompareTag("Image"))
        {
            if (transform.childCount == 0)
            {
                GameObject duplicateParent = Instantiate(gameObject, transform);
                duplicateParent.name = name;
                duplicateParent.tag = "Unused";
                duplicateParent.transform.position = transform.position;
                duplicateParent.transform.rotation = transform.rotation;

                SpriteRenderer renderer = duplicateParent.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = 120 / 255f; // Set alpha transparency to 120
                    renderer.color = color;
                }

                // Clear components except Transform and SpriteRenderer
                var components = duplicateParent.GetComponents<UnityEngine.Component>();
                foreach (var component in components)
                {
                    if (!(component is Transform || component is SpriteRenderer))
                    {
                        DestroyImmediate(component);
                    }
                }
                isImageDynamic = true;
            }
        }

        // Create new empty object
        GameObject newParent = new GameObject(newParentName);
        newParent.tag = "Unused";
        newParent.transform.SetParent(transform);
        newParent.transform.position = transform.position;


        // Run through all child objects
        foreach (UnityEngine.Transform child in transform)
        {
            // Skip ignored object.
            if (child.CompareTag(triggerTag) && child.GetComponent<DynamicTrigger>()) continue;
            if (Array.Exists(excludedNames, name => name == child.name)) continue;

            // Duplication
            GameObject duplicate = null;
            duplicate = Instantiate(child.gameObject, newParent.transform);

            if (duplicate != null)
            {
                duplicate.name = child.name + "_Preview";
                duplicate.tag = "Unused";

                // Adjust SpriteRenderer transparency
                SpriteRenderer renderer = duplicate.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = 120 / 255f; // Set alpha transparency to 120
                    renderer.color = color;
                }
            }
        }
    }

    private void UpdateMoveInterval(string movePreview, Vector2 positionDifference)
    {
        Movement targetInterval = null;

        // Map MovePreview to the their MoveInterval
        switch (movePreview)
        {
            case "MovePreview1":
                targetInterval = MoveInterval1;
                break;
            case "MovePreview2":
                targetInterval = MoveInterval2;
                break;
            case "MovePreview3":
                targetInterval = MoveInterval3;
                break;
            case "MovePreview4":
                targetInterval = MoveInterval4;
                break;
            case "MovePreview5":
                targetInterval = MoveInterval5;
                break;
            default:
                Debug.LogWarning($"No matching MoveInterval found for {movePreview}.");
                return;
        }

        if (targetInterval != null)
        {
            // Update the MoveInterval's X and Y values
            targetInterval.MoveXAxis = positionDifference.x;
            targetInterval.MoveYAxis = positionDifference.y;

        }
    }






    // Looping method to check for changes
    public void StartPositionMonitoring(string MovePreview)
    {
        if (isLoopActive.ContainsKey(MovePreview) && isLoopActive[MovePreview])
        {
            Debug.LogWarning($"Position monitoring is already running for {MovePreview}.");
            return;
        }

        // Run loop state and next check time
        isLoopActive[MovePreview] = true;
        nextCheckTimes[MovePreview] = 0f;

        // Define monitoring action
        EditorApplication.CallbackFunction monitoringAction = () =>
        {
            if (this == null || transform == null)
            {
                StopPositionMonitoring(MovePreview);
                Debug.LogError("Dynamic Parent removed! Cancelling position monitoring.");
                return;
            }

            if (EditorApplication.timeSinceStartup >= nextCheckTimes[MovePreview])
            {
                nextCheckTimes[MovePreview] = (float)EditorApplication.timeSinceStartup + checkInterval;
                CheckPositionChanges(MovePreview);
            }
        };

        // Store and add to update loop
        monitoringActions[MovePreview] = monitoringAction;
        EditorApplication.update += monitoringActions[MovePreview];
        Debug.Log($"Starting set-up for {MovePreview}...");
    }

    public void StopPositionMonitoring(string MovePreview)
    {
        if (!isLoopActive.ContainsKey(MovePreview) || !isLoopActive[MovePreview])
        {
            Debug.LogWarning($"Position monitoring is not active for {MovePreview}.");
            return;
        }

        // Remove the monitoring action
        EditorApplication.update -= monitoringActions[MovePreview];
        monitoringActions.Remove(MovePreview);
        isLoopActive[MovePreview] = false;

        Debug.Log($"Stopping set-up for {MovePreview}.");
    }

    private void CheckPositionChanges(string movePreview)
    {
        UnityEngine.Transform specificObject = transform.Find(movePreview);

        // Check if parent is null or removed
        if (this == null || transform == null)
        {
            StopPositionMonitoring(movePreview);
            Debug.LogError("DynamicParent is null or removed! Cancelling position monitoring.");
            return;
        }

        // Stop monitoring when MovePreview is removed and clear all previews.
        if (specificObject == null)
        {
            StopPositionMonitoring(movePreview);
            Debug.Log($"{movePreview} has been removed. Clearing all MovePreviews.");
            ClearMovePreview();
            return;
        }

        // Calculate top-left positions
        Vector2 topLeftSpecific = CalculateTopLeftPosition(false, movePreview);

        // reference top-left position
        Vector2 referenceTopLeft;

        if (movePreview == "MovePreview1")
        {
            // Use the dynamic parent's top-left position for MovePreview1
            referenceTopLeft = CalculateTopLeftPosition(true);
        }
        else
        {
            // Use the previous MovePreview's top-left position
            int previewNumber = int.Parse(movePreview.Replace("MovePreview", ""));
            string previousMovePreview = $"MovePreview{previewNumber - 1}";
            UnityEngine.Transform previousObject = transform.Find(previousMovePreview);

            if (previousObject != null)
            {
                referenceTopLeft = CalculateTopLeftPosition(false, previousMovePreview);
            }
            else
            {
                Debug.LogError($"Previous MovePreview ({previousMovePreview}) not found for {movePreview}.");
                return;
            }
        }

        // Check if the top-left position has changed
        if (topLeftSpecific != lastSpecificTopLeft)
        {
            // Update stored top-left position
            lastSpecificTopLeft = topLeftSpecific;

            // Difference result
            Vector2 positionDifference = topLeftSpecific - referenceTopLeft;

            // Update the MoveInterval
            UpdateMoveInterval(movePreview, positionDifference);
        }
    }








    // Calculation Method
    public Vector2 CalculateTopLeftPosition(bool calculateFromParent, string specificObjectName = "")
    {
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        // Ignore List
        string[] excludedNames = { "MovePreview1", "MovePreview2", "MovePreview3", "MovePreview4", "MovePreview5",
                                "VisualPointParent", "VisualPoint1", "VisualPoint2", "VisualPoint3", "VisualPoint4", "VisualPoint5" };
        string triggerTag = "Trigger";

        void AccumulateBounds(UnityEngine.Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag(triggerTag) && child.GetComponent<DynamicTrigger>()) continue;
                if (Array.Exists(excludedNames, name => name == child.name)) continue;

                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(renderer.bounds); // Combine bounds
                    }
                }

                // Recurse into child objects
                AccumulateBounds(child);
            }
        }

        if (calculateFromParent)
        {
            AccumulateBounds(transform);
        }
        else
        {
            // Calculate from specific object's child objects
            UnityEngine.Transform specificObject = transform.Find(specificObjectName);
            if (specificObject == null)
            {
                Debug.LogWarning($"Object with name '{specificObjectName}' not found under the Dynamic Parent.");
                return Vector2.zero;
            }

            AccumulateBounds(specificObject);
        }

        if (!hasBounds)
        {
            Debug.LogWarning("No bounds found for the given objects.");
            return Vector2.zero;
        }

        // Calculate the top-left position in world coordinates
        float topLeftX = RoundToThreeDecimals(combinedBounds.min.x); // Leftmost X in world space
        float topLeftY = RoundToThreeDecimals(combinedBounds.max.y); // Topmost Y in world space

        return new Vector2(topLeftX, topLeftY);
    }

    private float RoundToThreeDecimals(float value)
    {
        return Mathf.Round(value * 1000f) / 1000f;
    }





    // Red Visualization Point
    public void AddRedSprite(bool placeInDynamicParent, string objectName, string specificObjectName)
    {
        Sprite redSprite = Resources.Load<Sprite>("Textures/Red");
        if (redSprite == null)
        {
            Debug.LogError("Red.png not found in Resources/Textures.");
            return;
        }

        GameObject redObject = new GameObject(objectName);
        SpriteRenderer spriteRenderer = redObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = redSprite;
        redObject.tag = "Unused";
        spriteRenderer.sortingOrder = 999;
        redObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Determine parent and position based on boolean parameter
        if (placeInDynamicParent)
        {
            redObject.transform.SetParent(transform);
            Vector2 topLeftPosition = CalculateTopLeftPosition(true);
            redObject.transform.position = new Vector3(topLeftPosition.x, topLeftPosition.y, 0);
        }
        else
        {
            // Find the specific game object by name
            UnityEngine.Transform specificObject = transform.Find(specificObjectName);
            if (specificObject == null)
            {
                Debug.LogError($"Object with name '{specificObjectName}' not found.");
                DestroyImmediate(redObject);
                return;
            }
            redObject.transform.SetParent(specificObject);
            Vector2 topLeftPosition = CalculateTopLeftPosition(false, specificObjectName);
            redObject.transform.position = new Vector3(topLeftPosition.x, topLeftPosition.y, 0);
        }

    }

    private void ConnectVisualLine(string lineName, string startPointName, string endPointName, string parentName)
    {
        // Check if line exists
        UnityEngine.Transform parent = transform.Find(parentName);
        if (parent == null)
        {
            Debug.LogError($"Parent '{parentName}' not found.");
            return;
        }

        if (parent.Find(lineName) != null)
        {
            Debug.LogWarning($"{lineName} already exists under {parentName}.");
            return;
        }

        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent);
        lineObject.tag = "Unused";

        //config
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = Color.red };
        lineRenderer.sortingOrder = 999;

        // Store references to the start and end points
        UnityEngine.Transform startPoint = transform.Find(startPointName);
        UnityEngine.Transform endPoint = transform.Find(endPointName);

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError($"One or both points '{startPointName}' or '{endPointName}' not found.");
            DestroyImmediate(lineObject);
            return;
        }

        // Update the line's position
        EditorApplication.CallbackFunction updateLine = () =>
        {
            if (lineRenderer != null && startPoint != null && endPoint != null)
            {
                lineRenderer.SetPosition(0, startPoint.position);
                lineRenderer.SetPosition(1, endPoint.position);
            }
        };

        // Add the update function to the editor update loop
        EditorApplication.update += updateLine;

        // Remove the update function if line is destroyed
        lineObject.AddComponent<DestroyCallback>().OnDestroyAction = () =>
        {
            EditorApplication.update -= updateLine;
        };
    }







    // Set Up
    public void SetUpMove1()
    {
        // Check if MovePreview1 exists
        UnityEngine.Transform existingPreview = transform.Find("MovePreview1");
        if (existingPreview != null)
        {
            Debug.LogWarning("MovePreview1 already exists. Skipping setup.");
            return;
        }

        DuplicateChildrenPreview("MovePreview1");
        AddRedSprite(true, "VisualPointParent", ""); // Adds Red to the Dynamic Parent
        if (AddVisualLines)
        {
            AddRedSprite(false, "VisualPoint1", "MovePreview1");
            ConnectVisualLine("VisualLine1", "VisualPointParent", "MovePreview1/VisualPoint1", "MovePreview1");
        }
        StartPositionMonitoring("MovePreview1");
    }
    public void SetUpMove2()
    {
        // Check if MovePreview1 exists
        if (transform.Find("MovePreview1") == null)
        {
            Debug.LogError("Please Setup the previous preview first!");
            return;
        }

        // Check if MovePreview2 exists
        UnityEngine.Transform existingPreview = transform.Find("MovePreview2");
        if (existingPreview != null)
        {
            Debug.LogWarning("MovePreview2 already exists. Skipping setup.");
            return;
        }

        DuplicateChildrenPreview("MovePreview2");

        if (AddVisualLines)
        {
            AddRedSprite(false, "VisualPoint2", "MovePreview2");
            ConnectVisualLine("VisualLine2", "MovePreview1/VisualPoint1", "MovePreview2/VisualPoint2", "MovePreview2");
        }
        StartPositionMonitoring("MovePreview2");
    }
    public void SetUpMove3()
    {
        // Check if MovePreview1 and MovePreview2 exist
        if (transform.Find("MovePreview1") == null || transform.Find("MovePreview2") == null)
        {
            Debug.LogError("Please Setup the previous preview first!");
            return;
        }

        // Check if MovePreview3 exists
        UnityEngine.Transform existingPreview = transform.Find("MovePreview3");
        if (existingPreview != null)
        {
            Debug.LogWarning("MovePreview3 already exists. Skipping setup.");
            return;
        }

        DuplicateChildrenPreview("MovePreview3");

        if (AddVisualLines)
        {
            AddRedSprite(false, "VisualPoint3", "MovePreview3");
            ConnectVisualLine("VisualLine3", "MovePreview2/VisualPoint2", "MovePreview3/VisualPoint3", "MovePreview3");
        }
        StartPositionMonitoring("MovePreview3");
    }
    public void SetUpMove4()
    {
        // Check if MovePreview1, MovePreview2, and MovePreview3 exist
        if (transform.Find("MovePreview1") == null || transform.Find("MovePreview2") == null || transform.Find("MovePreview3") == null)
        {
            Debug.LogError("Please Setup the previous preview first!");
            return;
        }

        // Check if MovePreview4 exists
        UnityEngine.Transform existingPreview = transform.Find("MovePreview4");
        if (existingPreview != null)
        {
            Debug.LogWarning("MovePreview4 already exists. Skipping setup.");
            return;
        }

        DuplicateChildrenPreview("MovePreview4");

        if (AddVisualLines)
        {
            AddRedSprite(false, "VisualPoint4", "MovePreview4");
            ConnectVisualLine("VisualLine4", "MovePreview3/VisualPoint3", "MovePreview4/VisualPoint4", "MovePreview4");
        }
        StartPositionMonitoring("MovePreview4");
    }
    public void SetUpMove5()
    {
        // Check if MovePreview1, MovePreview2, MovePreview3, and MovePreview4 exist
        if (transform.Find("MovePreview1") == null || transform.Find("MovePreview2") == null || transform.Find("MovePreview3") == null || transform.Find("MovePreview4") == null)
        {
            Debug.LogError("Please Setup the previous preview first!");
            return;
        }

        // Check if MovePreview5 exists
        UnityEngine.Transform existingPreview = transform.Find("MovePreview5");
        if (existingPreview != null)
        {
            Debug.LogWarning("MovePreview5 already exists. Skipping setup.");
            return;
        }

        DuplicateChildrenPreview("MovePreview5");

        if (AddVisualLines)
        {
            AddRedSprite(false, "VisualPoint5", "MovePreview5");
            ConnectVisualLine("VisualLine5", "MovePreview4/VisualPoint4", "MovePreview5/VisualPoint5", "MovePreview5");
        }
        StartPositionMonitoring("MovePreview5");
    }

    public void ClearMovePreview()
    {
        // List of MovePreview names
        string[] movePreviewNames = { "MovePreview1", "MovePreview2", "MovePreview3", "MovePreview4", "MovePreview5", "VisualPointParent" };

        foreach (string previewName in movePreviewNames)
        {
            // Find the MovePreview object under the Dynamic Parent
            UnityEngine.Transform previewObject = transform.Find(previewName);

            // If it exists, destroy it
            if (previewObject != null)
            {
                DestroyImmediate(previewObject.gameObject); // Immediate destroy in editor mode
            }
        }

        if (isImageDynamic)
        {
            foreach (Transform child in transform)
            {
                if (child.name == transform.name)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            isImageDynamic = false;
        }
    }
    public void FinishSetUp()
    {
        UnityEngine.Transform previewObject = transform.Find("MovePreview1");

        if (previewObject != null)
        {
            DestroyImmediate(previewObject.gameObject);
            Debug.Log("MovePreview1 has been cleared.");
        }
        else
        {
            Debug.LogWarning("MovePreview1 does not exist.");
        }
    }

    public void ResetMoveIntervals()
    {
        // Reset all MoveIntervals
        MoveInterval1.MoveXAxis = 0f;
        MoveInterval1.MoveYAxis = 0f;
        MoveInterval1.SupportXAxis = 0f;
        MoveInterval1.SupportYAxis = 0f;

        MoveInterval2.MoveXAxis = 0f;
        MoveInterval2.MoveYAxis = 0f;
        MoveInterval2.SupportXAxis = 0f;
        MoveInterval2.SupportYAxis = 0f;

        MoveInterval3.MoveXAxis = 0f;
        MoveInterval3.MoveYAxis = 0f;
        MoveInterval3.SupportXAxis = 0f;
        MoveInterval3.SupportYAxis = 0f;

        MoveInterval4.MoveXAxis = 0f;
        MoveInterval4.MoveYAxis = 0f;
        MoveInterval4.SupportXAxis = 0f;
        MoveInterval4.SupportYAxis = 0f;

        MoveInterval5.MoveXAxis = 0f;
        MoveInterval5.MoveYAxis = 0f;
        MoveInterval5.SupportXAxis = 0f;
        MoveInterval5.SupportYAxis = 0f;

        Debug.Log("MoveIntervals Resetted.");
    }

    public void EditMoveSetup()
    {
        // Check and set up movements
        if (MovementUsage.UseMovement1) SetUpMoveWithOffset(1);
        if (MovementUsage.UseMovement2) SetUpMoveWithOffset(2);
        if (MovementUsage.UseMovement3) SetUpMoveWithOffset(3);
        if (MovementUsage.UseMovement4) SetUpMoveWithOffset(4);
        if (MovementUsage.UseMovement5) SetUpMoveWithOffset(5);
    }

    private void SetUpMoveWithOffset(int moveIndex)
    {
        switch (moveIndex)
        {
            case 1:
                SetUpMove1();
                break;
            case 2:
                SetUpMove2();
                break;
            case 3:
                SetUpMove3();
                break;
            case 4:
                SetUpMove4();
                break;
            case 5:
                SetUpMove5();
                break;
            default:
                Debug.LogError($"Invalid move index: {moveIndex}");
                return;
        }

        // Calculate cumulative offset for the MovePreview
        Vector3 cumulativeOffset = Vector3.zero;
        for (int i = 1; i <= moveIndex; i++)
        {
            var interval = GetMoveInterval(i);
            cumulativeOffset += new Vector3(interval.MoveXAxis, interval.MoveYAxis, 0f);
        }

        // Apply offset to the MovePreview position
        string movePreviewName = $"MovePreview{moveIndex}";
        Transform movePreview = transform.Find(movePreviewName);
        if (movePreview != null)
        {
            movePreview.position += cumulativeOffset;
        }
        else
        {
            Debug.LogWarning($"{movePreviewName} not found.");
        }
    }

    private Movement GetMoveInterval(int index)
    {
        return index switch
        {
            1 => MoveInterval1,
            2 => MoveInterval2,
            3 => MoveInterval3,
            4 => MoveInterval4,
            5 => MoveInterval5,
            _ => null,
        };
    }




    // Previewing
    private bool isPlayingPreview = false;
    private bool _isPreviewDisabled = false;
    public bool IsPreviewDisabled
    {
        get => _isPreviewDisabled;
        private set => _isPreviewDisabled = value;
    }

    private float postPreviewResetDelay = 0.5f; // Delay before reset
    private bool isWaitingForDelay = false; // Track the delay state

    private Vector3 originalPosition;
    private List<Movement> activeMovements;
    private int currentMovementIndex = 0;
    private float elapsedTime = 0f;


    public void PlayPreview()
    {
        if (isPlayingPreview)
        {
            Debug.LogWarning("Preview is already running!");
            return;
        }

        // Null check
        if (this == null || transform == null)
        {
            Debug.LogError("DynamicParent is null or removed! Cancelling preview.");
            return;
        }

        // Check for "MovePreview1"
        UnityEngine.Transform movePreview1 = transform.Find("MovePreview1");
        if (movePreview1 != null)
        {
            Debug.LogWarning("Cannot play preview while setting up move!");
            return;
        }

        // Disable buttons during preview
        IsPreviewDisabled = true;

        // original position
        originalPosition = transform.position;
        elapsedTime = 0f;                      // Reset elapsed time
        currentMovementIndex = 0;              // Start from the first movement

        // active movements in sequence
        activeMovements = new List<Movement>();
        if (MovementUsage.UseMovement1) activeMovements.Add(MoveInterval1);
        if (MovementUsage.UseMovement2) activeMovements.Add(MoveInterval2);
        if (MovementUsage.UseMovement3) activeMovements.Add(MoveInterval3);
        if (MovementUsage.UseMovement4) activeMovements.Add(MoveInterval4);
        if (MovementUsage.UseMovement5) activeMovements.Add(MoveInterval5);

        if (activeMovements.Count == 0)
        {
            Debug.LogWarning("No movements are active for preview!");
            IsPreviewDisabled = false; // Re-enable buttons if no movements
            return;
        }

        // preview start
        isPlayingPreview = true;
        isWaitingForDelay = activeMovements[0].Delay > 0f;
        EditorApplication.update += UpdatePreviewMovement;
    }

    private void UpdatePreviewMovement()
    {
        if (this == null || transform == null)
        {
            Debug.LogError("Dynamic Parent is removed during preview! Stopping preview.");
            StopPreviewOnNull();
            return;
        }

        Movement currentMovement = activeMovements[currentMovementIndex];

        // Delay 
        if (elapsedTime < currentMovement.Delay)
        {
            isWaitingForDelay = true; // Delay is active
            elapsedTime += Time.deltaTime;
            return; // Pause here until the delay is over
        }

        isWaitingForDelay = false; // Delay is over

        float adjustedTime = elapsedTime - currentMovement.Delay;
        elapsedTime += Time.deltaTime;

        // Calculate start, target, and support positions
        Vector3 startPosition = currentMovementIndex == 0 ? originalPosition :
            originalPosition + CalculateCumulativeOffset(currentMovementIndex - 1);

        Vector3 targetPosition = startPosition + new Vector3(
            currentMovement.MoveXAxis,
            currentMovement.MoveYAxis,
            0f
        );

        Vector3 supportPosition = startPosition + new Vector3(
            currentMovement.SupportXAxis,
            currentMovement.SupportYAxis,
            0f
        );

        // Smooth movement using easing
        float t = Mathf.Clamp01(adjustedTime / currentMovement.MoveDuration);
        transform.position = CalculateEasedPosition(t, startPosition, supportPosition, targetPosition);

        // Check if movement is complete
        if (adjustedTime >= currentMovement.MoveDuration)
        {
            transform.position = targetPosition; // snap to the final position
            currentMovementIndex++;
            elapsedTime = 0f; // reset elapsed time for the next movement

            // Handle next movement or finishing
            if (currentMovementIndex < activeMovements.Count)
            {
                // Prepare for the next interval
                elapsedTime = 0f;
                isWaitingForDelay = activeMovements[currentMovementIndex].Delay > 0f; // Update delay status
            }
            else
            {
                FinishPreview();
            }
        }
    }

    private void FinishPreview()
    {
        isPlayingPreview = false;
        EditorApplication.update -= UpdatePreviewMovement;

        if (ResetAfterPreviewFinish)
        {
            elapsedTime = 0f; // Reset elapsed time for potential delay handling
            EditorApplication.update += HandleResetDelay;
        }
        else
        {
            IsPreviewDisabled = false; // Reenable buttons
        }
    }

    private void StopPreviewOnNull()
    {
        isPlayingPreview = false;
        EditorApplication.update -= UpdatePreviewMovement;

        if (ResetAfterPreviewFinish)
        {
            elapsedTime = 0f;
            EditorApplication.update -= HandleResetDelay;
        }

        IsPreviewDisabled = false;
    }

    private void HandleResetDelay()
    {
        if ((float)EditorApplication.timeSinceStartup - elapsedTime >= postPreviewResetDelay)
        {
            ResetPreviewPosition();
            EditorApplication.update -= HandleResetDelay;
            IsPreviewDisabled = false; // Reenable the buttons
        }
    }

    public void ResetPreviewPosition()
    {
        if (isPlayingPreview)
        {
            Debug.LogWarning("Cannot reset position during preview!");
            return;
        }
        transform.position = originalPosition;
    }

    private Vector3 CalculateEasedPosition(float t, Vector3 start, Vector3 support, Vector3 end)
    {
        // Linear case: When support is exactly midpoint of start and end
        if (support == (start + end) / 2f)
        {
            return Vector3.Lerp(start, end, t); // Purely linear
        }

        // Quadratic Bézier easing for custom control
        float u = 1 - t; // Complement of t
        Vector3 point = u * u * start;           // (1-t)^2 * P0
        point += 2 * u * t * support;            // 2(1-t)t * P1
        point += t * t * end;                    // t^2 * P2
        return point;
    }

    private Vector3 CalculateCumulativeOffset(int lastIndex)
    {
        // Calculate the cumulative offset from all previous movements
        Vector3 cumulativeOffset = Vector3.zero;
        for (int i = 0; i <= lastIndex; i++)
        {
            cumulativeOffset += new Vector3(
                activeMovements[i].MoveXAxis,
                activeMovements[i].MoveYAxis,
                0f
            );
        }
        return cumulativeOffset;
    }
}




[CustomEditor(typeof(Dynamic))]
public class DynamicButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Dynamic dynamicComponent = (Dynamic)target;

        // Disable buttons during preview
        GUI.enabled = !dynamicComponent.IsPreviewDisabled;

        if (GUILayout.Button("Play Preview")) { dynamicComponent.PlayPreview(); }
        if (GUILayout.Button("Reset Preview")) { dynamicComponent.ResetPreviewPosition(); }

        if (GUILayout.Button("Set-Up Move 1")) { dynamicComponent.SetUpMove1(); }
        if (GUILayout.Button("Set-Up Move 2")) { dynamicComponent.SetUpMove2(); }
        if (GUILayout.Button("Set-Up Move 3")) { dynamicComponent.SetUpMove3(); }
        if (GUILayout.Button("Set-Up Move 4")) { dynamicComponent.SetUpMove4(); }
        if (GUILayout.Button("Set-Up Move 5")) { dynamicComponent.SetUpMove5(); }
        if (GUILayout.Button("Edit Movement")) { dynamicComponent.EditMoveSetup(); }

        if (GUILayout.Button("Finish All Set-Up")) { dynamicComponent.FinishSetUp(); }
        if (GUILayout.Button("Reset MoveIntervals")) { dynamicComponent.ResetMoveIntervals(); }

        GUI.enabled = true; // Re-enable GUI for other actions
    }

}

public class DestroyCallback : MonoBehaviour
{
    public Action OnDestroyAction;

    private void OnDestroy()
    {
        OnDestroyAction?.Invoke();
    }
}