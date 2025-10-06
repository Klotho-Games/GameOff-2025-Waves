using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Custom editor for UI.Text that integrates swatch-based color management.
/// 
/// WHY: Demonstrates how the modular SwatchEditorBase system can be easily extended
/// to support different component types. UI.Text components benefit from the same
/// centralized color palette system that SpriteRenderer uses, enabling consistent
/// colors across both world-space sprites and UI elements.
/// 
/// This editor provides a clean interface focused on the properties most relevant
/// to UI development while maintaining full swatch integration.
/// </summary>
[CustomEditor(typeof(Text))]
public class TextSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used UI.Text properties above the swatch section.
    /// 
    /// WHY: Source Text and Color are the primary properties for UI.Text components.
    /// Placing them prominently provides immediate access to essential controls.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Text"));

        // Character section with individual properties
        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_Font"), new GUIContent("Font"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_FontStyle"), new GUIContent("Font Style"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_FontSize"), new GUIContent("Font Size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_LineSpacing"), new GUIContent("Line Spacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_RichText"), new GUIContent("Rich Text"));
        EditorGUI.indentLevel--;

        // Paragraph section with individual properties
        EditorGUILayout.LabelField("Paragraph", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_Alignment"), new GUIContent("Alignment"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_AlignByGeometry"), new GUIContent("Align By Geometry"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_HorizontalOverflow"), new GUIContent("Horizontal Overflow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_VerticalOverflow"), new GUIContent("Vertical Overflow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_BestFit"), new GUIContent("Best Fit"));
        EditorGUI.indentLevel--;
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary UI.Text properties below the swatch section.
    /// 
    /// WHY: These properties are important for UI layout and behavior but are
    /// used less frequently than sprite and color during the design process.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastPadding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current UI.Text target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            Text Text = (Text)target;
            return Text.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new UI.Text components that don't have swatch references.
    /// 
    /// WHY: Provides seamless integration with the swatch system for UI elements.
    /// New Text components automatically get connected to the palette system.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            Text Text = (Text)t;

            if (Text.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(Text);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(Text);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(Text);
            }
        }
    }

    /// <summary>
    /// Checks if the UI.Text's color has been manually changed outside the swatch system.
    /// 
    /// WHY: Maintains data integrity by detecting when users directly modify the color field.
    /// Clears swatch references to prevent confusion when palette updates override manual changes.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        Text Text = (Text)target;
        SwatchColorReference swatchRef = Text.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return Text.color != swatchColor;
    }
}