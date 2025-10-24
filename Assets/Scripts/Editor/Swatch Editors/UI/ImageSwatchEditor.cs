using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Custom editor for UI.Image that integrates swatch-based color management.
/// </summary>
[CustomEditor(typeof(Image))]
public class ImageSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used UI.Image properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary UI.Image properties below the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastPadding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current UI.Image target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            Image image = (Image)target;
            return image.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new UI.Image components that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            Image image = (Image)t;

            if (image.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(image);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(image);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(image);
            }
        }
    }

    /// <summary>
    /// Checks if the UI.Image's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        Image image = (Image)target;
        SwatchColorReference swatchRef = image.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return image.color != swatchColor;
    }
}