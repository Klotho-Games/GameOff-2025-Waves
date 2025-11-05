using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class BeamController : MonoBehaviour
{
    /// <summary>
    /// Points that make up the beam path.
    /// </summary>
    public List<Vector2> Points = new() { Vector2.zero };

    [SerializeField] private Camera mainCamera;

    private LineRenderer lineRenderer;
    private float timer = 0f;
    private const float interval = 0.1f;
    private bool enableDebugMousePosition = false;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            UpdateBeamPath();
            SetLineRenderer();
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
        Points.Clear();
        Points.Add((Vector2)transform.position);

        Vector2 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector2.zero)
            return;

        Vector2 direction = (mouseWorldPos - (Vector2)transform.position).normalized;

        bool hitIsDarkness = false;
        int maxBounces = 10; // Safety limit to prevent infinite loops
        int bounceCount = 0;
        GameObject previouslyHitObject = null;

        while (hitIsDarkness == false && bounceCount < maxBounces)
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
        }

        // If we hit max bounces, add a warning
        if (bounceCount >= maxBounces)
        {
            Debug.LogWarning($"Beam reached maximum bounce limit ({maxBounces}). Check gate logic.");
        }


        static RaycastInfo RaycastForFirstGateTypeOrTheBigDarknessTag(Vector2 origin, Vector2 direction, GameObject ignoreObject)
        {
            // Visualize the ray in the editor for debug
            Debug.DrawRay(origin, direction * Mathf.Infinity, Color.cyan, interval);

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
    }

    

    private void SetLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = Points.Count;
        // set subsequent positions from Points[0]..Points[Count-1]
        for (int i = 0; i < Points.Count; ++i)
        {
            lineRenderer.SetPosition(i, new Vector3(Points[i].x, Points[i].y, 0f));
        }
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
