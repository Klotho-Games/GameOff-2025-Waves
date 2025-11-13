using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Automatic floor generation using tile assets
///  with a possibility to save the map to prefabs
/// 
/// Technical Design:
/// - Creates a random map of tiles,
/// - Iterates to create more natural patterns,
/// - Randomly places decorative elements
///  satisfying their spawn conditions,
/// - Allows to save the generated map as a prefab for reuse.
/// </summary>
[System.Serializable]
public class DecorativeElement
{
    public Sprite sprite;
    public int minTileValue;
    public int maxTileValue;
    public int minSpacing;

    public DecorativeElement(Sprite sprite = null, int minTileValue = 0, int maxTileValue = 4, int minSpacing = 0)
    {
        this.sprite = sprite;
        this.minTileValue = minTileValue;
        this.maxTileValue = maxTileValue;
    }
}

[System.Serializable]
public class FloorGenerator
{
    [Header("Setup")]
    [Tooltip("Folder path in Resources that the generation can be saved (e.g., 'FloorMaps')")]
    public string mapFolderName = "FloorMaps";

    [Tooltip("Number of floor tile sprites to use for floor generation")]
    public int floorTileSpritesCount = 5;
    [Tooltip("List of floor tile sprites to use for floor generation")]
    public List<Sprite> floorTileSprites;

    [Tooltip("Number of decorative element sprites to randomly place on the floor")]
    public int decorativeElementSpritesCount = 3;
    [Tooltip("List of decorative element sprites to randomly place on the floor")]
    public List<DecorativeElement> decorativeElementSprites;

    [Tooltip("Number of iterations to refine the floor map for more natural patterns")]
    public int iterations = 250;

    public int numberOfDecorativeElementsToPlace = 20;

    private int mapIndex = 0;

    private GameObject floorParentObject;
    private GameObject decorationParentObject;
    private GameObject tileParentObject;
    private int decorationIndex = 0;

    private const int mapHeight = 42;
    private const int mapWidth = 72;
    private int[,] floorMap;

    private const int cornerNum = 3*16;
    private const int edgeNum = 5*(4*(mapWidth-4) + 4*(mapHeight-4));
    private const int centerNum = 8*(mapWidth-4)*(mapHeight-4);

    private const int zMapPosition = 1000;

    public bool GenerateFloorMap()
    {
        floorMap = new int[mapWidth, mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int rand = Random.Range(0, 2);
                floorMap[x, y] = rand == 0 ? Random.Range(0, floorTileSpritesCount/3) : Random.Range(Mathf.CeilToInt(floorTileSpritesCount*2/3f), floorTileSpritesCount);
            }
        }

        if (tileParentObject != null)
        {
            Object.DestroyImmediate(tileParentObject);
        }

        return ContinueGeneration();
    }

    private (int, int) GetControlledRandomTilePosition()
    {
        int rand = Random.Range(0, 1 + cornerNum + edgeNum + centerNum);
        switch (rand)
        {
            case < cornerNum:
                return Corner();

            case >= cornerNum and < cornerNum + edgeNum:
                return Edge();
            case >= cornerNum + edgeNum and <= cornerNum + edgeNum + centerNum:
                return Center();
        }
        Debug.LogError("GetControlledRandomTilePosition: Fallback reached!");
        return (0, 0); // Fallback, should not reach here

        (int, int) Corner()
        {
            int x = Random.Range(0, 4);
            int y = Random.Range(0, 4);
            if (x >= 2)
                x = mapWidth - (4 - x);
            if (y >= 2)
                y = mapHeight - (4 - y);
            return (x, y);
        }
        (int, int) Edge()
        {
            int side = Random.Range(0, 4);
            int x = 0;
            int y = 0;
            switch (side)
            {
                case 0: // Top edge
                    x = Random.Range(2, mapWidth - 2);
                    y = Random.Range(0, 2);
                    break;
                case 1: // Bottom edge
                    x = Random.Range(2, mapWidth - 2);
                    y = mapHeight - 1 - Random.Range(0, 2);
                    break;
                case 2: // Left edge
                    x = Random.Range(0, 2);
                    y = Random.Range(2, mapHeight - 2);
                    break;
                case 3: // Right edge
                    x = mapWidth - 1 - Random.Range(0, 2);
                    y = Random.Range(2, mapHeight - 2);
                    break;
            }
            return (x, y);
        }
        (int, int) Center()
        {
            int x = Random.Range(2, mapWidth - 2);
            int y = Random.Range(2, mapHeight - 2);
            return (x, y);
        }
    }

    public bool ContinueGeneration()
    {
        if (decorationParentObject != null)
        {
            Object.DestroyImmediate(decorationParentObject);
        }

        for (int i = 0; i < iterations; i++)
        {
            int x, y;
            (x, y) = GetControlledRandomTilePosition();
            
            List<int> dirs = new() { 0, 1, 2, 3, 4, 5, 6, 7 };

            int dir;
            int x1 = x;
            int y1 = y;

            do
            {
                if (dirs.Count == 0)
                {
                    Debug.LogWarning("ContinueGeneration: No valid directions found for tile at (" + x + ", " + y + ")");
                    break;
                }

                int dirIndex = Random.Range(0, dirs.Count);
                dir = dirs[dirIndex];

                x1 = x;
                y1 = y;

                switch (dir)
                {
                    case 0: // Up
                        y1 += 2;
                        break;
                    case 1: // Down
                        y1 -= 2;
                        break;
                    case 2: // Left
                        x1 -= 2;
                        break;
                    case 3: // Right
                        x1 += 2;
                        break;
                    case 4: // Up-Left
                        x1 -= 2;
                        y1 += 2;
                        break;
                    case 5: // Up-Right
                        x1 += 2;
                        y1 += 2;
                        break;
                    case 6: // Down-Left
                        x1 -= 2;
                        y1 -= 2;
                        break;
                    case 7: // Down-Right
                        x1 += 2;
                        y1 -= 2;
                        break;
                }

                if (SecondTileOutOfBounds(x1, y1))
                {
                    dirs.RemoveAt(dirIndex);
                }

            } while (SecondTileOutOfBounds(x1, y1) && dirs.Count > 0);

            if (dirs.Count == 0)
            {
                continue; // Skip this iteration if no valid direction was found
            }

            (int,int) middle = ((x + x1) / 2, (y + y1) / 2);
            int value = floorMap[x, y];
            int value1 = floorMap[x1, y1];
            int middleValue = floorMap[middle.Item1, middle.Item2];

            floorMap[middle.Item1, middle.Item2] = Mathf.RoundToInt((value + value1 + middleValue) / 3f);
        }

        return SetDecorativeElementsPositions();

        static bool SecondTileOutOfBounds(int x1, int y1)
        {
            if (x1 < 0 || x1 >= mapWidth || y1 < 0 || y1 >= mapHeight)
                return true;
            return false;
        }
    }
    
    private bool SetDecorativeElementsPositions()
    {
        if (numberOfDecorativeElementsToPlace <= 0 || numberOfDecorativeElementsToPlace > (mapWidth * mapHeight))
        {
            Debug.LogError("SetDecorativeElementsPositions: Invalid number of decorative elements to place. Placing none.");
            return true;
        }
        
        List<(int, int)> placedPositions = new();
        for (int i = 0, j = 0; i < numberOfDecorativeElementsToPlace && j < 2 * numberOfDecorativeElementsToPlace; i++, j++) // Limit attempts to avoid infinite loops
        {
            (int, int) tilePos = (Random.Range(0, mapWidth), Random.Range(0, mapHeight));
            if (placedPositions.Contains(tilePos))
            {
                i--;
                continue;
            }
            placedPositions.Add(tilePos);
            PlaceDecoration(tilePos);
        }

        InstantiateFloorMap();
        return true;

        void PlaceDecoration((int, int) tilePos)
        {
            int value = floorMap[tilePos.Item1, tilePos.Item2];
            List<DecorativeElement> suitableDecorations = new();
            foreach (var decor in decorativeElementSprites)
            {
                // Check tile range
                if (value < decor.minTileValue || value > decor.maxTileValue)
                    continue;
                
                suitableDecorations.Add(decor);
            }

            if (suitableDecorations.Count == 0)
                return;

            InstantiateDecorationAtTile(suitableDecorations[Random.Range(0, suitableDecorations.Count)].sprite, tilePos);
        }
    }

    private void InstantiateDecorationAtTile(Sprite decorSprite, (int, int) tilePos)
    {
        if (floorParentObject == null)
        {
            floorParentObject = new GameObject("GeneratedFloor");
            floorParentObject.transform.position = new Vector3(-17.75f, -10.25f, zMapPosition);
        }
        if (decorationParentObject == null)
        {
            decorationIndex = 0;
            decorationParentObject = new("Decoration");
            decorationParentObject.transform.parent = floorParentObject.transform;
            decorationParentObject.transform.localPosition = Vector3.zero;
        }

        GameObject decorObject = new("DecorativeElement[" + decorationIndex + "]");
        decorObject.transform.parent = decorationParentObject.transform;
        decorObject.transform.localPosition = new Vector3(tilePos.Item1 * 0.5f, tilePos.Item2 * 0.5f, 0);

        SpriteRenderer sr = decorObject.AddComponent<SpriteRenderer>();
        sr.sprite = decorSprite;
        sr.sortingOrder = 1;

        decorationIndex++;
    }

    private void InstantiateFloorMap()
    {
        if (floorParentObject == null)
        {
            floorParentObject = new GameObject("GeneratedFloor");
            floorParentObject.transform.position = new Vector3(-17.75f, -10.25f, zMapPosition);
        }
        if (tileParentObject == null)
        {
            tileParentObject = new("Tiles");
            tileParentObject.transform.parent = floorParentObject.transform;
            tileParentObject.transform.localPosition = Vector3.zero;
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                GameObject tileObject = new("FloorTile["+x+", "+y+"]");
                tileObject.transform.parent = tileParentObject.transform;
                tileObject.transform.localPosition = new Vector3(x * 0.5f, y * 0.5f, 0);

                SpriteRenderer sr = tileObject.AddComponent<SpriteRenderer>();
                sr.sprite = floorTileSprites[floorMap[x, y]];
                sr.sortingOrder = 0;
            }
        }
    }

    public bool SaveGeneratedMapAsTexture()
    {
        if (floorMap == null)
        {
            Debug.LogError("SaveGeneratedMapAsTexture: No floor map generated yet.");
            return false;
        }

        // Create a single sprite from the floor map
        Texture2D floorTexture = CreateFloorTexture();
        
        // Save the texture as an asset
        string texturePath = "Assets/Textures/" + mapFolderName;
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
        {
            AssetDatabase.CreateFolder("Assets", "Textures");
        }
        if (!AssetDatabase.IsValidFolder(texturePath))
        {
            AssetDatabase.CreateFolder("Assets/Textures", mapFolderName);
        }
        
        string textureFileName = texturePath + "/FloorTexture_" + mapIndex.ToString("D3") + ".png";
        byte[] pngData = floorTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(textureFileName, pngData);
        Object.DestroyImmediate(floorTexture);
        AssetDatabase.Refresh();
        
        // Set texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(textureFileName) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 32;
            importer.SaveAndReimport();
        }
        
        Debug.Log("SaveGeneratedMapAsTexture: Saved texture at " + textureFileName);
        mapIndex++;
        return true;
    }
    
    private Texture2D CreateFloorTexture()
    {
        // Find actual pixel size of sprites
        Sprite firstSprite = floorTileSprites[0];
        if (firstSprite == null || firstSprite.texture == null)
        {
            Debug.LogError("CreateFloorTexture: First sprite is null or has no texture!");
            return new Texture2D(1, 1);
        }
        
        // Check if textures are readable once before processing
        HashSet<Texture2D> checkedTextures = new HashSet<Texture2D>();
        foreach (var sprite in floorTileSprites)
        {
            if (sprite != null && sprite.texture != null && !checkedTextures.Contains(sprite.texture))
            {
                checkedTextures.Add(sprite.texture);
                if (!sprite.texture.isReadable)
                {
                    Debug.LogError($"CreateFloorTexture: Sprite texture '{sprite.texture.name}' is not readable! Enable Read/Write in import settings for this texture.");
                    return new Texture2D(1, 1);
                }
            }
        }
        
        // Check decoration sprites too
        if (decorationParentObject != null)
        {
            foreach (Transform child in decorationParentObject.transform)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.texture != null && !checkedTextures.Contains(sr.sprite.texture))
                {
                    checkedTextures.Add(sr.sprite.texture);
                    if (!sr.sprite.texture.isReadable)
                    {
                        Debug.LogError($"CreateFloorTexture: Decoration sprite texture '{sr.sprite.texture.name}' is not readable! Enable Read/Write in import settings.");
                        return new Texture2D(1, 1);
                    }
                }
            }
        }
        
        int pixelsPerTile = Mathf.RoundToInt(firstSprite.textureRect.width);
        int textureWidth = mapWidth * pixelsPerTile;
        int textureHeight = mapHeight * pixelsPerTile;
        
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        
        // Fill with transparent pixels first
        Color[] clearPixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        texture.SetPixels(clearPixels);
        
        // Draw each tile onto the texture
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Sprite tileSprite = floorTileSprites[floorMap[x, y]];
                if (tileSprite != null && tileSprite.texture != null)
                {
                    // Get the sprite's texture data
                    Texture2D spriteTexture = tileSprite.texture;
                    Rect spriteRect = tileSprite.textureRect;
                    
                    // Copy pixels from sprite to texture at the correct position
                    Color[] pixels = spriteTexture.GetPixels(
                        (int)spriteRect.x, 
                        (int)spriteRect.y, 
                        (int)spriteRect.width, 
                        (int)spriteRect.height
                    );
                    
                    // Resize pixels to fit pixelsPerTile size if needed
                    int srcWidth = (int)spriteRect.width;
                    int srcHeight = (int)spriteRect.height;
                    if (srcWidth != pixelsPerTile || srcHeight != pixelsPerTile)
                    {
                        pixels = ResizePixels(pixels, srcWidth, srcHeight, pixelsPerTile, pixelsPerTile);
                    }
                    
                    texture.SetPixels(x * pixelsPerTile, y * pixelsPerTile, pixelsPerTile, pixelsPerTile, pixels);
                }
            }
        }
        
        // Draw decorations on top
        if (decorationParentObject != null)
        {
            foreach (Transform child in decorationParentObject.transform)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                {
                    Sprite decorSprite = sr.sprite;
                    Texture2D decorTexture = decorSprite.texture;
                    Rect decorRect = decorSprite.textureRect;
                    
                    // Get decoration position in tile coordinates
                    Vector3 localPos = child.localPosition;
                    int decorPixelX = Mathf.RoundToInt(localPos.x * 2 * pixelsPerTile); // *2 because tiles are 0.5 units
                    int decorPixelY = Mathf.RoundToInt(localPos.y * 2 * pixelsPerTile);
                    
                    // Get decoration pixels
                    Color[] decorPixels = decorTexture.GetPixels(
                        (int)decorRect.x,
                        (int)decorRect.y,
                        (int)decorRect.width,
                        (int)decorRect.height
                    );
                    
                    int decorWidth = (int)decorRect.width;
                    int decorHeight = (int)decorRect.height;
                    
                    // Blend decoration onto texture
                    for (int dy = 0; dy < decorHeight; dy++)
                    {
                        for (int dx = 0; dx < decorWidth; dx++)
                        {
                            int texX = decorPixelX + dx;
                            int texY = decorPixelY + dy;
                            
                            if (texX >= 0 && texX < textureWidth && texY >= 0 && texY < textureHeight)
                            {
                                Color decorColor = decorPixels[dy * decorWidth + dx];
                                if (decorColor.a > 0)
                                {
                                    Color baseColor = texture.GetPixel(texX, texY);
                                    // Alpha blend
                                    Color blended = Color.Lerp(baseColor, decorColor, decorColor.a);
                                    texture.SetPixel(texX, texY, blended);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private Color[] ResizePixels(Color[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        Color[] result = new Color[dstWidth * dstHeight];
        float xRatio = (float)srcWidth / dstWidth;
        float yRatio = (float)srcHeight / dstHeight;
        
        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                int srcX = Mathf.FloorToInt(x * xRatio);
                int srcY = Mathf.FloorToInt(y * yRatio);
                result[y * dstWidth + x] = source[srcY * srcWidth + srcX];
            }
        }
        
        return result;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor utility window for generating a new floor map
/// </summary>
public class FloorGeneratorLoaderWindow : EditorWindow
{
    [SerializeField] private FloorGenerator floorGenerator = new();
    private Vector2 scrollPosition;
    private bool shouldGenerateNew = false;
    private bool shouldContinue = false;

    [MenuItem("Tools/Floor Generator")]
    public static void ShowWindow()
    {
        FloorGeneratorLoaderWindow window = GetWindow<FloorGeneratorLoaderWindow>("Floor Generator");
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Floor Generator Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Map Folder Name
        floorGenerator.mapFolderName = EditorGUILayout.TextField("Map Folder Name", floorGenerator.mapFolderName);
        EditorGUILayout.Space();

        // Floor Tile Sprites
        GUILayout.Label("Floor Tiles", EditorStyles.boldLabel);
        floorGenerator.floorTileSpritesCount = EditorGUILayout.IntField("Floor Tile Sprites Count", floorGenerator.floorTileSpritesCount);
        
        if (floorGenerator.floorTileSprites == null)
            floorGenerator.floorTileSprites = new List<Sprite>();

        // Adjust list size
        while (floorGenerator.floorTileSprites.Count < floorGenerator.floorTileSpritesCount)
            floorGenerator.floorTileSprites.Add(null);
        while (floorGenerator.floorTileSprites.Count > floorGenerator.floorTileSpritesCount)
            floorGenerator.floorTileSprites.RemoveAt(floorGenerator.floorTileSprites.Count - 1);

        for (int i = 0; i < floorGenerator.floorTileSprites.Count; i++)
        {
            floorGenerator.floorTileSprites[i] = (Sprite)EditorGUILayout.ObjectField($"Tile Sprite {i}", floorGenerator.floorTileSprites[i], typeof(Sprite), false);
        }
        EditorGUILayout.Space();

        // Decorative Elements
        GUILayout.Label("Decorative Elements", EditorStyles.boldLabel);
        floorGenerator.decorativeElementSpritesCount = EditorGUILayout.IntField("Decorative Sprites Count", floorGenerator.decorativeElementSpritesCount);
        
        if (floorGenerator.decorativeElementSprites == null)
            floorGenerator.decorativeElementSprites = new List<DecorativeElement>();

        // Adjust list size
        while (floorGenerator.decorativeElementSprites.Count < floorGenerator.decorativeElementSpritesCount)
            floorGenerator.decorativeElementSprites.Add(new DecorativeElement(null, 0, floorGenerator.floorTileSpritesCount - 1, 0));
        while (floorGenerator.decorativeElementSprites.Count > floorGenerator.decorativeElementSpritesCount)
            floorGenerator.decorativeElementSprites.RemoveAt(floorGenerator.decorativeElementSprites.Count - 1);

        for (int i = 0; i < floorGenerator.decorativeElementSprites.Count; i++)
        {
            var decor = floorGenerator.decorativeElementSprites[i];
            EditorGUILayout.LabelField($"Decoration {i}", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            
            decor.sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", decor.sprite, typeof(Sprite), false);
            decor.minTileValue = EditorGUILayout.IntField("Min Tile Value", decor.minTileValue);
            decor.maxTileValue = EditorGUILayout.IntField("Max Tile Value", decor.maxTileValue);
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        EditorGUILayout.Space();

        // Generation Settings
        GUILayout.Label("Generation Settings", EditorStyles.boldLabel);
        floorGenerator.iterations = EditorGUILayout.IntField("Iterations", floorGenerator.iterations);
        floorGenerator.numberOfDecorativeElementsToPlace = EditorGUILayout.IntField("Decorative Elements", floorGenerator.numberOfDecorativeElementsToPlace);
        EditorGUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate New", GUILayout.Height(30)))
        {
            if (ValidateSettings())
            {
                shouldGenerateNew = true;
            }
        }
        if (GUILayout.Button("Continue Previous Generation", GUILayout.Height(30)))
        {
            if (ValidateSettings())
            {
                shouldContinue = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save as Texture", GUILayout.Height(30)))
        {
            if (ValidateSettings())
            {
                floorGenerator.SaveGeneratedMapAsTexture();
            }
        }

        EditorGUILayout.EndScrollView();

        // Process deferred actions after GUI layout is complete
        if (Event.current.type == EventType.Repaint)
        {
            if (shouldGenerateNew)
            {
                shouldGenerateNew = false;
                floorGenerator.GenerateFloorMap();
                Debug.Log("Floor map generation started.");
                Repaint();
            }
            else if (shouldContinue)
            {
                shouldContinue = false;
                floorGenerator.ContinueGeneration();
                Debug.Log("Continued floor map generation.");
                Repaint();
            }
        }
    }

    private bool ValidateSettings()
    {
        if (floorGenerator.floorTileSprites == null || floorGenerator.floorTileSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Error", "Please add at least one floor tile sprite.", "OK");
            return false;
        }

        foreach (var sprite in floorGenerator.floorTileSprites)
        {
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "All floor tile sprites must be assigned.", "OK");
                return false;
            }
        }

        return true;
    }
}
#endif