using UnityEngine;

[ExecuteInEditMode] // Runs in the editor so you can see fixes without pressing Play
[RequireComponent(typeof(SpriteRenderer))]
public class TextureFixer : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private int _mainTexID;

    void OnEnable()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
        
        // We cache the ID for "_MainTex" to make it faster than looking up the string every frame
        _mainTexID = Shader.PropertyToID("_MainTex");
    }

    void LateUpdate()
    {
        UpdateShaderTexture();
    }

    void UpdateShaderTexture()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        // 1. Get the current property block from the renderer
        _renderer.GetPropertyBlock(_propBlock);

        // 2. Force the texture to be the texture of the current sprite frame
        // Note: If using a Sprite Atlas, this sends the Whole Atlas. 
        // The Sprite Renderer handles the UVs automatically to pick the right spot.
        Texture2D currentTexture = _renderer.sprite.texture;
        
        if (currentTexture != null)
        {
            _propBlock.SetTexture(_mainTexID, currentTexture);
        }

        // 3. Apply the modified block back to the renderer
        _renderer.SetPropertyBlock(_propBlock);
    }
    
    // Verify on initialization as requested
    void Start()
    {
        UpdateShaderTexture();
    }
}
