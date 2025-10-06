using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor that replaces Unity's default SpriteRenderer inspector with a swatch-based color management system.
/// 
/// WHY: Unity's default SpriteRenderer inspector shows all 3D rendering properties (shadows, lighting, etc.) 
/// that are irrelevant for 2D games. This editor provides a cleaner 2D-focused interface with color palette 
/// integration, allowing artists to maintain consistent color schemes across sprites without manual color picking.
/// 
/// The swatch system enables:
/// - Centralized color management through ColorPalette ScriptableObjects
/// - Automatic color synchronization when palette changes
/// - Performance optimization by maintaining sprite lists instead of expensive scene searches
/// - Seamless integration with Unity's undo system
/// </summary>
[CustomEditor(typeof(SpriteRenderer))]
public class SpriteRendererWithSwatchesEditor : Editor
{
    // UI Configuration - These values balance visual clarity with inspector space efficiency
    private const int boxSize = 18;  // Swatch button size - large enough to see colors clearly, small enough for multiple per row
    private const int highlightPadding = 2;  // Border space around selected swatch for visual feedback
    
    /// <summary>
    /// Performance optimization flag. When enabled, avoids costly FindObjectsByType calls on every inspector refresh.
    /// 
    /// WHY: FindObjectsByType is expensive and getting called every frame the inspector updates would degrade performance. For scenes with 
    /// hundreds of sprites, this causes noticeable editor lag. Manual update mode relies on event notifications 
    /// from SwatchColorReference components instead of constant scene scanning.
    /// </summary>
    private readonly bool enableManualSwatchUpdate = true;

    /// <summary>
    /// Static list maintaining all SwatchColorReference components in the scene.
    /// 
    /// WHY: Static storage allows the list to persist across inspector instances and provides O(1) access 
    /// for batch updates. Components self-register on creation/destruction, eliminating the need for 
    /// expensive FindObjectsByType calls during inspector refreshes.
    /// </summary>
    public static List<SwatchColorReference> SwatchRefs = new();
    
    /// <summary>
    /// Cached reference to the active ColorPalette to avoid repeated Resources.Load calls.
    /// 
    /// WHY: Resources.Load has overhead and the palette rarely changes during editing sessions.
    /// Caching improves inspector performance and reduces disk I/O.
    /// </summary>
    private ColorPalette colorPalette;
    
    /// <summary>
    /// Controls foldout state of the swatch section in the inspector.
    /// 
    /// WHY: Provides user control over inspector real estate. When working with many sprites,
    /// users can collapse swatches to focus on other properties, improving workflow efficiency.
    /// </summary>
    private bool showSwatches = true;

    /// <summary>
    /// Subscribe to Unity's undo system when the editor becomes active.
    /// 
    /// WHY: When users undo/redo palette changes, all sprite colors need to update automatically 
    /// to reflect the new palette state. Without this, sprites would show outdated colors after 
    /// undo operations, breaking the visual connection between swatches and sprites.
    /// </summary>
    private void OnEnable()
    {
        Undo.undoRedoPerformed += UpdateAllSwatchReferences;
    }

    /// <summary>
    /// Unsubscribe from undo system when editor is disabled to prevent memory leaks.
    /// 
    /// WHY: Prevents orphaned event handlers that would cause errors when the editor instance 
    /// is destroyed but callbacks still fire. Essential for proper Unity editor lifecycle management.
    /// </summary>
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= UpdateAllSwatchReferences;
    }

    /// <summary>
    /// Main inspector rendering method that orchestrates the custom SpriteRenderer interface.
    /// 
    /// WHY: Replaces Unity's default inspector with a workflow optimized for 2D development.
    /// The method flow is carefully designed to handle edge cases (new objects, missing palettes, 
    /// manual color changes) while maintaining performance and providing immediate visual feedback.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Load ColorPalette if not already loaded - ensures palette is always available for swatch display
        if (colorPalette == null) LoadColorPalette();

        // Update all swatch references if manual swatch update is enabled - performance optimization
        ManualSwatchUpdate();

        // Auto-assign swatch 0 to new SpriteRenderers - provides sensible defaults for new objects
        AutoAssignDefaultSwatch();

        // Draw SpriteRenderer properties above swatches
        DrawSpriteRendererPropertiesAbove();

        // Draw swatches section
        DrawSwatchesSection();

        // Draw SpriteRenderer properties below swatches
        DrawSpriteRendererPropertiesBelow();

        // Apply property modifications
        serializedObject.ApplyModifiedProperties();

        // Check if any properties were changed and handle swatch desynchronization
        if (GUI.changed)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)target;

            // If color was changed manually (outside swatch system), clear swatch references
            // WHY: Maintains data integrity when users directly modify color field. Without this,
            // the sprite would show a custom color but still reference a swatch, causing confusion
            // when the palette updates and overwrites the manual color change.
            Color swatchColor = spriteRenderer.GetComponent<SwatchColorReference>().ColorFromPalette();
            if (spriteRenderer.color != swatchColor)
            {
                ClearSwatchReferences();
            }
        }

        void DrawSwatchesSection()
        {
            if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0) return;

            SwatchColorReference swatchRef = GetCurrentSwatchReference();

            showSwatches = EditorGUILayout.Foldout(showSwatches, "Swatches", true);

            if (showSwatches)
            {
                DrawCurrentSwatchInfoAndClearButton(swatchRef);
                DrawSwatchButtons(swatchRef);
            }

            void DrawCurrentSwatchInfoAndClearButton(SwatchColorReference swatchRef)
            {
                if (swatchRef == null || swatchRef.GetSwatchIndex() < 0) return;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"Current Swatch: {swatchRef.GetSwatchIndex()}", EditorStyles.miniLabel);

                if (GUILayout.Button("Clear Swatch Reference", GUILayout.Width(150)))
                {
                    ClearSwatchReferences();
                }

                EditorGUILayout.EndHorizontal();
            }

            void DrawSwatchButtons(SwatchColorReference swatchRef)
            {
                // Calculate how many swatches fit per row
                float inspectorWidth = EditorGUIUtility.currentViewWidth - 40; // Account for margins/padding
                int swatchWidth = boxSize + highlightPadding;
                int swatchesPerRow = Mathf.Max(1, (int)(inspectorWidth / swatchWidth));

                for (int i = 0; i < colorPalette.colors.Length; i += swatchesPerRow)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Draw swatches for this row
                    for (int j = i; j < Mathf.Min(i + swatchesPerRow, colorPalette.colors.Length); j++)
                    {
                        DrawSingleSwatchButton(j, swatchRef);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        SwatchColorReference GetCurrentSwatchReference()
        {
            if (target != null)
            {
                SpriteRenderer sr = (SpriteRenderer)target;
                return sr.GetComponent<SwatchColorReference>();
            }
            return null;
        }

        void ClearSwatchReferences()
        {
            foreach (var t in targets)
            {
                SpriteRenderer spriteRenderer = (SpriteRenderer)t;
                SwatchColorReference sRef = spriteRenderer.GetComponent<SwatchColorReference>();
                if (sRef != null)
                {
                    Undo.RecordObject(sRef, "Clear Swatch Reference");
                    sRef.ClearSwatchReference();
                    EditorUtility.SetDirty(sRef);
                }
            }
        }

        /// <summary>
        /// Draws the most commonly used SpriteRenderer properties above the swatch section.
        /// 
        /// WHY: Sprite and Color are the primary properties artists interact with in 2D workflows.
        /// Placing them prominently at the top provides immediate access to the most important controls,
        /// improving workflow efficiency by reducing scrolling in the inspector.
        /// </summary>
        void DrawSpriteRendererPropertiesAbove()
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
        void DrawSpriteRendererPropertiesBelow()
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
        /// Loads the ColorPalette from Resources, creating a default one if none exists.
        /// 
        /// WHY: The swatch system requires a palette to function. Auto-creation provides a seamless 
        /// first-time experience - users don't need to manually set up infrastructure before using swatches.
        /// The default white swatch serves as a neutral starting point that works with any sprite.
        /// 
        /// Uses Resources folder for easy runtime access and to ensure the palette persists across scenes.
        /// </summary>
        void LoadColorPalette()
        {
            colorPalette = Resources.Load<ColorPalette>("ColorPalette");

            if (colorPalette == null)
            {
                // Create a new ColorPalette asset with sensible defaults
                colorPalette = CreateInstance<ColorPalette>();
                colorPalette.colors = new Color[] { Color.white }; // Default to one white color

                string assetPath = SavePalletteAssetToResources();

                // Debug.Log($"Created ColorPalette asset at: {assetPath}");
            }

            string SavePalletteAssetToResources()
            {
                // Create Resources folder if it doesn't exist
                string resourcesPath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                // Save it as an asset in the Resources folder
                string assetPath = "Assets/Resources/ColorPalette.asset";
                AssetDatabase.CreateAsset(colorPalette, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return assetPath;
            }
        }
    }

    private void DrawSingleSwatchButton(int index, SwatchColorReference swatchRef)
    {
        Color c = colorPalette.colors[index];
        // Allocate space for the swatch button with padding
        Rect rect = GUILayoutUtility.GetRect(boxSize + highlightPadding, boxSize + highlightPadding, GUILayout.Width(boxSize + highlightPadding), GUILayout.Height(boxSize + highlightPadding));

        // Center the actual swatch within the allocated space
        Rect swatchRect = new(rect.x + highlightPadding / 2, rect.y + highlightPadding / 2, boxSize, boxSize);

        // Highlight current swatch with a border
        bool isCurrentSwatch = swatchRef != null && swatchRef.GetSwatchIndex() == index;
        if (isCurrentSwatch)
        {
            // Highlight the current swatch
            EditorGUI.DrawRect(rect, Color.white);
        }

        EditorGUI.DrawRect(swatchRect, c);

        ProcessSwatchMouseEvents(index, swatchRect);



        void ApplySwatchToTargets(int swatchIndex)
        {
            foreach (var t in targets)
            {
                SpriteRenderer spriteRenderer = (SpriteRenderer)t;
                SwatchColorReference targetSwatchRef = spriteRenderer.GetComponent<SwatchColorReference>();

                // Record the SpriteRenderer state before changes
                Undo.RecordObject(spriteRenderer, "Change Sprite Color");

                // Add SwatchColorReference component if it doesn't exist
                if (targetSwatchRef == null)
                {
                    targetSwatchRef = CreateSwatchColorReference(spriteRenderer);
                    // For new components, record the object after creation to capture initial state
                    Undo.RecordObject(targetSwatchRef, "Change Swatch Reference");
                }
                else
                {
                    // Record the existing component state before changes
                    Undo.RecordObject(targetSwatchRef, "Change Swatch Reference");
                }

                // Set the swatch index and apply color
                targetSwatchRef.SetSwatchIndexAndApplyColor(swatchIndex);
                UnityEditor.EditorUtility.SetDirty(targetSwatchRef);
            }
        }

        void OpenColorPicker(int swatchIndex)
        {
            Color currentColor = colorPalette.colors[swatchIndex];

            // Create a callback that will be triggered when color changes
            System.Action<Color> onColorChanged = newColor =>
            {
                Undo.RecordObject(colorPalette, "Change Swatch Color");
                colorPalette.colors[swatchIndex] = newColor;
                EditorUtility.SetDirty(colorPalette);
                AssetDatabase.SaveAssets();

                // Manually trigger the same logic as ColorPaletteEditor
                UpdateAllSwatchReferences();

                // Force repaint to update the swatch display
                Repaint();
            };

            // Open Unity's color picker using ColorField in a popup
            ColorPickerWindow.Show(currentColor, onColorChanged, $"Swatch {swatchIndex}");
        }

        void ShowSwatchContextMenu(int swatchIndex)
        {
            GenericMenu menu = new();

            menu.AddItem(new GUIContent("Edit Color"), false, () => OpenColorPicker(swatchIndex));
            menu.AddItem(new GUIContent("Copy Color"), false, () => CopyColorToClipboard(swatchIndex));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate Swatch"), false, () => DuplicateSwatch(swatchIndex));
            menu.AddItem(new GUIContent("Delete Swatch"), false, () => DeleteSwatch(swatchIndex));

            menu.ShowAsContext();
        }

        void CopyColorToClipboard(int swatchIndex)
        {
            Color color = colorPalette.colors[swatchIndex];
            string colorHex = ColorUtility.ToHtmlStringRGBA(color);
            EditorGUIUtility.systemCopyBuffer = $"#{colorHex}";
            // Debug.Log($"Copied color #{colorHex} to clipboard");
        }

        void DuplicateSwatch(int swatchIndex)
        {
            Undo.RecordObject(colorPalette, "Duplicate Swatch");

            Color colorToDuplicate = colorPalette.colors[swatchIndex];
            Color[] newColors = new Color[colorPalette.colors.Length + 1];

            // Copy existing colors
            for (int i = 0; i < colorPalette.colors.Length; i++)
            {
                newColors[i] = colorPalette.colors[i];
            }

            // Add duplicated color at the end
            newColors[newColors.Length - 1] = colorToDuplicate;

            colorPalette.colors = newColors;
            EditorUtility.SetDirty(colorPalette);
            AssetDatabase.SaveAssets();
        }

        void DeleteSwatch(int swatchIndex)
        {
            if (colorPalette.colors.Length <= 1)
            {
                EditorUtility.DisplayDialog("Cannot Delete", "Cannot delete the last remaining swatch.", "OK");
                return;
            }

            Undo.RecordObject(colorPalette, "Delete Swatch");

            Color[] newColors = new Color[colorPalette.colors.Length - 1];
            int newIndex = 0;

            for (int i = 0; i < colorPalette.colors.Length; i++)
            {
                if (i != swatchIndex)
                {
                    newColors[newIndex] = colorPalette.colors[i];
                    newIndex++;
                }
            }

            colorPalette.colors = newColors;
            EditorUtility.SetDirty(colorPalette);
            AssetDatabase.SaveAssets();

            // Update any SwatchColorReference components that might be affected
            UpdateSwatchReferencesAfterDeletion(swatchIndex);
        }

        void UpdateSwatchReferencesAfterDeletion(int deletedIndex)
        {
            // Find all SwatchColorReference components in the scene and update their indices
            SwatchColorReference[] allSwatchRefs = FindObjectsByType<SwatchColorReference>(FindObjectsSortMode.None);

            foreach (var swatchRef in allSwatchRefs)
            {
                int currentIndex = swatchRef.GetSwatchIndex();
                if (currentIndex > deletedIndex)
                {
                    // Shift index down by 1
                    Undo.RecordObject(swatchRef, "Update Swatch Index");
                    swatchRef.SetSwatchIndex(currentIndex - 1);
                    EditorUtility.SetDirty(swatchRef);
                }
                else if (currentIndex == deletedIndex)
                {
                    // Clear the reference since the swatch was deleted
                    Undo.RecordObject(swatchRef, "Clear Deleted Swatch Reference");
                    swatchRef.ClearSwatchReference();
                    EditorUtility.SetDirty(swatchRef);
                }
            }
        }

        void ProcessSwatchMouseEvents(int index, Rect swatchRect)
        {

            // Handle different mouse events
            Event currentEvent = Event.current;
            if (swatchRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (currentEvent.button == 0) // Left mouse button
                    {
                        if (currentEvent.clickCount == 1)
                        {
                            // Single left-click: Apply swatch
                            ApplySwatchToTargets(index);
                            currentEvent.Use();
                        }
                        else if (currentEvent.clickCount == 2)
                        {
                            // Double left-click: Open color picker
                            OpenColorPicker(index);
                            currentEvent.Use();
                        }
                    }
                    else if (currentEvent.button == 1) // Right mouse button
                    {
                        // Right-click: Show context menu
                        ShowSwatchContextMenu(index);
                        currentEvent.Use();
                    }
                }
            }
        }
    }

    private SwatchColorReference CreateSwatchColorReference(SpriteRenderer spriteRenderer)
    {
        SwatchColorReference targetSwatchRef = Undo.AddComponent<SwatchColorReference>(spriteRenderer.gameObject);
        SwatchRefs.Add(targetSwatchRef);
        return targetSwatchRef;
    }

    private void AutoAssignDefaultSwatch()
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
                    // Debug.Log($"Registered existing SwatchColorReference for {spriteRenderer.name}");
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

                // Debug.Log($"Auto-assigned swatch 0 to {spriteRenderer.name}");
            }
        }
    }

    public static void UpdateAllSwatchReferences()
    {
        // Lazy cleanup - remove nulls as we encounter them
        for (int i = SwatchRefs.Count - 1; i >= 0; i--)
        {
            var swatchRef = SwatchRefs[i];
            if (swatchRef == null)
            {
                SwatchRefs.RemoveAt(i);
                continue;
            }

            if (swatchRef.GetSwatchIndex() < 0) continue;

            swatchRef.UpdateColorFromPalette();
            EditorUtility.SetDirty(swatchRef);
        }
    }

    private void ManualSwatchUpdate()
    {
        if (!enableManualSwatchUpdate) return;

        foreach (var target in targets)
        {
            SpriteRenderer spriteRenderer = target as SpriteRenderer;
            if (spriteRenderer == null) continue;

            if (spriteRenderer.TryGetComponent<SwatchColorReference>(out var swatchRef))
            {
                SwatchColorReference[] foundRefs = swatchRef.ManualUpdateSwatchReferencesList();
                foreach (var reference in foundRefs)
                {
                    if (!SwatchRefs.Contains(reference))
                    {
                        SwatchRefs.Add(reference);
                        // // Debug.Log($"Registered existing SwatchColorReference for {spriteRenderer.name}");
                    }
                }
                UpdateAllSwatchReferences();
                return;
            }
        }
    }
}