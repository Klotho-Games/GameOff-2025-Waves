using UnityEngine;

/// <summary>
/// Component that marks a 2D sprite as highlightable when hovered over.
/// </summary>
public class HighlightableElement2D : MonoBehaviour {
    [Header("Highlight Settings")]
    [SerializeField] public Transform highlightAnchor;
    [SerializeField] public float highlightScale = 1.1f;
    [SerializeField] public Color highlightTint = new(1.2f, 1.2f, 1.2f, 1f);
    
    /// <summary>
    /// All SpriteRenderer components that will be affected by highlighting.
    /// </summary>
    public SpriteRenderer[] Models { get; private set; }
    
    /// <summary>
    /// Original colors before highlighting.
    /// </summary>
    public Color[] OriginalColors { get; private set; }

    /// <summary>
    /// Cached original colors before highlighting (for restoration).
    /// </summary>
    public Color[] CachedOriginalColors { get; set; }

    [SerializeField] private bool enableDebug = false;
    
    void Awake() {
        // Auto-assign highlight anchor if not set
        if (highlightAnchor == null)
            highlightAnchor = transform;
    }

    void OnEnable() {
        // Find all SpriteRenderer components
        Models = GetComponentsInChildren<SpriteRenderer>();
        OriginalColors = new Color[Models.Length];
        
        // Cache original colors and ensure materials are instanced
        for (int i = 0; i < Models.Length; i++) {
            OriginalColors[i] = Models[i].color;
            // Instance the material to avoid affecting shared materials
            _ = Models[i].material;
        }

        // Ensure we have a 2D collider for detection
        if (GetComponent<Collider2D>() == null)
        {
            if (enableDebug) Debug.LogWarning($"HighlightableElement2D on {gameObject.name} requires a Collider2D component for mouse detection!");
            gameObject.AddComponent<Collider2D>();
        }
    }
    
    /// <summary>
    /// Resets colors to original values (useful for cleanup).
    /// </summary>
    public void ResetToOriginalColors() {
        for (int i = 0; i < Models.Length && i < OriginalColors.Length; i++) {
            if (Models[i] != null) {
                Models[i].color = OriginalColors[i];
            }
        }
    }
}