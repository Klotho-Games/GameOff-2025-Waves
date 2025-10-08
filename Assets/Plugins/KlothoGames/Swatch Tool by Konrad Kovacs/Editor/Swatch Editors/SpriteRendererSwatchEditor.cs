using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for SpriteRenderer that integrates swatch-based color management with 2D-optimized property display.
/// 
/// WHY: Unity's default SpriteRenderer inspector shows all 3D rendering properties (shadows, lighting, etc.) 
/// that are irrelevant for 2D games. This editor provides a cleaner 2D-focused interface with color palette 
/// integration, allowing artists to maintain consistent color schemes across sprites.
/// 
/// Inherits from SwatchEditorBase to reuse all swatch functionality while adding SpriteRenderer-specific
/// property handling and behavior.
/// </summary>
[CustomEditor(typeof(SpriteRenderer))]
public class SpriteRendererWithSwatchesEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used SpriteRenderer properties above the swatch section.
    /// 
    /// WHY: Sprite and Color are the primary properties artists interact with in 2D workflows.
    /// Placing them prominently at the top provides immediate access to the most important controls,
    /// improving workflow efficiency by reducing scrolling in the inspector.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        // Draw only the essential SpriteRenderer properties
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary SpriteRenderer properties below the swatch section, filtered for 2D relevance.
    /// 
    /// WHY: These properties are used less frequently than sprite/color but are still important for 2D.
    /// Excludes 3D-specific properties (shadows, lighting) that clutter the interface for 2D developers.
    /// The conditional size field prevents confusion when in Simple draw mode where size is irrelevant.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipY"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DrawMode"));

        // Show size property only if draw mode is not Simple
        // WHY: In Simple mode, size is determined by the sprite's pixels-per-unit, making the size field 
        // irrelevant and potentially confusing. Only show it when users can actually control it.
        var drawMode = serializedObject.FindProperty("m_DrawMode");
        if (drawMode.enumValueIndex != 0) // Not Simple mode
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Size"));
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Materials"));

        // Sorting properties - essential for 2D layering
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SortingLayerID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SortingOrder"));
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current SpriteRenderer target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            SpriteRenderer sr = (SpriteRenderer)target;
            return sr.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new SpriteRenderers that don't have swatch references.
    /// 
    /// WHY: Provides a seamless first-time experience - new SpriteRenderers automatically
    /// get integrated into the swatch system with a sensible default (swatch 0).
    /// Also handles duplicated objects that need to re-register with the editor system.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)t;

            if (spriteRenderer.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(spriteRenderer);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(spriteRenderer);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(spriteRenderer);
            }
        }
    }

    /// <summary>
    /// Checks if the SpriteRenderer's color has been manually changed outside the swatch system.
    /// 
    /// WHY: Maintains data integrity when users directly modify the color field. If a user
    /// manually changes the color, we clear the swatch reference to avoid confusion when
    /// the palette updates and overwrites the manual color change.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        SpriteRenderer spriteRenderer = (SpriteRenderer)target;
        SwatchColorReference swatchRef = spriteRenderer.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return spriteRenderer.color != swatchColor;
    }
}