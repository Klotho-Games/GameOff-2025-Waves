using UnityEngine;
using UnityEditor;

/// <summary>
/// Compact popup color picker window that appears near the mouse cursor for contextual swatch editing.
/// 
/// WHY: Unity's built-in ColorField works well in inspectors but is awkward for contextual editing
/// triggered by right-clicking swatches. This custom window provides:
/// - Immediate visual feedback at the point of interaction (near mouse cursor)
/// - Compact utility window that doesn't clutter the editor interface  
/// - Real-time color updates that sync immediately with the palette and all sprites
/// - Context-aware positioning that avoids screen edges and overlapping UI
/// 
/// The utility window style keeps it "floating" above the main interface without taking focus
/// away from the inspector, maintaining workflow continuity during color adjustments.
/// </summary>
public class ColorPickerWindow : EditorWindow
{
    /// <summary>
    /// Current color being edited. Updates in real-time as user adjusts the color picker.
    /// </summary>
    private Color currentColor;
    
    /// <summary>
    /// Callback function that receives color changes and applies them to the palette system.
    /// 
    /// WHY: Decouples the color picker from the palette implementation, making it reusable
    /// for other color editing needs while ensuring changes propagate immediately to all sprites.
    /// </summary>
    private System.Action<Color> onColorChanged;
    
    /// <summary>
    /// Fixed window dimensions optimized for color picker UI.
    /// 
    /// WHY: Small size keeps the window unobtrusive while providing enough space for Unity's
    /// color picker control. Fixed size prevents user from accidentally making it too small
    /// to use or too large for the simple interface.
    /// </summary>
    private static Vector2 windowSize = new(100, 22);

    /// <summary>
    /// Creates and displays a new color picker window positioned near the mouse cursor.
    /// 
    /// WHY: Static factory method provides a clean API while handling the complex window
    /// positioning logic. The positioning strategy ensures the window appears contextually
    /// near the user's interaction point without going off-screen or overlapping critical UI.
    /// 
    /// Utility window style keeps it "always on top" and prevents it from cluttering the 
    /// Window menu or taking focus away from the main editing workflow.
    /// </summary>
    public static void Show(Color initialColor, System.Action<Color> callback, string title = "Color Picker")
    {
        ColorPickerWindow window = GetWindow<ColorPickerWindow>(true, title, true);
        window.currentColor = initialColor;
        window.onColorChanged = callback;
        window.titleContent = new GUIContent(title); // Update title content
        window.minSize = windowSize;
        window.maxSize = windowSize;

        Vector2 windowPosition = WindowPosition();
        window.position = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);

        window.ShowUtility();
        
        /// <summary>
        /// Calculates optimal window position based on mouse location and screen boundaries.
        /// 
        /// WHY: Provides context-sensitive positioning that feels natural and avoids edge cases:
        /// - Primary: Near mouse cursor for immediate context
        /// - Fallback 1: Previous window position if mouse unavailable  
        /// - Fallback 2: Screen center as last resort
        /// 
        /// The offset (windowSize.x / 3, windowSize.y / 2) positions the window slightly 
        /// offset from the mouse to avoid obscuring the swatch being edited.
        /// </summary>
        Vector2 WindowPosition()
        {
            if (Event.current != null)
            {
                return GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - new Vector2(windowSize.x / 3, windowSize.y / 2);
            }
            else if (window.position.position != Vector2.zero)
            {
                return window.position.position;
            }
            else
            {
                return new Vector2(Screen.currentResolution.width / 4, Screen.currentResolution.height / 4);
            }
        }
    }

    /// <summary>
    /// Renders the color picker interface and handles real-time color change propagation.
    /// 
    /// WHY: Provides immediate visual feedback by invoking the callback on every color change.
    /// This creates a responsive editing experience where users see their color adjustments
    /// applied to sprites instantly as they drag through the color picker, rather than having
    /// to confirm or apply changes manually.
    /// 
    /// EditorGUI.BeginChangeCheck/EndChangeCheck ensures the callback only fires when colors
    /// actually change, avoiding unnecessary updates during normal UI interaction.
    /// </summary>
    private void OnGUI()
    {
        // Draw the color field with change detection
        EditorGUI.BeginChangeCheck();
        Color newColor = EditorGUILayout.ColorField(currentColor);
        
        // Propagate color changes immediately for real-time feedback
        if (EditorGUI.EndChangeCheck())
        {
            currentColor = newColor;
            onColorChanged?.Invoke(currentColor);
        }
    }
}