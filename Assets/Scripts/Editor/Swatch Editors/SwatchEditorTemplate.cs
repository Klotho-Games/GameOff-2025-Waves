using UnityEngine;
using UnityEditor;

/// <summary>
/// Template for creating custom editors that integrate with the swatch system.
/// 
/// INSTRUCTIONS FOR USE:
/// 1. Copy this file and rename it to match your target component (e.g., "LightSwatchEditor.cs")
/// 2. Replace "YourComponentType" with the actual component type (e.g., Light, Camera, etc.)
/// 3. Update the CustomEditor attribute to target your component type
/// 4. Implement the abstract methods to handle your component's specific properties
/// 5. Customize the property drawing methods to show relevant fields for your component
/// 
/// EXAMPLE for Light component:
/// - Replace "YourComponentType" with "Light"
/// - Update [CustomEditor(typeof(Light))]
/// - In DrawComponentPropertiesAbove(), show light type, intensity, etc.
/// - In DrawComponentPropertiesBelow(), show range, shadows, culling mask, etc.
/// - In HasColorChangedManually(), compare light.color with swatch color
/// </summary>

// [CustomEditor(typeof(YourComponentType))] // UNCOMMENT and replace YourComponentType
public class YourComponentSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draw the most important properties for your component above the swatch section.
    /// 
    /// EXAMPLE for Light:
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Type"));
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Intensity"));
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        // TODO: Add your component's primary properties here
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SomeProperty"));
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draw secondary properties for your component below the swatch section.
    /// 
    /// EXAMPLE for Light:
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Range"));
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Shadows"));
    /// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CullingMask"));
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        // TODO: Add your component's secondary properties here
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SomeOtherProperty"));
    }

    /// <summary>
    /// Get the SwatchColorReference component from your target component.
    /// Usually no changes needed - just update the cast type.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            // TODO: Replace YourComponentType with your actual component type
            // YourComponentType component = (YourComponentType)target;
            // return component.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assign swatch 0 to new components that don't have swatch references.
    /// Update the cast type and component variable name.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            // TODO: Replace YourComponentType with your actual component type
            // YourComponentType component = (YourComponentType)t;

            /*
            if (component.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(component);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(component);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(component);
            }
            */
        }
    }

    /// <summary>
    /// Check if your component's color has been manually changed outside the swatch system.
    /// Update to access your component's color property.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        // TODO: Replace YourComponentType with your actual component type
        // YourComponentType component = (YourComponentType)target;
        // SwatchColorReference swatchRef = component.GetComponent<SwatchColorReference>();
        
        /*
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return component.color != swatchColor; // Replace 'color' with your component's color property name
        */
        
        return false;
    }
}

/*
QUICK SETUP EXAMPLES:

FOR LIGHT COMPONENT:
1. Rename file to "LightSwatchEditor.cs"
2. Replace class name with "LightSwatchEditor"
3. Change [CustomEditor(typeof(Light))]
4. Replace "YourComponentType" with "Light" in all methods
5. In DrawComponentPropertiesAbove():
   EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Type"));
   EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
   EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Intensity"));

FOR CAMERA COMPONENT (if it had a color property):
1. Rename file to "CameraSwatchEditor.cs"
2. Replace class name with "CameraSwatchEditor" 
3. Change [CustomEditor(typeof(Camera))]
4. Replace "YourComponentType" with "Camera" in all methods
5. Add relevant Camera properties in the Draw methods

FOR CUSTOM COMPONENTS:
Follow the same pattern but use your custom component type name.
Make sure your component has a public Color property for the swatch system to work.
*/