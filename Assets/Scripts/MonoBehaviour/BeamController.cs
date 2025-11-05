using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeamController : MonoBehaviour
{
    /// <summary>
    /// Points that make up the beam path.
    /// </summary>

    [SerializeField] private int _intensity = 10;
    [SerializeField] private GameObject lineRendererObjectPrefab;
    [SerializeField] private Camera mainCamera;

    [ShowInInspector] private Stack<GameObject> spawnedLineRenderers = new();
    private bool enableDebugMousePosition = false;

    private void FixedUpdate()
    {
        DeleteOldLineRenderers();
        UpdateBeamPath();
    }

    private void DeleteOldLineRenderers()
    {
        while (spawnedLineRenderers.Count > 0)
        {
            GameObject lrObj = spawnedLineRenderers.Pop();
            Destroy(lrObj);
        }
    }
    
    struct RaycastInfo
    {
        public bool isDarkness;
        public Vector2 contactPoint;
        public Vector2 normal;
        public GateType gateTypeComponent;
        public GameObject hitObject;
    }

    private void UpdateBeamPath()
    {
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector2.zero)
            return;

        Vector2 direction = (mouseWorldPos - (Vector2)transform.position).normalized;

        DrawNextBeam(_intensity, (Vector2)transform.position, direction, null);

        /* while (hitIsDarkness == false && bounceCount < maxBounces)
        {
            RaycastInfo raycastInfo = RaycastForFirstGateTypeOrTheBigDarknessTag(Points[^1], direction, previouslyHitObject);

            // Safety check: if contactPoint is zero/invalid, treat as darkness
            if (raycastInfo.contactPoint == Vector2.zero || raycastInfo.contactPoint == Points[^1])
            {
                // Add a far point in the current direction and exit
                Points.Add(Points[^1] + direction * 100f);
                break;
            }

            Points.Add(raycastInfo.contactPoint);

            if (raycastInfo.isDarkness)
            {
                hitIsDarkness = true;
            }
            else
            {
                // TODO: Implement proper gate-specific logic based on raycastInfo.gateTypeComponent.gateType
                // For now, this is placeholder reflection logic
                // In a real implementation, you would:
                // - Get the surface normal from the hit
                // - Apply different transformations based on gate type (mirror, lens, etc.)

                // Temporary: reflect with proper normal (you'll need to get this from the hit)
                // Using Vector2.up is just a placeholder - replace with actual surface normal
                Vector2 surfaceNormal = raycastInfo.normal;

                direction = Vector2.Reflect(direction, surfaceNormal);

                previouslyHitObject = raycastInfo.hitObject;
                bounceCount++;
            }
        } */
    }

    private void DrawNextBeam(int intensity, Vector2 origin, Vector2 direction, GameObject ignoreObject)
    {
        --intensity;
        if (intensity <= 0)
            return;

        RaycastInfo raycastInfo = RaycastForFirstGateTypeOrTheBigDarknessTag(origin, direction, ignoreObject);

        #region Draw the line segment
        LineRenderer segmentLR = Instantiate(lineRendererObjectPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<LineRenderer>();
        segmentLR.positionCount = 2;
        segmentLR.widthMultiplier = Mathf.Log(intensity);
        segmentLR.SetPosition(0, new(origin.x, origin.y, 0f));
        segmentLR.SetPosition(1, new(raycastInfo.contactPoint.x, raycastInfo.contactPoint.y, 0f));
        spawnedLineRenderers.Push(segmentLR.gameObject);
        #endregion

        if (raycastInfo.isDarkness)
            return;

        switch (raycastInfo.gateTypeComponent.gateType)
        {
            case GateTypes.Mirror:
                // Reflect the beam
                Vector2 reflectedDir = Vector2.Reflect(direction, raycastInfo.normal);
                DrawNextBeam(intensity, raycastInfo.contactPoint + reflectedDir * 0.01f, reflectedDir, raycastInfo.hitObject);
                break;

            default:
                // For other gate types, just stop the beam for now
                Debug.LogWarning($"Gate type {raycastInfo.gateTypeComponent.gateType} not implemented yet.");
                break;
        }
    }

    private RaycastInfo RaycastForFirstGateTypeOrTheBigDarknessTag(Vector2 origin, Vector2 direction, GameObject ignoreObject)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Infinity);
        if (hits != null && hits.Length > 0)
        {
            // Sort hits by distance to make sure we process nearest-first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == ignoreObject) continue;

                // Check for GateType component
                if (hit.collider.TryGetComponent<GateType>(out var gateComp))
                {
                    return new RaycastInfo
                    {
                        isDarkness = false,
                        contactPoint = hit.point,
                        normal = hit.normal,
                        gateTypeComponent = gateComp,
                        hitObject = hit.collider.gameObject
                    };
                }

                // Check for TheBigDarknessTag
                if (hit.collider.TryGetComponent<TheBigDarknessTag>(out var isDarknessTag))
                {
                    return new RaycastInfo
                    {
                        isDarkness = true,
                        contactPoint = hit.point,
                        normal = Vector2.zero,
                        gateTypeComponent = null,
                        hitObject = null
                    };
                }
            }
        }

        // Nothing relevant hit: extend beam to max distance
        return new RaycastInfo
        {
            isDarkness = true,
            contactPoint = Vector2.zero, // arbitrary far point
            normal = Vector2.zero,
            gateTypeComponent = null,
            hitObject = null
        };
    }
    
    
    /// <summary>
    /// Converts mouse screen position to world position for 2D.
    /// Works with both old Input Manager and new Input System.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = GetMouseScreenPosition();
        if (enableDebugMousePosition) Debug.Log($"Mouse Screen Position: {mouseScreenPos}");
        
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    /// <summary>
    /// Gets mouse screen position using the appropriate input system.
    /// </summary>
    private Vector3 GetMouseScreenPosition()
    {
        // New Input System
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        if (Input.mousePosition != null)
        {
            return Input.mousePosition;
        }
        return Vector3.zero;
    }
}
