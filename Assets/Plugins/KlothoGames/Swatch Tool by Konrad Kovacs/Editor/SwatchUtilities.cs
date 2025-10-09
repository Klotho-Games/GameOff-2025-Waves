using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Utility class containing shared swatch system functionality that can be used across different editor types.
/// </summary>
public static class SwatchUtilities
{
    /// <summary>
    /// Loads or creates the ColorPalette from Resources folder.
    /// </summary>
    /// <returns>The loaded or newly created ColorPalette</returns>
    public static ColorPalette LoadOrCreateColorPalette()
    {
        ColorPalette palette = Resources.Load<ColorPalette>("ColorPalette");

        if (palette == null)
        {
            palette = ScriptableObject.CreateInstance<ColorPalette>();
            palette.colors = new Color[] { Color.white }; // Default to one white color

            SavePaletteToResources(palette);
        }

        return palette;
    }

    /// <summary>
    /// Saves a ColorPalette to the Resources folder.
    /// </summary>
    /// <param name="palette">The palette to save</param>
    /// <returns>The asset path where the palette was saved</returns>
    public static string SavePaletteToResources(ColorPalette palette)
    {
        // Create Resources folder if it doesn't exist
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Save it as an asset in the Resources folder
        string assetPath = "Assets/Resources/ColorPalette.asset";
        AssetDatabase.CreateAsset(palette, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return assetPath;
    }

    /// <summary>
    /// Finds all components on a GameObject that have a Color property and can work with the swatch system.
    /// </summary>
    /// <param name="gameObject">The GameObject to search</param>
    /// <returns>List of components that have a Color property</returns>
    public static List<Component> FindColorableComponents(GameObject gameObject)
    {
        List<Component> colorableComponents = new();
        Component[] allComponents = gameObject.GetComponents<Component>();

        foreach (var component in allComponents)
        {
            if (component == null) continue;

            // Skip SwatchColorReference itself
            if (component is SwatchColorReference) continue;

            // Check for IColorable interface first (most performant)
            if (component is IColorable)
            {
                colorableComponents.Add(component);
                continue;
            }

            // Check for color property using reflection
            var colorProperty = component.GetType().GetProperty("color", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (colorProperty != null && 
                colorProperty.PropertyType == typeof(Color) && 
                colorProperty.CanRead && 
                colorProperty.CanWrite)
            {
                colorableComponents.Add(component);
            }
        }

        return colorableComponents;
    }

    /// <summary>
    /// Creates a SwatchColorReference component and automatically detects the best target component.
    /// </summary>
    /// <param name="gameObject">The GameObject to add the SwatchColorReference to</param>
    /// <returns>The created SwatchColorReference component, or null if no suitable target found</returns>
    public static SwatchColorReference CreateSwatchColorReference(GameObject gameObject)
    {
        List<Component> colorableComponents = FindColorableComponents(gameObject);
        
        if (colorableComponents.Count == 0)
        {
            Debug.LogWarning($"No colorable components found on {gameObject.name}");
            return null;
        }

        // Prioritize common components
        Component targetComponent = null;
        
        // First priority: SpriteRenderer (most common in 2D)
        targetComponent = gameObject.GetComponent<SpriteRenderer>();
        if (targetComponent != null && colorableComponents.Contains(targetComponent))
        {
            return CreateSwatchReferenceForComponent(targetComponent);
        }

        // Second priority: UI.Image
        targetComponent = gameObject.GetComponent<UnityEngine.UI.Image>();
        if (targetComponent != null && colorableComponents.Contains(targetComponent))
        {
            return CreateSwatchReferenceForComponent(targetComponent);
        }

        // Third priority: Light
        targetComponent = gameObject.GetComponent<Light>();
        if (targetComponent != null && colorableComponents.Contains(targetComponent))
        {
            return CreateSwatchReferenceForComponent(targetComponent);
        }

        // Fallback: Use the first colorable component found
        return CreateSwatchReferenceForComponent(colorableComponents[0]);
    }

    /// <summary>
    /// Creates a SwatchColorReference for a specific component.
    /// </summary>
    /// <param name="targetComponent">The component to create a swatch reference for</param>
    /// <returns>The created SwatchColorReference</returns>
    public static SwatchColorReference CreateSwatchReferenceForComponent(Component targetComponent)
    {
        SwatchColorReference swatchRef = targetComponent.gameObject.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null)
        {
            swatchRef = Undo.AddComponent<SwatchColorReference>(targetComponent.gameObject);
        }

        // The SwatchColorReference will automatically detect the target component
        swatchRef.GetReferencedComponent();
        
        return swatchRef;
    }

    /// <summary>
    /// Updates all SwatchColorReference components to reflect the current palette state.
    /// </summary>
    /// <param name="swatchRefs">List of SwatchColorReference components to update</param>
    public static void UpdateAllSwatchReferences(List<SwatchColorReference> swatchRefs)
    {
        // Clean up null references
        for (int i = swatchRefs.Count - 1; i >= 0; i--)
        {
            if (swatchRefs[i] == null)
            {
                swatchRefs.RemoveAt(i);
                continue;
            }

            var swatchRef = swatchRefs[i];
            if (swatchRef.GetSwatchIndex() < 0) continue;

            swatchRef.UpdateColorFromPalette();
            EditorUtility.SetDirty(swatchRef);
        }
    }

    /// <summary>
    /// Registers a SwatchColorReference in the global list if it's not already present.
    /// </summary>
    /// <param name="swatchRef">The SwatchColorReference to register</param>
    /// <param name="swatchRefs">The list to add it to</param>
    public static void RegisterSwatchReference(SwatchColorReference swatchRef, List<SwatchColorReference> swatchRefs)
    {
        if (swatchRef != null && !swatchRefs.Contains(swatchRef))
        {
            swatchRefs.Add(swatchRef);
        }
    }

    /// <summary>
    /// Creates a context menu for swatch operations (used in right-click menus).
    /// </summary>
    /// <param name="swatchIndex">The index of the swatch to create menu for</param>
    /// <param name="onEditColor">Callback for editing the color</param>
    /// <param name="onCopyColor">Callback for copying the color</param>
    /// <param name="onDuplicate">Callback for duplicating the swatch</param>
    /// <param name="onDelete">Callback for deleting the swatch</param>
    /// <returns>The configured GenericMenu</returns>
    public static GenericMenu CreateSwatchContextMenu(int swatchIndex, 
        System.Action onEditColor, 
        System.Action onCopyColor, 
        System.Action onDuplicate, 
        System.Action onDelete)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Edit Color"), false, () => onEditColor?.Invoke());
        menu.AddItem(new GUIContent("Copy Color"), false, () => onCopyColor?.Invoke());
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Duplicate Swatch"), false, () => onDuplicate?.Invoke());
        menu.AddItem(new GUIContent("Delete Swatch"), false, () => onDelete?.Invoke());

        return menu;
    }

    /// <summary>
    /// Copies a color to the clipboard as a hex string.
    /// </summary>
    /// <param name="color">The color to copy</param>
    public static void CopyColorToClipboard(Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGBA(color);
        EditorGUIUtility.systemCopyBuffer = $"#{colorHex}";
    }

    /// <summary>
    /// Validates that a swatch index is within valid bounds for a palette.
    /// </summary>
    /// <param name="swatchIndex">The index to validate</param>
    /// <param name="palette">The palette to check against</param>
    /// <returns>True if the index is valid</returns>
    public static bool IsValidSwatchIndex(int swatchIndex, ColorPalette palette)
    {
        return palette != null && 
               palette.colors != null && 
               swatchIndex >= 0 && 
               swatchIndex < palette.colors.Length;
    }

    /// <summary>
    /// Gets a safe color from the palette, returning Color.clear for invalid indices.
    /// </summary>
    /// <param name="swatchIndex">The swatch index</param>
    /// <param name="palette">The palette to get color from</param>
    /// <returns>The color at the index, or Color.clear if invalid</returns>
    public static Color GetSafeColorFromPalette(int swatchIndex, ColorPalette palette)
    {
        if (IsValidSwatchIndex(swatchIndex, palette))
        {
            return palette.colors[swatchIndex];
        }
        return Color.clear;
    }
}