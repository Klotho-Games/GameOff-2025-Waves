using UnityEngine;

/// <summary>
/// Example custom component that demonstrates how any component with a 'color' property
/// can work with the SwatchColorReference system.
/// </summary>
public class CustomColoredComponent : MonoBehaviour
{
    [SerializeField] private Color _color = Color.white;
    
    /// <summary>
    /// Public color property that SwatchColorReference can automatically detect and use.
    /// </summary>
    public Color color
    {
        get => _color;
        set
        {
            _color = value;
            // Apply the color change to whatever this component controls
            // For example, you might update a material, particle system, etc.
            ApplyColorChange();
        }
    }
    
    private void ApplyColorChange()
    {
        // Example: Apply to a material if present
        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = _color;
        }
        
        // Example: Apply to a light if present  
        var light = GetComponent<Light>();
        if (light != null)
        {
            light.color = _color;
        }
        
        Debug.Log($"CustomColoredComponent: Color changed to {_color}");
    }
}