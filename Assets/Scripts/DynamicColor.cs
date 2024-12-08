using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Vectorier/DynamicColor")]
public class DynamicColor : MonoBehaviour
{
    [SerializeField]
    public string TransformationName = "";
    [Tooltip("Duration in seconds")] public float Duration = 1.0f;
    [Tooltip("Which color and transparency to change into.")] public Color ChangeToColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private Color savedColor;
    private bool isTransitioning = false;

    private void GetSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void PreviewColor(Action onFinish = null)
    {
        GetSpriteRenderer();

        if (spriteRenderer != null)
        {
            if (!isTransitioning)
            {
                savedColor = spriteRenderer.color;
            }
            EditorCoroutine.Start(GraduallyChangeColor(spriteRenderer, ChangeToColor, Duration, onFinish));
        }
    }

    public void ResetColor()
    {
        GetSpriteRenderer();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = savedColor;
        }
    }

    private IEnumerator GraduallyChangeColor(SpriteRenderer spriteRenderer, Color targetColor, float duration, Action onFinish)
    {
        isTransitioning = true;
        Color initialColor = spriteRenderer.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(initialColor, targetColor, elapsedTime / duration);
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }

        spriteRenderer.color = targetColor;
        yield return new EditorWaitForSeconds(0.5f);

        ResetColor();
        isTransitioning = false;
        onFinish?.Invoke();
    }
}

[CustomEditor(typeof(DynamicColor))]
public class DynamicColorButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DynamicColor dynamicColorComponent = (DynamicColor)target;

        if (GUILayout.Button("Preview Color"))
        {
            dynamicColorComponent.PreviewColor(() =>
            {
                MarkObjectAsDirty(dynamicColorComponent);
            });
        }
    }

    private void MarkObjectAsDirty(DynamicColor dynamicColorComponent)
    {
        EditorUtility.SetDirty(dynamicColorComponent);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}

public class EditorCoroutine
{
    public static IEnumerator Start(IEnumerator update, Action onFinish = null)
    {
        EditorApplication.CallbackFunction callback = null;
        callback = () =>
        {
            try
            {
                if (update.MoveNext() == false)
                {
                    EditorApplication.update -= callback;
                    onFinish?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.update -= callback;
            }
        };

        EditorApplication.update += callback;
        return update;
    }
}

public class EditorWaitForSeconds : IEnumerator
{
    private float waitTime;
    private float startTime;

    public EditorWaitForSeconds(float time)
    {
        waitTime = time;
        startTime = (float)EditorApplication.timeSinceStartup;
    }

    public bool MoveNext()
    {
        return (float)EditorApplication.timeSinceStartup < startTime + waitTime;
    }

    public void Reset() { }
    public object Current { get { return null; } }
}
