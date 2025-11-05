using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteAlways]
public class TransformFullScreenScaler : MonoBehaviour
{
    [SerializeField] private Camera cam;

    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;
    private float lastCamSize = 0f;
    private Camera lastCam = null;

    void OnEnable()
    {
        Resize();
        CacheState();
    }

    void Update()
    {
        if (cam == null)
            cam = Camera.main;

        // If resolution changed, camera changed, or camera zoom changed, resize
        if (cam != lastCam || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || (cam != null && cam.orthographicSize != lastCamSize))
        {
            Resize();
            CacheState();
        }
    }

    private void OnValidate()
    {
        // Called in editor when serialized fields change
        Resize();
        CacheState();
    }

    private void CacheState()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastCam = cam;
        lastCamSize = cam != null ? cam.orthographicSize : 0f;
    }


    private void Resize()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        transform.localScale = new Vector2(10f * width / height, 10f);
    }
}
