using UnityEngine;

/// <summary>
/// Example clickable 2D object that works with the hover system.
/// </summary>
[RequireComponent(typeof(HighlightableElement2D))]
[RequireComponent(typeof(Collider2D))]
public class Clickable2D : MonoBehaviour, IClickable {
    [Header("Click Settings")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private bool enableDebug = true;
    
    private AudioSource audioSource;
    
    void Awake() {
        // Setup audio source if we have a click sound
        if (clickSound != null) {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }
    
    public virtual void OnClick() {
        if (enableDebug) {
            Debug.Log($"Clicked 2D object: {gameObject.name}");
        }
        
        // Play click sound
        if (clickSound != null && audioSource != null) {
            audioSource.PlayOneShot(clickSound);
        }
        
        // Override this method in derived classes for custom click behavior
        HandleCustomClick();
    }
    
    /// <summary>
    /// Override this method to add custom click behavior.
    /// </summary>
    protected virtual void HandleCustomClick() {
        // Example: Destroy the object after a delay
        // Destroy(gameObject, 0.5f);
        
        // Example: Change the sprite color
        // GetComponent<SpriteRenderer>().color = Color.red;
        
        // Add your custom behavior here
        Debug.Log($"Add custom click behavior for {gameObject.name}");
    }
}