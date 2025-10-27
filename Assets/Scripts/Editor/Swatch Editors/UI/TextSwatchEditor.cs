using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Custom editor for UI.Text that integrates swatch-based color management.
/// </summary>
[CustomEditor(typeof(Text))]
public class TextSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used UI.Text properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Text"));

        // Character section with individual properties
        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_Font"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_FontStyle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_FontSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_LineSpacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_RichText"));
        EditorGUI.indentLevel--;

        // Paragraph section with individual properties
        EditorGUILayout.LabelField("Paragraph", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Alignment();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_AlignByGeometry"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_HorizontalOverflow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_VerticalOverflow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FontData.m_BestFit"));
        EditorGUI.indentLevel--;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary UI.Text properties below the swatch section.
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

    #region Alignment Implementation

    private enum VerticalTextAlignment
    {
        Top,
        Middle,
        Bottom
    }

    private enum HorizontalTextAlignment
    {
        Left,
        Center,
        Right
    }

    private const int kAlignmentButtonWidth = 20;

    private void Alignment()
    {
        SerializedProperty alignmentProp = serializedObject.FindProperty("m_FontData.m_Alignment");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Alignment", GUILayout.Width(EditorGUIUtility.labelWidth - 4));

        // Horizontal alignment buttons
        TextAnchor currentAlignment = (TextAnchor)alignmentProp.intValue;
        HorizontalTextAlignment horizontalAlignment = GetHorizontalAlignment(currentAlignment);

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(horizontalAlignment == HorizontalTextAlignment.Left,
            EditorGUIUtility.IconContent("GUISystem/align_horizontally_left", "Left Align"),
            EditorStyles.miniButtonLeft, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetHorizontalAlignment(alignmentProp, HorizontalTextAlignment.Left);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(horizontalAlignment == HorizontalTextAlignment.Center,
            EditorGUIUtility.IconContent("GUISystem/align_horizontally_center", "Center Align"),
            EditorStyles.miniButtonMid, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetHorizontalAlignment(alignmentProp, HorizontalTextAlignment.Center);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(horizontalAlignment == HorizontalTextAlignment.Right,
            EditorGUIUtility.IconContent("GUISystem/align_horizontally_right", "Right Align"),
            EditorStyles.miniButtonRight, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetHorizontalAlignment(alignmentProp, HorizontalTextAlignment.Right);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        GUILayout.Space(10);

        // Vertical alignment buttons
        VerticalTextAlignment verticalAlignment = GetVerticalAlignment(currentAlignment);

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(verticalAlignment == VerticalTextAlignment.Top,
            EditorGUIUtility.IconContent("GUISystem/align_vertically_top", "Top Align"),
            EditorStyles.miniButtonLeft, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetVerticalAlignment(alignmentProp, VerticalTextAlignment.Top);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(verticalAlignment == VerticalTextAlignment.Middle,
            EditorGUIUtility.IconContent("GUISystem/align_vertically_center", "Middle Align"),
            EditorStyles.miniButtonMid, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetVerticalAlignment(alignmentProp, VerticalTextAlignment.Middle);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Toggle(verticalAlignment == VerticalTextAlignment.Bottom,
            EditorGUIUtility.IconContent("GUISystem/align_vertically_bottom", "Bottom Align"),
            EditorStyles.miniButtonRight, GUILayout.Width(kAlignmentButtonWidth)))
        {
            if (EditorGUI.EndChangeCheck())
            {
                SetVerticalAlignment(alignmentProp, VerticalTextAlignment.Bottom);
            }
        }
        else
        {
            EditorGUI.EndChangeCheck();
        }

        EditorGUILayout.EndHorizontal();
    }

    private static HorizontalTextAlignment GetHorizontalAlignment(TextAnchor ta)
    {
        switch (ta)
        {
            case TextAnchor.MiddleCenter:
            case TextAnchor.UpperCenter:
            case TextAnchor.LowerCenter:
                return HorizontalTextAlignment.Center;

            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                return HorizontalTextAlignment.Right;

            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                return HorizontalTextAlignment.Left;
        }

        return HorizontalTextAlignment.Left;
    }

    private static VerticalTextAlignment GetVerticalAlignment(TextAnchor ta)
    {
        switch (ta)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                return VerticalTextAlignment.Top;

            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                return VerticalTextAlignment.Middle;

            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                return VerticalTextAlignment.Bottom;
        }

        return VerticalTextAlignment.Top;
    }

    private void SetHorizontalAlignment(SerializedProperty alignmentProp, HorizontalTextAlignment horizontalAlignment)
    {
        TextAnchor currentAlignment = (TextAnchor)alignmentProp.intValue;
        VerticalTextAlignment currentVerticalAlignment = GetVerticalAlignment(currentAlignment);
        alignmentProp.intValue = (int)GetAnchor(currentVerticalAlignment, horizontalAlignment);
        serializedObject.ApplyModifiedProperties();
    }

    private void SetVerticalAlignment(SerializedProperty alignmentProp, VerticalTextAlignment verticalAlignment)
    {
        TextAnchor currentAlignment = (TextAnchor)alignmentProp.intValue;
        HorizontalTextAlignment currentHorizontalAlignment = GetHorizontalAlignment(currentAlignment);
        alignmentProp.intValue = (int)GetAnchor(verticalAlignment, currentHorizontalAlignment);
        serializedObject.ApplyModifiedProperties();
    }

    private static TextAnchor GetAnchor(VerticalTextAlignment verticalAlignment, HorizontalTextAlignment horizontalAlignment)
    {
        TextAnchor anchor = TextAnchor.UpperLeft;

        switch (horizontalAlignment)
        {
            case HorizontalTextAlignment.Left:
                switch (verticalAlignment)
                {
                    case VerticalTextAlignment.Bottom:
                        anchor = TextAnchor.LowerLeft;
                        break;
                    case VerticalTextAlignment.Middle:
                        anchor = TextAnchor.MiddleLeft;
                        break;
                    default:
                        anchor = TextAnchor.UpperLeft;
                        break;
                }
                break;
            case HorizontalTextAlignment.Center:
                switch (verticalAlignment)
                {
                    case VerticalTextAlignment.Bottom:
                        anchor = TextAnchor.LowerCenter;
                        break;
                    case VerticalTextAlignment.Middle:
                        anchor = TextAnchor.MiddleCenter;
                        break;
                    default:
                        anchor = TextAnchor.UpperCenter;
                        break;
                }
                break;
            default:
                switch (verticalAlignment)
                {
                    case VerticalTextAlignment.Bottom:
                        anchor = TextAnchor.LowerRight;
                        break;
                    case VerticalTextAlignment.Middle:
                        anchor = TextAnchor.MiddleRight;
                        break;
                    default:
                        anchor = TextAnchor.UpperRight;
                        break;
                }
                break;
        }
        return anchor;
    }

    #endregion
}