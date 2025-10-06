using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Component that links a SpriteRenderer to a specific color in the ColorPalette system.
/// 
/// WHY: Provides the runtime bridge between sprites and the palette system. Without this component,
/// sprites would lose their color relationships when the palette changes. This maintains the connection
/// and enables automatic color updates, ensuring visual consistency across the entire project.
/// 
/// The component handles automatic registration for performance optimization and manages serialization
/// edge cases like duplicated objects that need to re-register with the editor system.
/// </summary>
[System.Serializable]
public class SwatchColorReference : MonoBehaviour
{
    /// <summary>
    /// Flag for manual update mode to avoid expensive FindObjectsByType calls.
    /// 
    /// WHY: Set by duplicated objects that need to trigger a one-time editor update to register
    /// themselves in the performance-optimized static list maintained by the editor.
    /// </summary>
    [SerializeField] private bool manualSwatchesUpdate = false;
    
    /// <summary>
    /// Index into the ColorPalette array. -1 indicates no swatch assignment.
    /// 
    /// WHY: HideInInspector prevents clutter - users interact through the swatch buttons, not raw indices.
    /// SerializeField ensures the connection persists through play mode and scene saves.
    /// </summary>
    [SerializeField] private int swatchIndex = -1;
    
    /// <summary>
    /// Cached reference to the required SpriteRenderer component.
    /// 
    /// WHY: Avoids GetComponent calls during frequent Update() color checks, improving runtime performance.
    /// </summary>
    public SpriteRenderer SpriteRenderer { get; private set; }

    /// <summary>
    /// Cache the SpriteRenderer component reference for performance.
    /// 
    /// WHY: Awake runs before Start, ensuring the component is ready when UpdateColorFromPalette
    /// is called. Caching avoids repeated GetComponent calls during runtime color updates.
    /// </summary>
    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer == null)
        {
            Debug.LogError("SwatchColorReference requires a SpriteRenderer component.");
        }
    }

    /// <summary>
    /// Apply the initial palette color when the object starts.
    /// 
    /// WHY: Ensures sprites show the correct palette color immediately when the scene loads,
    /// even if their serialized color doesn't match the current palette (e.g., after palette changes).
    /// </summary>
    private void Start()
    {
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Continuously monitor for palette changes and update color accordingly.
    /// 
    /// WHY: The palette can change at runtime (through editor tools or scripts), and sprites
    /// need to reflect these changes immediately. Update() provides the constant monitoring needed
    /// for real-time color synchronization across all sprites using the same palette.
    /// 
    /// Performance note: Only updates when colors actually differ to minimize SetDirty calls.
    /// </summary>
    public void Update()
    {
        // Check if palette has changed and update color if needed
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Assigns a swatch index and immediately applies its color to the sprite.
    /// 
    /// WHY: Provides atomic operation for swatch assignment. Ensures the visual change happens 
    /// immediately when users click swatch buttons, giving instant feedback and maintaining
    /// the visual connection between palette and sprite.
    /// </summary>
    public void SetSwatchIndexAndApplyColor(int index)
    {
        SetSwatchIndex(index);
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Sets the swatch index without applying color. Used internally for serialization.
    /// 
    /// WHY: Separated from color application to handle cases where only the reference needs
    /// to be updated (like undo operations) without triggering visual changes prematurely.
    /// </summary>
    public void SetSwatchIndex(int index)
    {
        swatchIndex = index;
    }

    /// <summary>
    /// Returns the current swatch index, or -1 if no swatch is assigned.
    /// 
    /// WHY: Provides read access for the editor to highlight the current swatch and 
    /// for systems that need to know which palette color this sprite references.
    /// </summary>
    public int GetSwatchIndex()
    {
        return swatchIndex;
    }

    /// <summary>
    /// Fallback method to re-cache the SpriteRenderer component if needed.
    /// 
    /// WHY: Handles edge cases where the cached reference becomes null (rare Unity scenarios).
    /// Provides recovery mechanism to maintain functionality even in unusual circumstances.
    /// </summary>
    public void GetSpriteRenderer()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer == null) Debug.LogError("SwatchColorReference requires a SpriteRenderer component.");
        return;
    }

    /// <summary>
    /// Updates the sprite's color to match the current palette, but only if they differ.
    /// 
    /// WHY: Called frequently from Update(), so optimization is critical. Only applies changes
    /// when needed to avoid unnecessary SetDirty calls that trigger serialization. The equality
    /// check prevents performance overhead in scenes with many sprites.
    /// 
    /// SetDirty ensures changes persist through play mode and scene saves.
    /// </summary>
    public void UpdateColorFromPalette()
    {
        if (SpriteRenderer == null) GetSpriteRenderer();

        Color tempColor = SpriteRenderer.color;
        Color colorFromPalette = ColorFromPalette();

        if (colorFromPalette != tempColor)
        {
            SpriteRenderer.color = colorFromPalette;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(SpriteRenderer);
#endif
        }
    }

    /// <summary>
    /// Retrieves the color from the palette that this component references.
    /// 
    /// WHY: Centralizes palette access and bounds checking. Returns Color.clear for invalid
    /// indices to provide safe fallback behavior instead of exceptions. This graceful degradation
    /// prevents broken sprites when palette sizes change or indices become invalid.
    /// 
    /// Uses Resources.Load for runtime compatibility - the palette needs to be accessible
    /// from both editor and runtime contexts.
    /// </summary>
    public Color ColorFromPalette()
    {
        if (swatchIndex < 0 || SpriteRenderer == null) return Color.clear;

        ColorPalette palette = Resources.Load<ColorPalette>("ColorPalette");
        if (palette == null || palette.colors == null || swatchIndex >= palette.colors.Length) return Color.clear;

        Color paletteColor = palette.colors[swatchIndex];
        // Always update the color to ensure it matches the palette
        return paletteColor;
    }

    public void ClearSwatchReference()
    {
        swatchIndex = -1;
    }

    public SwatchColorReference[] ManualUpdateSwatchReferencesList()
    {
        if (!manualSwatchesUpdate) return new SwatchColorReference[0];
        
        // Update all swatch references
        SwatchColorReference[] swatchRefs = FindObjectsByType<SwatchColorReference>(FindObjectsSortMode.None);

        manualSwatchesUpdate = false;
        return swatchRefs;
    }
}