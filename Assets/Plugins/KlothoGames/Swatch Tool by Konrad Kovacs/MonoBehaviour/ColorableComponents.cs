using UnityEngine;

/// <summary>
/// Wrapper to make SpriteRenderer compatible with IColorable interface.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ColorableSpriteRenderer : MonoBehaviour, IColorable
{
    private SpriteRenderer spriteRenderer;
    
    public Color color
    {
        get => spriteRenderer.color;
        set => spriteRenderer.color = value;
    }
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
}

/// <summary>
/// Wrapper to make UI.Image compatible with IColorable interface.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class ColorableImage : MonoBehaviour, IColorable
{
    private UnityEngine.UI.Image image;
    
    public Color color
    {
        get => image.color;
        set => image.color = value;
    }
    
    private void Awake()
    {
        image = GetComponent<UnityEngine.UI.Image>();
    }
}