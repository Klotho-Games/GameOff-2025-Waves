using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class HighlightedElement2DController : MonoBehaviour
{
    const float sizeTweenDuration = 0.2f;
    const float colorTweenDuration = 0.15f;
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask hoverLayers = -1;
    [SerializeField] bool enableDebug = false;
    [ShowInInspector][CanBeNull] public HighlightableElement2D Current { get; private set; }

    void Awake()
    {
        // Auto-assign main camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                if (enableDebug) Debug.LogError("HighlightedElement2DController: No main camera found! Please assign a camera.");
            }
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Handle mobile touch input
        if (Application.isMobilePlatform && !IsTouchActive())
        {
            SetCurrentHighlighted(null);
            return;
        }

        // Get mouse world position for 2D raycast
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        if (enableDebug) Debug.Log($"Mouse World Position: {mouseWorldPos}");
        var highlightableElement = RaycastHighlightableElement2D(mouseWorldPos);
        SetCurrentHighlighted(highlightableElement);

        // Handle click input
        if (Current != null && IsClickPressed())
        {
            var clickable = Current.GetComponent<IClickable>();
            if (clickable != null)
            {
                clickable.OnClick();
            }
        }
    }

    /// <summary>
    /// Converts mouse screen position to world position for 2D.
    /// Works with both old Input Manager and new Input System.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = GetMouseScreenPosition();
        if (enableDebug) Debug.Log($"Mouse Screen Position: {mouseScreenPos}");
        
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

    /// <summary>
    /// Checks if touch is currently active using the appropriate input system.
    /// </summary>
    private bool IsTouchActive()
    {
        return Touchscreen.current != null && Touchscreen.current.touches.Count > 0;
    }

    /// <summary>
    /// Checks if mouse/touch click was pressed this frame using the appropriate input system.
    /// </summary>
    private bool IsClickPressed()
    {
        // New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Uses 2D physics raycast to detect hoverable elements.
    /// </summary>
    [CanBeNull]
    private HighlightableElement2D RaycastHighlightableElement2D(Vector2 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, hoverLayers);

        if (hit.collider != null)
        {
            var element = hit.collider.GetComponentInParent<HighlightableElement2D>();

            if (element != null)
            {
                if (enableDebug) Debug.Log($"Mouse over 2D element: {element.gameObject.name}");
            }
            else
            {
                if (enableDebug) Debug.Log("Mouse over 2D collider with no HighlightableElement2D component.");
            }

            return element;
        }

        return null;
    }

    void SetCurrentHighlighted([CanBeNull] HighlightableElement2D newHighlighted)
    {
        if (newHighlighted != Current)
        {
            // Remove highlight from previous element
            if (Current != null)
            {
                AnimateHighlightedElement2D(Current, false);

                if (enableDebug) Debug.Log($"Stopped hovering 2D: {Current.gameObject.name}");
            }

            Current = newHighlighted;

            // Apply highlight to new element
            if (newHighlighted != null)
            {
                AnimateHighlightedElement2D(newHighlighted, true);

                if (enableDebug) Debug.Log($"Started hovering 2D: {newHighlighted.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Animates 2D sprite highlighting effects.
    /// </summary>
    static void AnimateHighlightedElement2D([NotNull] HighlightableElement2D highlightable, bool isHighlighted)
    {
        // Scale animation
        Vector3 targetScale = isHighlighted
            ? Vector3.one * highlightable.highlightScale
            : Vector3.one / highlightable.highlightScale;
        Tween.Scale(highlightable.highlightAnchor, targetScale, sizeTweenDuration, Ease.OutBack);

        // Color tint animation
        if (isHighlighted)
        {
            // Check if we need to cache original colors
            bool needsColorCaching = highlightable.CachedOriginalColors == null;
            if (needsColorCaching)
            {
                highlightable.CachedOriginalColors = new Color[highlightable.Models.Length];
            }
            
            // Cache colors (if needed) and apply highlight tint in single loop
            for (int i = 0; i < highlightable.Models.Length; i++)
            {
                var spriteRenderer = highlightable.Models[i];
                
                if (needsColorCaching) highlightable.CachedOriginalColors[i] = spriteRenderer.color;
                
                Color targetColor = OverlayColor(highlightable.CachedOriginalColors[i], highlightable.highlightTint, false);
                Tween.Color(spriteRenderer, targetColor, colorTweenDuration, Ease.OutQuad);
            }
        }
        else
        {
            // Restore original colors from cache
            if (highlightable.CachedOriginalColors != null)
            {
                for (int i = 0; i < highlightable.Models.Length && i < highlightable.CachedOriginalColors.Length; i++)
                {
                    Tween.Color(highlightable.Models[i], highlightable.CachedOriginalColors[i], colorTweenDuration, Ease.OutQuad);
                }
                
                // Clear the cache AFTER the tween completes to prevent re-caching during tween
                Tween.Delay(colorTweenDuration).OnComplete(() => {
                    if (highlightable != null)
                    {
                        highlightable.CachedOriginalColors = null;
                        Debug.Log("Cleared CachedOriginalColors after tween.");
                    }
                });
            }
        }
    }

    static Color OverlayColor(Color baseColor, Color overlayColor, bool isUndo)
    {
        return new Color(
            Mathf.Clamp01(baseColor.r * overlayColor.r),
            Mathf.Clamp01(baseColor.g * overlayColor.g),
            Mathf.Clamp01(baseColor.b * overlayColor.b),
            baseColor.a);
    }

    /// <summary>
    /// Gets the current mouse world position (useful for other systems).
    /// </summary>
    public Vector2 GetCurrentMouseWorldPosition()
    {
        return GetMouseWorldPosition();
    }

    /// <summary>
    /// Manually clear the current highlight.
    /// </summary>
    public void ClearHighlight()
    {
        SetCurrentHighlighted(null);
    }
}
