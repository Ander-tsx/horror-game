using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildGhostVsJasonScene
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string PixelPath = "Assets/Art/UI/GhostVsJasonPixel.asset";
    private const string GhostSpritesPath = "Assets/Art/Characters/Ghost/ghost_face_sprites.png";
    private const string JasonSpritesPath = "Assets/Art/Characters/Jason/jason_spritesheet.png";

    [InitializeOnLoadMethod]
    private static void ScheduleInitialSceneBuild()
    {
        EditorApplication.delayCall += BuildSceneIfMissing;
    }

    [MenuItem("Tools/Build Ghost vs Jason Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Art/UI");
        EnsureTmpFont();

        Sprite floorSprite = GetOrCreatePixelSprite();
        Sprite[] ghostFrames = LoadSprites(GhostSpritesPath);
        Sprite[] jasonFrames = LoadSprites(JasonSpritesPath);
        Sprite background = LoadBackground();
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/background_music.mp3");
        AudioClip attackSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Player/player_attack.mp3");
        AudioClip deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Player/player_die.mp3");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCameraAndBackground(background);
        BuildFloor(floorSprite);

        GameObject jason = BuildFighter("Jason", new Vector3(3.5f, -3.85f, 0f), jasonFrames,
            new Color(0.86f, 0.88f, 0.92f), attackSound, true);
        GameObject ghost = BuildFighter("Ghost", new Vector3(-3.5f, -3.85f, 0f), ghostFrames,
            new Color(0.76f, 0.95f, 1f), attackSound, false);

        GameObject musicObject = new GameObject("Audio");
        AudioSource musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        AudioSource effectsSource = musicObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;

        UIManager uiManager = BuildUI();
        GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        Health jasonHealth = jason.GetComponent<Health>();
        Health ghostHealth = ghost.GetComponent<Health>();
        gameManager.jasonHealth = jasonHealth;
        gameManager.ghostHealth = ghostHealth;
        gameManager.uiManager = uiManager;
        gameManager.musicSource = musicSource;
        gameManager.effectsSource = effectsSource;
        gameManager.deathClip = deathSound;

        uiManager.jasonHealth = jasonHealth;
        uiManager.ghostHealth = ghostHealth;
        jasonHealth.uiManager = uiManager;
        ghostHealth.uiManager = uiManager;
        jasonHealth.gameManager = gameManager;
        ghostHealth.gameManager = gameManager;
        ghost.GetComponent<EnemyAI>().target = jason.transform;
        jason.GetComponent<PlayerController>().opponent = ghost.transform;

        AddToBuildSettings(ScenePath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("Escena Ghost vs Jason creada en " + ScenePath);
    }

    private static void BuildSceneIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildSceneIfMissing;
            return;
        }

        string sceneFile = System.IO.Path.Combine(Application.dataPath, "Scenes/Game.unity");
        if (System.IO.File.Exists(sceneFile))
            return;

        try
        {
            BuildScene();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static GameObject BuildFighter(string fighterName, Vector3 position, Sprite[] frames,
        Color tint, AudioClip attackSound, bool isPlayer)
    {
        GameObject root = new GameObject(fighterName);
        root.transform.position = position;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 2.15f);
        box.offset = new Vector2(0f, 1.075f);

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(root.transform, false);
        visualObject.transform.localScale = Vector3.one * 2.8f;
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = frames.Length > 0 ? frames[0] : GetOrCreatePixelSprite();
        renderer.color = tint;
        renderer.sortingOrder = 2;
        CenterSpriteAtFeet(renderer);

        SpriteFrameAnimator spriteAnimator = root.AddComponent<SpriteFrameAnimator>();
        spriteAnimator.targetRenderer = renderer;
        if (isPlayer)
        {
            spriteAnimator.idleFrames = TakeFrames(frames, 0, 6);
            spriteAnimator.walkFrames = TakeFrames(frames, 18, 6);
            spriteAnimator.attackFrames = TakeFrames(frames, 23, 12);
            spriteAnimator.deathFrames = TakeFrames(frames, 35, 3);
        }
        else
        {
            spriteAnimator.idleFrames = TakeFrames(frames, 0, 3);
            spriteAnimator.walkFrames = TakeFrames(frames, 8, 4);
            spriteAnimator.attackFrames = TakeFrames(frames, 13, 8);
            spriteAnimator.deathFrames = TakeFrames(frames, 29, 3);
        }

        AudioSource fighterAudio = root.AddComponent<AudioSource>();
        fighterAudio.playOnAwake = false;
        FighterCombat combat = root.AddComponent<FighterCombat>();
        combat.attackClip = attackSound;
        combat.animator = spriteAnimator;
        combat.attackReach = 1.25f;
        combat.hitRadius = 0.85f;
        Health health = root.AddComponent<Health>();
        health.visual = renderer;

        if (isPlayer)
        {
            PlayerController controller = root.AddComponent<PlayerController>();
            controller.combat = combat;
            controller.animator = spriteAnimator;
            controller.health = health;
            combat.attackCooldown = 0.48f;
            combat.SetFacingDirection(-1);
        }
        else
        {
            EnemyAI controller = root.AddComponent<EnemyAI>();
            controller.combat = combat;
            controller.animator = spriteAnimator;
            controller.health = health;
            controller.attackDistance = 2f;
            combat.attackCooldown = 0f;
            combat.SetFacingDirection(1);
        }

        string prefabPath = "Assets/Prefabs/" + fighterName + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
        return root;
    }

    private static UIManager BuildUI()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        UIManager manager = canvasObject.AddComponent<UIManager>();
        manager.ghostFill = CreateHealthBar(canvasObject.transform, "Ghost", false);
        manager.jasonFill = CreateHealthBar(canvasObject.transform, "Jason", true);
        manager.resultPanel = CreateResultPanel(canvasObject.transform, manager);
        return manager;
    }

    private static Image CreateHealthBar(Transform parent, string characterName, bool rightSide)
    {
        GameObject labelObject = CreateText(parent, characterName, characterName.ToUpper(), 32,
            new Vector2(rightSide ? 1f : 0f, 1f), new Vector2(rightSide ? -80f : 80f, -50f),
            new Vector2(500f, 44f), rightSide ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft,
            new Color(1f, 0.9f, 0.72f));
        labelObject.transform.SetAsLastSibling();

        GameObject background = CreateImage(parent, characterName + " Health Background",
            new Color(0.08f, 0.07f, 0.08f, 0.92f),
            new Vector2(rightSide ? 1f : 0f, 1f), new Vector2(rightSide ? -80f : 80f, -105f),
            new Vector2(500f, 28f));

        Image fill = CreateImage(background.transform, characterName + " Health Fill",
            rightSide ? new Color(0.85f, 0.25f, 0.18f) : new Color(0.28f, 0.78f, 0.88f),
            Vector2.zero, Vector2.zero, Vector2.zero).GetComponent<Image>();
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = rightSide ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        return fill;
    }

    private static GameObject CreateResultPanel(Transform parent, UIManager manager)
    {
        GameObject panel = CreateImage(parent, "Result Panel", new Color(0.02f, 0.025f, 0.035f, 0.88f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        panel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        GameObject result = CreateText(panel.transform, "Winner Text", "GHOST GANA", 84,
            new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(900f, 150f),
            TextAlignmentOptions.Center, new Color(1f, 0.87f, 0.65f));
        manager.resultText = result.GetComponent<TMP_Text>();

        GameObject buttonObject = CreateImage(panel.transform, "Restart Button",
            new Color(0.72f, 0.19f, 0.13f), new Vector2(0.5f, 0.38f), Vector2.zero,
            new Vector2(300f, 76f));
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.88f, 0.3f, 0.18f);
        button.colors = colors;
        GameObject buttonText = CreateText(buttonObject.transform, "Restart Label", "REINICIAR", 34,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 70f),
            TextAlignmentOptions.Center, Color.white);
        buttonText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        buttonText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        buttonText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        buttonText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        manager.restartButton = button;
        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateText(Transform parent, string objectName, string value, float fontSize,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, 1f - anchor.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.font = FindTmpFont();
        return textObject;
    }

    private static GameObject CreateImage(Transform parent, string objectName, Color color,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, 1f - anchor.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static void BuildCameraAndBackground(Sprite background)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.035f, 0.035f, 0.055f);

        if (background == null)
            return;

        GameObject backgroundObject = new GameObject("Halloween Background", typeof(SpriteRenderer));
        SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
        renderer.sprite = background;
        renderer.sortingOrder = -10;
        Vector2 spriteSize = background.bounds.size;
        float height = camera.orthographicSize * 2f;
        float width = height * 16f / 9f;
        float scale = Mathf.Max(width / spriteSize.x, height / spriteSize.y);
        backgroundObject.transform.localScale = Vector3.one * scale;
        backgroundObject.transform.position = Vector3.zero;
    }

    private static void BuildFloor(Sprite floorSprite)
    {
        GameObject floor = new GameObject("Ground", typeof(BoxCollider2D), typeof(SpriteRenderer));
        floor.transform.position = new Vector3(0f, -4.45f, 0f);
        BoxCollider2D collider = floor.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(18f, 0.45f);
        SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>();
        renderer.sprite = floorSprite;
        renderer.color = new Color(0.075f, 0.075f, 0.09f);
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(18f, 0.45f);
        renderer.sortingOrder = 0;
    }

    private static Sprite LoadBackground()
    {
        const string sourcePath = "Assets/Art/Backgrounds/halloween_background.jfif";
        const string path = "Assets/Art/UI/halloween_background.jpg";
        string sourceFile = System.IO.Path.Combine(Application.dataPath, "Art/Backgrounds/halloween_background.jfif");
        string destinationFile = System.IO.Path.Combine(Application.dataPath, "Art/UI/halloween_background.jpg");

        if (System.IO.File.Exists(sourceFile) && !System.IO.File.Exists(destinationFile))
        {
            // Unity no reconoce algunas extensiones JFIF como textura; JPG usa el mismo contenido JPEG.
            System.IO.File.Copy(sourceFile, destinationFile);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        if (!System.IO.File.Exists(destinationFile))
            Debug.LogWarning("No se encontró el fondo: " + sourcePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite[] LoadSprites(string path)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        // Los recortes existentes terminan en un índice numérico y se ordenan para reproducirlos.
        return assets.OfType<Sprite>()
            .OrderBy(sprite => GetFrameNumber(sprite.name))
            .ToArray();
    }

    private static int GetFrameNumber(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int frame)
            ? frame
            : int.MaxValue;
    }

    private static Sprite[] TakeFrames(Sprite[] frames, int start, int count)
    {
        if (frames == null || frames.Length == 0)
            return Array.Empty<Sprite>();

        if (start >= frames.Length)
            start = 0;
        int available = Mathf.Min(count, frames.Length - start);
        return frames.Skip(start).Take(Mathf.Max(1, available)).ToArray();
    }

    private static void CenterSpriteAtFeet(SpriteRenderer renderer)
    {
        Vector3 localPosition = renderer.transform.localPosition;
        renderer.transform.localPosition = new Vector3(-renderer.sprite.bounds.center.x, localPosition.y, localPosition.z);
    }

    private static Sprite GetOrCreatePixelSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        if (existing != null)
            return existing;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, PixelPath);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
    }

    private static void EnsureTmpFont()
    {
        if (FindTmpFont() != null)
            return;

        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        AssetDatabase.Refresh();
    }

    private static TMP_FontAsset FindTmpFont()
    {
        string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in fontGuids)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font != null)
                return font;
        }
        return null;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == scenePath))
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}