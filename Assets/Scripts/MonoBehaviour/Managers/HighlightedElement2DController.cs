#if PRIME_TWEEN_INSTALLED
using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;

namespace PrimeTweenDemo {
    public class HighlightedElement2DController : MonoBehaviour {
        [SerializeField] Camera mainCamera;
        [SerializeField] LayerMask hoverLayers = -1;
        [SerializeField] bool enableDebug = false;
        [CanBeNull] public HighlightableElement2D current { get; private set; }

        void Awake() {
            // Auto-assign main camera if not set
            if (mainCamera == null) {
                mainCamera = Camera.main;
                if (mainCamera == null) {
                    Debug.LogError("HighlightedElement2DController: No main camera found! Please assign a camera.");
                }
            }
        }

        void Update() {
            if (mainCamera == null) return;
            
            // Handle mobile touch input
            if (Application.isMobilePlatform && Input.touchCount == 0) {
                SetCurrentHighlighted(null);
                return;
            }
            
            // Get mouse world position for 2D raycast
            Vector2 mouseWorldPos = GetMouseWorldPosition();
            var highlightableElement = RaycastHighlightableElement2D(mouseWorldPos);
            SetCurrentHighlighted(highlightableElement);

            // Handle click input
            if (current != null && Input.GetMouseButtonDown(0)) {
                var clickable = current.GetComponent<IClickable>();
                if (clickable != null) {
                    clickable.OnClick();
                }
            }
        }

        /// <summary>
        /// Converts mouse screen position to world position for 2D.
        /// </summary>
        private Vector2 GetMouseWorldPosition() {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = -mainCamera.transform.position.z;
            return mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }
        
        /// <summary>
        /// Uses 2D physics raycast to detect hoverable elements.
        /// </summary>
        [CanBeNull]
        private HighlightableElement2D RaycastHighlightableElement2D(Vector2 worldPosition) {
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, hoverLayers);
            
            if (hit.collider != null) {
                var element = hit.collider.GetComponentInParent<HighlightableElement2D>();
                
                if (enableDebug && element != null) {
                    Debug.Log($"Mouse over 2D element: {element.gameObject.name}");
                }
                
                return element;
            }
            
            return null;
        }

        void SetCurrentHighlighted([CanBeNull] HighlightableElement2D newHighlighted) {
            if (newHighlighted != current) {
                // Remove highlight from previous element
                if (current != null) {
                    AnimateHighlightedElement2D(current, false);
                    
                    if (enableDebug) {
                        Debug.Log($"Stopped hovering 2D: {current.gameObject.name}");
                    }
                }
                
                current = newHighlighted;
                
                // Apply highlight to new element
                if (newHighlighted != null) {
                    AnimateHighlightedElement2D(newHighlighted, true);
                    
                    if (enableDebug) {
                        Debug.Log($"Started hovering 2D: {newHighlighted.gameObject.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Animates 2D sprite highlighting effects.
        /// </summary>
        static void AnimateHighlightedElement2D([NotNull] HighlightableElement2D highlightable, bool isHighlighted) {
            // Scale animation for 2D sprites
            Vector3 targetScale = isHighlighted 
                ? Vector3.one * 1.1f 
                : Vector3.one;
            
            Tween.Scale(highlightable.highlightAnchor, targetScale, 0.2f, Ease.OutBack);
            
            // Color tint animation for 2D sprites
            foreach (var spriteRenderer in highlightable.models) {
                Color targetColor = isHighlighted 
                    ? Color.white * 1.2f 
                    : Color.white;
                
                Tween.Color(spriteRenderer, targetColor, 0.15f, Ease.OutQuad);
            }
        }
        
        /// <summary>
        /// Gets the current mouse world position (useful for other systems).
        /// </summary>
        public Vector2 GetCurrentMouseWorldPosition() {
            return GetMouseWorldPosition();
        }
        
        /// <summary>
        /// Manually clear the current highlight.
        /// </summary>
        public void ClearHighlight() {
            SetCurrentHighlighted(null);
        }
    }
}
#endif
