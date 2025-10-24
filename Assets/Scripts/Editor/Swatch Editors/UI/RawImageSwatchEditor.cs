using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Custom editor for UI.RawImage that integrates swatch-based color management.
/// </summary>
[CustomEditor(typeof(RawImage))]
public class RawImageSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used UI.RawImage properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Texture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary UI.RawImage properties below the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastPadding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_UVRect"));
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current UI.RawImage target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            RawImage RawImage = (RawImage)target;
            return RawImage.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new UI.RawImage components that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            RawImage RawImage = (RawImage)t;

            if (RawImage.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(RawImage);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(RawImage);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(RawImage);
            }
        }
    }

    /// <summary>
    /// Checks if the UI.RawImage's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        RawImage RawImage = (RawImage)target;
        SwatchColorReference swatchRef = RawImage.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return RawImage.color != swatchColor;
    }
}