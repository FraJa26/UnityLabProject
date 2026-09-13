using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Arma automáticamente el nivel jugable de la Unidad 2 (Tilemap, cámara, animaciones,
// UI, audio, partículas y GameManager) para no depender de construirlo a mano en el
// Editor. Es un tool de Editor, no forma parte del build final del juego.
public static class LevelBuilder
{
    private const string SpritesFolder = "Assets/_Project/Sprites";
    private const string TilesFolder = "Assets/_Project/Tiles";
    private const string AnimFolder = "Assets/_Project/Animations";
    private const string PrefabsFolder = "Assets/_Project/Prefabs";
    private const string ScenesFolder = "Assets/_Project/Scenes";
    private const string AudioFolder = "Assets/_Project/Audio";
    private const string SceneName = "Level1";
    private const string GroundLayerName = "Ground";

    private static readonly List<(int x, int y)> groundTiles = new();
    private static readonly List<(int x, int y)> platformTiles = new();

    private enum CharPose { Idle, Run, Jump, Fall }

    [MenuItem("Lab/Build Level 1 Scene")]
    public static void BuildLevel()
    {
        EnsureGroundLayer();
        ImportAudioAssets();

        Sprite tileGroundSprite = GetOrCreateSprite("tile_ground", DrawGroundTile);
        Sprite tilePlatformSprite = GetOrCreateSprite("tile_platform", DrawPlatformTile);
        Sprite skySprite = GetOrCreateSprite("bg_sky", DrawSkyTexture, 64f, FilterMode.Bilinear);
        Sprite hillsSprite = GetOrCreateSprite("bg_hills", DrawHillsTexture, 64f, FilterMode.Bilinear);
        Sprite spikeSprite = GetOrCreateSprite("hazard_spike", DrawSpikeTexture);
        Sprite coinSprite = GetOrCreateSprite("circle", GenerateCircleTexture);
        Sprite boxSprite = GetOrCreateSprite("square", GenerateSquareTexture);
        Sprite flagSprite = GetOrCreateSprite("goal_flag", DrawFlagTexture);

        Sprite[] idleFrames = GetOrCreateFrameSet("player_idle", 2, i => DrawCharacter(CharPose.Idle, i));
        Sprite[] runFrames = GetOrCreateFrameSet("player_run", 4, i => DrawCharacter(CharPose.Run, i));
        Sprite[] jumpFrames = GetOrCreateFrameSet("player_jump", 1, i => DrawCharacter(CharPose.Jump, i));
        Sprite[] fallFrames = GetOrCreateFrameSet("player_fall", 1, i => DrawCharacter(CharPose.Fall, i));

        Tile groundTileAsset = GetOrCreateTile("Tile_Ground", tileGroundSprite);
        Tile platformTileAsset = GetOrCreateTile("Tile_Platform", tilePlatformSprite);

        AnimatorController controller = BuildAnimatorController(idleFrames, runFrames, jumpFrames, fallFrames);
        GameObject coinSparkPrefab = BuildCoinSparklePrefab();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        BuildLevelLayout();

        Tilemap tilemap = CreateTilemap(groundLayer);
        foreach (var (x, y) in groundTiles) tilemap.SetTile(new Vector3Int(x, y, 0), groundTileAsset);
        foreach (var (x, y) in platformTiles) tilemap.SetTile(new Vector3Int(x, y, 0), platformTileAsset);

        CreateBackground(skySprite, hillsSprite);

        Vector3 playerStart = new Vector3(0.5f, 1.4f, 0f);
        GameObject player = CreatePlayer(idleFrames[0], playerStart, groundLayer, controller);

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = playerStart;

        Camera cam = CreateCameraRig(player.transform, playerStart);

        CreatePushableBox(boxSprite, new Vector3(45f, 1.45f, 0f));
        CreateHazard(spikeSprite, new Vector3(16.5f, 1.3f, 0f));
        CreateHazard(spikeSprite, new Vector3(48.5f, 1.3f, 0f));

        var coinPositions = new (float x, float y)[]
        {
            (3f, 2.6f), (5f, 2.6f), (7f, 2.6f),
            (14f, 2.6f), (20f, 3.6f),
            (26f, 3.6f), (30f, 4.6f), (34f, 4.6f), (35f, 4.6f),
            (54f, 2.6f), (59.5f, 3.8f),
        };
        foreach (var (x, y) in coinPositions)
        {
            CreateCoin(coinSprite, new Vector3(x, y, 0f), coinSparkPrefab);
        }

        CreateGoal(flagSprite, new Vector3(59.5f, 4.5f, 0f));
        CreateVoidKillZone();

        (GameManager gameManager, TMP_Text[] texts) = CreateUI(player.transform, spawnPoint.transform);
        CreateAudioManager();

        Directory.CreateDirectory(ScenesFolder);
        string scenePath = $"{ScenesFolder}/{SceneName}.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != scenePath).ToList();
        buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LevelBuilder] Escena '{SceneName}' generada en {scenePath}");
    }

    // ---------------------------------------------------------------- layout

    private static void BuildLevelLayout()
    {
        groundTiles.Clear();
        platformTiles.Clear();

        FillGround(-2, 10, 0);
        FillGround(13, 17, 0);
        FillGround(18, 22, 1);

        AddPlatformRow(25, 27, 2);
        AddPlatformRow(29, 31, 3);
        AddPlatformRow(33, 36, 3);
        AddPlatformRow(38, 40, 1);

        FillGround(42, 50, 0);

        AddPlatformRow(53, 55, 1);
        AddPlatformRow(58, 61, 2);
    }

    private static void FillGround(int xStart, int xEnd, int topY)
    {
        for (int x = xStart; x <= xEnd; x++)
            for (int y = -2; y <= topY; y++)
                groundTiles.Add((x, y));
    }

    private static void AddPlatformRow(int xStart, int xEnd, int y)
    {
        for (int x = xStart; x <= xEnd; x++) platformTiles.Add((x, y));
    }

    // ---------------------------------------------------------------- tilemap

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

    private static Tilemap CreateTilemap(int groundLayer)
    {
        GameObject gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        GameObject tilemapGO = new GameObject("Tilemap_Ground");
        tilemapGO.transform.SetParent(gridGO.transform);
        tilemapGO.layer = groundLayer;

        Tilemap tilemap = tilemapGO.AddComponent<Tilemap>();
        tilemapGO.AddComponent<TilemapRenderer>();

        TilemapCollider2D tilemapCollider = tilemapGO.AddComponent<TilemapCollider2D>();
        tilemapCollider.usedByComposite = true;

        Rigidbody2D rb = tilemapGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        tilemapGO.AddComponent<CompositeCollider2D>();

        return tilemap;
    }

    private static Tile GetOrCreateTile(string name, Sprite sprite)
    {
        string path = $"{TilesFolder}/{name}.asset";
        Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(TilesFolder);
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    // ---------------------------------------------------------------- background

    private static void CreateBackground(Sprite sky, Sprite hills)
    {
        GameObject skyGO = new GameObject("Background_Sky");
        SpriteRenderer skySr = skyGO.AddComponent<SpriteRenderer>();
        skySr.sprite = sky;
        skySr.sortingOrder = -20;
        skyGO.transform.position = new Vector3(30f, 5f, 10f);
        skyGO.transform.localScale = new Vector3(90f, 20f, 1f);

        GameObject hillsGO = new GameObject("Background_Hills");
        SpriteRenderer hillsSr = hillsGO.AddComponent<SpriteRenderer>();
        hillsSr.sprite = hills;
        hillsSr.sortingOrder = -10;
        hillsGO.transform.position = new Vector3(30f, 1f, 5f);
        hillsGO.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        ParallaxLayer parallax = hillsGO.AddComponent<ParallaxLayer>();
        SerializedObject so = new SerializedObject(parallax);
        so.FindProperty("parallaxFactor").floatValue = 0.35f;
        so.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------- player

    private static GameObject CreatePlayer(Sprite idleSprite, Vector3 position, int groundLayer, AnimatorController controller)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = idleSprite;
        sr.sortingOrder = 5;

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

        PlayerController playerController = go.AddComponent<PlayerController>();
        SerializedObject pcSO = new SerializedObject(playerController);
        pcSO.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        pcSO.FindProperty("groundLayer").intValue = 1 << groundLayer;
        pcSO.ApplyModifiedProperties();

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        go.AddComponent<PlayerAnimator>();

        return go;
    }

    // ---------------------------------------------------------------- camera

    private static Camera CreateCameraRig(Transform target, Vector3 playerStart)
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.6f, 0.75f, 0.85f);
        camGO.transform.position = new Vector3(playerStart.x, playerStart.y + 1.5f, -10f);

        CameraFollow follow = camGO.AddComponent<CameraFollow>();
        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("minX").floatValue = -6f;
        so.FindProperty("maxX").floatValue = 67f;
        so.ApplyModifiedProperties();

        return cam;
    }

    // ---------------------------------------------------------------- gameplay objects

    private static void CreatePushableBox(Sprite sprite, Vector3 position)
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
    }

    private static void CreateHazard(Sprite sprite, Vector3 position)
    {
        GameObject go = new GameObject("Spike");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.9f, 0.5f);
        col.offset = new Vector2(0f, -0.1f);

        go.AddComponent<Hazard>();
    }

    private static void CreateCoin(Sprite sprite, Vector3 position, GameObject sparklePrefab)
    {
        GameObject go = new GameObject("Coin");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.85f, 0.1f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        Coin coin = go.AddComponent<Coin>();
        SerializedObject so = new SerializedObject(coin);
        so.FindProperty("efectoRecogida").objectReferenceValue = sparklePrefab;
        so.ApplyModifiedProperties();
    }

    private static void CreateGoal(Sprite sprite, Vector3 position)
    {
        GameObject go = new GameObject("Goal");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 3;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 3f);

        go.AddComponent<Goal>();
    }

    private static void CreateVoidKillZone()
    {
        GameObject go = new GameObject("VoidKillZone");
        go.transform.position = new Vector3(30f, -9f, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(80f, 4f);

        go.AddComponent<Hazard>();
    }

    // ---------------------------------------------------------------- audio

    private static void ImportAudioAssets()
    {
        string[] files = { "sfx_jump.wav", "sfx_coin.wav", "sfx_hurt.wav", "sfx_win.wav", "music_loop.wav" };
        foreach (string f in files)
        {
            string path = $"{AudioFolder}/{f}";
            if (File.Exists(path)) AssetDatabase.ImportAsset(path);
        }
    }

    private static void CreateAudioManager()
    {
        GameObject go = new GameObject("AudioManager");
        AudioManager manager = go.AddComponent<AudioManager>();

        AudioSource musicSource = go.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        AudioSource sfxSource = go.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/music_loop.wav");
        AudioClip jump = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_jump.wav");
        AudioClip coin = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_coin.wav");
        AudioClip hurt = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_hurt.wav");
        AudioClip win = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_win.wav");

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("musicSource").objectReferenceValue = musicSource;
        so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
        so.FindProperty("musicClip").objectReferenceValue = music;
        so.FindProperty("jumpClip").objectReferenceValue = jump;
        so.FindProperty("coinClip").objectReferenceValue = coin;
        so.FindProperty("hurtClip").objectReferenceValue = hurt;
        so.FindProperty("winClip").objectReferenceValue = win;
        so.ApplyModifiedProperties();
    }

    private static GameObject BuildCoinSparklePrefab()
    {
        string prefabPath = $"{PrefabsFolder}/CoinSparkleFX.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        GameObject go = new GameObject("CoinSparkleFX");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.4f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.85f, 0.2f);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        Directory.CreateDirectory(PrefabsFolder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------------------------------------------------------------- UI

    private static (GameManager, TMP_Text[]) CreateUI(Transform player, Transform spawnPoint)
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        TMP_Text textoMonedas = CreateText(canvasGO.transform, "TextoMonedas", "Monedas: 0",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -30f), new Vector2(220f, 40f), 28, TextAlignmentOptions.Left);
        TMP_Text textoPuntos = CreateText(canvasGO.transform, "TextoPuntos", "Puntos: 0",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -66f), new Vector2(220f, 40f), 24, TextAlignmentOptions.Left);
        TMP_Text textoVidas = CreateText(canvasGO.transform, "TextoVidas", "Vidas: 3",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -30f), new Vector2(220f, 40f), 28, TextAlignmentOptions.Right);
        TMP_Text textoTiempo = CreateText(canvasGO.transform, "TextoTiempo", "Tiempo: 00:00",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -66f), new Vector2(220f, 40f), 24, TextAlignmentOptions.Right);

        GameObject panelGameOver = CreatePanel(canvasGO.transform, "PanelGameOver", "GAME OVER");
        GameObject panelVictoria = CreatePanel(canvasGO.transform, "PanelVictoria", "¡NIVEL COMPLETADO!");

        GameObject gmGO = new GameObject("GameManager");
        GameManager gameManager = gmGO.AddComponent<GameManager>();

        SerializedObject gmSO = new SerializedObject(gameManager);
        gmSO.FindProperty("textoMonedas").objectReferenceValue = textoMonedas;
        gmSO.FindProperty("textoPuntos").objectReferenceValue = textoPuntos;
        gmSO.FindProperty("textoTiempo").objectReferenceValue = textoTiempo;
        gmSO.FindProperty("textoVidas").objectReferenceValue = textoVidas;
        gmSO.FindProperty("panelGameOver").objectReferenceValue = panelGameOver;
        gmSO.FindProperty("panelVictoria").objectReferenceValue = panelVictoria;
        gmSO.FindProperty("player").objectReferenceValue = player;
        gmSO.FindProperty("puntoDeInicio").objectReferenceValue = spawnPoint;
        gmSO.ApplyModifiedProperties();

        WireRestartButton(panelGameOver, gameManager);
        WireRestartButton(panelVictoria, gameManager);

        panelGameOver.SetActive(false);
        panelVictoria.SetActive(false);

        return (gameManager, new[] { textoMonedas, textoPuntos, textoVidas, textoTiempo });
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    private static GameObject CreatePanel(Transform parent, string name, string title)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        CreateText(panel.transform, "Title", title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 40f), new Vector2(700f, 100f), 48, TextAlignmentOptions.Center);

        GameObject buttonGO = new GameObject("BotonReiniciar", typeof(RectTransform));
        buttonGO.transform.SetParent(panel.transform, false);
        RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0f, -40f);
        btnRt.sizeDelta = new Vector2(220f, 56f);
        Image btnImg = buttonGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.55f, 0.25f);
        buttonGO.AddComponent<Button>();
        CreateText(buttonGO.transform, "Label", "Reiniciar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 26, TextAlignmentOptions.Center);

        return panel;
    }

    private static void WireRestartButton(GameObject panel, GameManager gameManager)
    {
        Button button = panel.transform.Find("BotonReiniciar").GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, gameManager.ReiniciarNivel);
    }

    // ---------------------------------------------------------------- Animator

    private static AnimatorController BuildAnimatorController(Sprite[] idle, Sprite[] run, Sprite[] jump, Sprite[] fall)
    {
        string path = $"{AnimFolder}/PlayerAnimator.controller";
        Directory.CreateDirectory(AnimFolder);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        if (!controller.parameters.Any(p => p.name == "Speed"))
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (!controller.parameters.Any(p => p.name == "IsGrounded"))
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        if (!controller.parameters.Any(p => p.name == "VerticalVelocity"))
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        foreach (var childState in sm.states.ToArray()) sm.RemoveState(childState.state);

        AnimationClip idleClip = CreateClip("Anim_Idle", idle, 4f, true);
        AnimationClip runClip = CreateClip("Anim_Run", run, 10f, true);
        AnimationClip jumpClip = CreateClip("Anim_Jump", jump, 1f, false);
        AnimationClip fallClip = CreateClip("Anim_Fall", fall, 1f, false);

        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;
        AnimatorState jumpState = sm.AddState("Jump");
        jumpState.motion = jumpClip;
        AnimatorState fallState = sm.AddState("Fall");
        fallState.motion = fallClip;

        sm.defaultState = idleState;

        AddTransition(idleState, runState, ("Speed", AnimatorConditionMode.Greater, 0.1f));
        AddTransition(runState, idleState, ("Speed", AnimatorConditionMode.Less, 0.1f));

        AddTransition(idleState, jumpState, ("IsGrounded", AnimatorConditionMode.IfNot, 0f), ("VerticalVelocity", AnimatorConditionMode.Greater, 0.05f));
        AddTransition(runState, jumpState, ("IsGrounded", AnimatorConditionMode.IfNot, 0f), ("VerticalVelocity", AnimatorConditionMode.Greater, 0.05f));

        AddTransition(idleState, fallState, ("IsGrounded", AnimatorConditionMode.IfNot, 0f), ("VerticalVelocity", AnimatorConditionMode.Less, 0.05f));
        AddTransition(runState, fallState, ("IsGrounded", AnimatorConditionMode.IfNot, 0f), ("VerticalVelocity", AnimatorConditionMode.Less, 0.05f));

        AddTransition(jumpState, fallState, ("VerticalVelocity", AnimatorConditionMode.Less, 0.05f));

        AddTransition(jumpState, idleState, ("IsGrounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Less, 0.1f));
        AddTransition(jumpState, runState, ("IsGrounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Greater, 0.1f));
        AddTransition(fallState, idleState, ("IsGrounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Less, 0.1f));
        AddTransition(fallState, runState, ("IsGrounded", AnimatorConditionMode.If, 0f), ("Speed", AnimatorConditionMode.Greater, 0.1f));

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddTransition(AnimatorState from, AnimatorState to,
        params (string param, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0.05f;
        t.exitTime = 0f;
        foreach (var c in conditions) t.AddCondition(c.mode, c.threshold, c.param);
    }

    private static AnimationClip CreateClip(string name, Sprite[] frames, float frameRate, bool loop)
    {
        string path = $"{AnimFolder}/{name}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        AnimationClip clip = existing != null ? existing : new AnimationClip();
        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (existing == null) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);

        return clip;
    }

    // ---------------------------------------------------------------- sprite/texture generation

    private static Sprite GetOrCreateSprite(string name, System.Func<Texture2D> generator,
        float pixelsPerUnit = 64f, FilterMode filterMode = FilterMode.Point)
    {
        string path = $"{SpritesFolder}/{name}.png";
        Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existingSprite != null) return existingSprite;

        Directory.CreateDirectory(SpritesFolder);
        Texture2D tex = generator();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite[] GetOrCreateFrameSet(string baseName, int count, System.Func<int, Texture2D> generator)
    {
        var sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            sprites[i] = GetOrCreateSprite($"{baseName}_{idx}", () => generator(idx));
        }
        return sprites;
    }

    private static void FillRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color color)
    {
        Color32 c = color;
        for (int y = Mathf.Max(0, y0); y <= Mathf.Min(size - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(size - 1, x1); x++)
                pixels[y * size + x] = c;
    }

    private static void FillCircle(Color32[] pixels, int size, int cx, int cy, int r, Color color)
    {
        Color32 c = color;
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(size - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(size - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) pixels[y * size + x] = c;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    private static int[] RunPattern = { 4, 0, -4, 0 };

    private static Texture2D DrawCharacter(CharPose pose, int frame)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        Color body = new Color(0.85f, 0.55f, 0.2f);
        Color head = new Color(0.95f, 0.8f, 0.55f);
        Color limb = new Color(0.5f, 0.32f, 0.12f);

        int bob = (pose == CharPose.Idle && frame == 1) ? 2 : 0;

        FillRect(pixels, size, 20, 22 + bob, 44, 46 + bob, body);
        FillCircle(pixels, size, 32, 52 + bob, 10, head);

        if (pose == CharPose.Jump)
        {
            FillRect(pixels, size, 21, 14, 29, 24, limb);
            FillRect(pixels, size, 35, 14, 43, 24, limb);
        }
        else if (pose == CharPose.Fall)
        {
            FillRect(pixels, size, 14, 6, 25, 22, limb);
            FillRect(pixels, size, 39, 6, 50, 22, limb);
        }
        else
        {
            int off = pose == CharPose.Run ? RunPattern[frame % RunPattern.Length] : 0;
            FillRect(pixels, size, 22, 10 + off, 30, 22, limb);
            FillRect(pixels, size, 34, 10 - off, 42, 22, limb);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawGroundTile()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        Color grass = new Color(0.36f, 0.62f, 0.32f);
        Color dirt = new Color(0.42f, 0.30f, 0.18f);
        Color dirtDark = new Color(0.34f, 0.23f, 0.13f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = y >= size - 10 ? (Color32)grass : ((x + y) % 17 == 0 ? (Color32)dirtDark : (Color32)dirt);

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawPlatformTile()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        Color wood = new Color(0.55f, 0.4f, 0.22f);
        Color woodDark = new Color(0.45f, 0.32f, 0.16f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = (y % 16 < 2) ? (Color32)woodDark : (Color32)wood;

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawSkyTexture()
    {
        const int w = 4, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        Color top = new Color(0.35f, 0.55f, 0.75f);
        Color bottom = new Color(0.78f, 0.87f, 0.87f);

        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(h - 1));
            for (int x = 0; x < w; x++) pixels[y * w + x] = c;
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawHillsTexture()
    {
        const int w = 256, h = 80;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        Color hill = new Color(0.3f, 0.45f, 0.4f, 0.9f);
        Vector2[] centers = { new(30, 8), new(90, 18), new(150, 4), new(210, 14) };
        float[] radii = { 45, 60, 50, 55 };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                for (int i = 0; i < centers.Length; i++)
                {
                    if (Vector2.Distance(new Vector2(x, y), centers[i]) <= radii[i]) { c = hill; break; }
                }
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawSpikeTexture()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        Color spike = new Color(0.75f, 0.2f, 0.2f);
        Vector2[][] tris =
        {
            new[] { new Vector2(2, 0), new Vector2(22, 0), new Vector2(12, 40) },
            new[] { new Vector2(22, 0), new Vector2(42, 0), new Vector2(32, 40) },
            new[] { new Vector2(42, 0), new Vector2(62, 0), new Vector2(52, 40) },
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                foreach (var t in tris)
                {
                    if (PointInTriangle(p, t[0], t[1], t[2])) { c = spike; break; }
                }
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawFlagTexture()
    {
        const int w = 64, h = 192;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

        Color pole = new Color(0.6f, 0.6f, 0.6f);
        Color cloth = new Color(0.85f, 0.2f, 0.2f);

        for (int y = 0; y < h; y++)
            for (int x = 28; x < 34; x++) pixels[y * w + x] = pole;

        for (int y = h - 50; y < h - 10; y++)
        {
            int rowFromTop = y - (h - 50);
            int width = Mathf.Max(0, 26 - rowFromTop / 2);
            for (int x = 34; x < 34 + width && x < w; x++) pixels[y * w + x] = cloth;
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
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
                pixels[y * size + x] = dist <= radius ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
