using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramienta de editor para levantar automáticamente la escena "Laboratorio" de pruebas
// (Misión 2) con primitivas 2D generadas por código: piso, paredes, plataforma,
// caja empujable (Rigidbody2D dinámico) y moneda coleccionable (Collider2D trigger).
public static class LabSceneBuilder
{
    private const string SpritesFolder = "Assets/_Project/Sprites";
    private const string ScenesFolder = "Assets/_Project/Scenes";
    private const string SceneName = "Lab";
    private const string GroundLayerName = "Ground";

    [MenuItem("Lab/Build Test Room Scene")]
    public static void BuildScene()
    {
        EnsureGroundLayer();

        Sprite squareSprite = GetOrCreateSprite("square", GenerateSquareTexture);
        Sprite circleSprite = GetOrCreateSprite("circle", GenerateCircleTexture);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 1f, -10f);
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        // Piso
        CreateStaticPlatform("Ground", squareSprite, new Vector3(0f, -4f, 0f),
            new Vector3(16f, 1f, 1f), new Color(0.35f, 0.6f, 0.3f), groundLayer);

        // Paredes laterales
        CreateStaticPlatform("Wall_Left", squareSprite, new Vector3(-8.5f, 0f, 0f),
            new Vector3(1f, 10f, 1f), new Color(0.4f, 0.4f, 0.45f), groundLayer);
        CreateStaticPlatform("Wall_Right", squareSprite, new Vector3(8.5f, 0f, 0f),
            new Vector3(1f, 10f, 1f), new Color(0.4f, 0.4f, 0.45f), groundLayer);

        // Plataforma flotante (altura calculada para quedar dentro del alcance del salto:
        // con Gravity Scale 4.5 y Jump Force 14 la altura máxima de salto es ~2.2 unidades)
        CreateStaticPlatform("Platform_1", squareSprite, new Vector3(3.5f, -2.2f, 0f),
            new Vector3(3f, 0.5f, 1f), new Color(0.45f, 0.35f, 0.25f), groundLayer);

        // Jugador
        GameObject player = CreatePlayer(squareSprite, new Vector3(-5f, -2.8f, 0f), groundLayer);

        // Caja empujable (Rigidbody2D dinámico)
        CreatePushableBox(squareSprite, new Vector3(-1f, -3.3f, 0f));

        // Moneda coleccionable (Collider2D trigger)
        CreateCoin(circleSprite, new Vector3(3.5f, 0.1f, 0f));

        Directory.CreateDirectory(ScenesFolder);
        string scenePath = $"{ScenesFolder}/{SceneName}.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        EditorBuildSettingsScene[] buildScenes = { new EditorBuildSettingsScene(scenePath, true) };
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LabSceneBuilder] Escena '{SceneName}' generada en {scenePath}");
    }

    private static void EnsureGroundLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == GroundLayerName) return;
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = GroundLayerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    private static GameObject CreateStaticPlatform(string name, Sprite sprite, Vector3 position,
        Vector3 scale, Color color, int layer)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (layer >= 0) go.layer = layer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static GameObject CreatePlayer(Sprite sprite, Vector3 position, int groundLayer)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.9f, 0.85f, 0.2f);

        go.AddComponent<BoxCollider2D>();

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.gravityScale = 4.5f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(go.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.55f, 0f);

        PlayerController controller = go.AddComponent<PlayerController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        so.FindProperty("groundLayer").intValue = 1 << groundLayer;
        so.ApplyModifiedProperties();

        return go;
    }

    private static GameObject CreatePushableBox(Sprite sprite, Vector3 position)
    {
        GameObject go = new GameObject("PushableBox");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.75f, 0.4f, 0.2f);

        go.AddComponent<BoxCollider2D>();

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 2f;
        rb.gravityScale = 1f;
        rb.linearDamping = 0.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        return go;
    }

    private static GameObject CreateCoin(Sprite sprite, Vector3 position)
    {
        GameObject go = new GameObject("Coin");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.85f, 0.1f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        go.AddComponent<Collectible>();
        return go;
    }

    private static Sprite GetOrCreateSprite(string name, System.Func<Texture2D> generator)
    {
        string path = $"{SpritesFolder}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(SpritesFolder);
        Texture2D tex = generator();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D GenerateSquareTexture()
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateCircleTexture()
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                pixels[y * size + x] = dist <= radius
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
