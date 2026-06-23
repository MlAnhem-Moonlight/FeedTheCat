using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FitCameraToSprite : MonoBehaviour
{
    public SpriteRenderer targetSprite;
    [Tooltip("When true the camera will lock (follow) the target sprite position every frame")]
    public bool lockToTarget = true;
    [Tooltip("When true the camera will re-fit its size every frame (useful if the target or screen size changes at runtime)")]
    public bool fitEveryFrame = false;
    [Tooltip("Extra world-space padding added around the sprite when fitting the camera (in world units)")]
    public float padding = 0f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            Fit();
    }
#endif

    [ContextMenu("Fit Camera")]
    public void Fit()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (targetSprite == null)
            return;

        Bounds bounds = targetSprite.bounds;

        // include padding on each side
        float spriteWidth = bounds.size.x + padding * 2f;
        float spriteHeight = bounds.size.y + padding * 2f;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = spriteWidth / spriteHeight;

        if (screenRatio >= targetRatio)
        {
            cam.orthographicSize = spriteHeight * 0.5f;
        }
        else
        {
            cam.orthographicSize =
                spriteWidth / (2f * screenRatio);
        }

        transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            transform.position.z
        );
    }

    private void LateUpdate()
    {
        if (targetSprite == null)
            return;

        // Always follow target if requested
        if (lockToTarget)
        {
            Bounds bounds = targetSprite.bounds;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
        }

        // Recalculate camera size when requested
        if (fitEveryFrame)
            Fit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // keep camera reference up-to-date in the editor and preview changes
        cam = GetComponent<Camera>();
        if (!Application.isPlaying)
            Fit();
    }
#endif
}