using UnityEngine;
using UnityEditor;

/// <summary>
/// Compact popup color picker window that appears near the mouse cursor for contextual swatch editing.
/// </summary>
public class ColorPickerWindow : EditorWindow
{
    /// <summary>
    /// Current color being edited. Updates in real-time as user adjusts the color picker.
    /// </summary>
    private Color currentColor;
    
    /// <summary>
    /// Callback function that receives color changes and applies them to the palette system.
    /// </summary>
    private System.Action<Color> onColorChanged;
    
    /// <summary>
    /// Fixed window dimensions optimized for color picker UI.
    /// </summary>
    private static Vector2 windowSize = new(100, 22);

    /// <summary>
    /// Creates and displays a new color picker window positioned near the mouse cursor.
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