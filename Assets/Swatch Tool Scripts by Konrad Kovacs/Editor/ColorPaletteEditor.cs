using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for ColorPalette ScriptableObjects that ensures automatic swatch synchronization.
/// 
/// WHY: Unity's default ScriptableObject editor doesn't know about the swatch system's need for 
/// real-time updates. When artists modify palette colors directly in the inspector, all sprites 
/// using those swatches need to update immediately to reflect the changes.
/// 
/// Without this editor, palette changes would only be visible after:
/// - Recompiling scripts
/// - Entering/exiting play mode  
/// - Manually refreshing the inspector
/// 
/// This creates a poor user experience where color changes appear "broken" until refresh.
/// The custom editor provides immediate visual feedback, making the palette system feel responsive and reliable.
/// </summary>
[CustomEditor(typeof(ColorPalette))]
public class ColorPaletteEditor : Editor
{
    /// <summary>
    /// Renders the default ColorPalette inspector with automatic swatch synchronization.
    /// 
    /// WHY: Maintains Unity's standard ScriptableObject editing experience while adding the critical
    /// swatch update functionality. Users get familiar array editing UI with the added benefit of
    /// seeing their changes applied to sprites in real-time.
    /// 
    /// The GUI.changed check ensures updates only trigger when colors actually change, avoiding
    /// unnecessary performance overhead during normal inspector interaction (scrolling, focusing, etc.).
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Check if any properties were changed
        if (GUI.changed)
        {
            // Use the shared method from CustomSpriteRenderer to update all sprites
            // WHY: Centralizes the update logic in one place and ensures consistent behavior
            // across all systems that need to trigger swatch updates (undo/redo, palette changes, etc.)
            SpriteRendererWithSwatchesEditor.UpdateAllSwatchReferences();
        }
    }
}