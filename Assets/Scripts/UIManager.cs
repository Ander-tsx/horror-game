using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Health ghostHealth;
    public Health jasonHealth;
    public Image ghostFill;
    public Image jasonFill;
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button restartButton;

    private TMP_Text ghostValueText;
    private TMP_Text jasonValueText;
    private float ghostTargetFill = 1f;
    private float jasonTargetFill = 1f;
    private static Sprite fallbackFillSprite;

    private void Awake()
    {
        ResolveReferences();
        PrepareHealthBar(ghostFill, false);
        PrepareHealthBar(jasonFill, true);
        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void Start()
    {
        if (ghostHealth != null) RefreshHealth(ghostHealth);
        if (jasonHealth != null) RefreshHealth(jasonHealth);
    }

    private void Update()
    {
        AnimateFill(ghostFill, ghostTargetFill, false);
        AnimateFill(jasonFill, jasonTargetFill, true);
    }

    public void RefreshHealth(Health changedHealth)
    {
        float amount = Mathf.Clamp01((float)changedHealth.CurrentHealth / Mathf.Max(1, changedHealth.maxHealth));
        if (changedHealth == ghostHealth)
        {
            ghostTargetFill = amount;
            if (ghostValueText != null) ghostValueText.text = changedHealth.CurrentHealth + " / " + changedHealth.maxHealth;
        }
        else if (changedHealth == jasonHealth)
        {
            jasonTargetFill = amount;
            if (jasonValueText != null) jasonValueText.text = changedHealth.CurrentHealth + " / " + changedHealth.maxHealth;
        }
    }

    public void ShowResult(string message)
    {
        if (resultText != null)
            resultText.text = message;
        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    private void RestartGame()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.RestartGame();
    }

    private void ResolveReferences()
    {
        if (ghostHealth == null)
            ghostHealth = FindHealth("Ghost");
        if (jasonHealth == null)
            jasonHealth = FindHealth("Jason");
        if (ghostFill == null)
            ghostFill = FindImage("Ghost Health Fill");
        if (jasonFill == null)
            jasonFill = FindImage("Jason Health Fill");
    }

    private void PrepareHealthBar(Image fill, bool fromRight)
    {
        if (fill == null)
            return;

        if (fill.sprite == null)
            fill.sprite = GetFillSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = fromRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
        fill.raycastTarget = false;

        GameObject frame = fill.transform.parent != null ? fill.transform.parent.gameObject : null;
        if (frame != null)
        {
            Outline outline = frame.GetComponent<Outline>();
            if (outline == null) outline = frame.AddComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.012f, 0.018f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        TMP_Text valueText = FindOrCreateValueText(fill, fromRight);
        if (fromRight) jasonValueText = valueText;
        else ghostValueText = valueText;
    }

    private void AnimateFill(Image fill, float target, bool jason)
    {
        if (fill == null)
            return;

        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, Time.unscaledDeltaTime * 1.8f);
        fill.color = HealthColor(fill.fillAmount, jason);
    }

    private static Color HealthColor(float amount, bool jason)
    {
        Color healthy = jason ? new Color(0.88f, 0.23f, 0.16f) : new Color(0.16f, 0.76f, 0.82f);
        Color warning = new Color(1f, 0.66f, 0.16f);
        Color critical = new Color(0.72f, 0.035f, 0.07f);
        return amount >= 0.45f
            ? Color.Lerp(warning, healthy, Mathf.InverseLerp(0.45f, 1f, amount))
            : Color.Lerp(critical, warning, Mathf.InverseLerp(0f, 0.45f, amount));
    }

    private TMP_Text FindOrCreateValueText(Image fill, bool jason)
    {
        Transform parent = fill.transform.parent;
        if (parent == null)
            return null;

        string objectName = jason ? "Jason Health Value" : "Ghost Health Value";
        Transform existing = parent.Find(objectName);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TMP_Text>();
        }

        RectTransform rect = text.rectTransform;
        float anchorX = jason ? 0f : 1f;
        rect.anchorMin = new Vector2(anchorX, 0.5f);
        rect.anchorMax = new Vector2(anchorX, 0.5f);
        rect.pivot = new Vector2(anchorX, 0.5f);
        rect.anchoredPosition = new Vector2(jason ? 8f : -8f, 0f);
        rect.sizeDelta = new Vector2(104f, 22f);
        text.font = resultText != null ? resultText.font : TMP_Settings.defaultFontAsset;
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = jason ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        text.raycastTarget = false;
        text.text = "100 / 100";
        text.transform.SetAsLastSibling();
        return text;
    }

    private static Health FindHealth(string objectName)
    {
        GameObject fighter = GameObject.Find(objectName);
        return fighter != null ? fighter.GetComponent<Health>() : null;
    }

    private static Image FindImage(string objectName)
    {
        GameObject imageObject = GameObject.Find(objectName);
        return imageObject != null ? imageObject.GetComponent<Image>() : null;
    }

    private static Sprite GetFillSprite()
    {
        try
        {
            Sprite builtInSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (builtInSprite != null)
                return builtInSprite;
        }
        catch (System.ArgumentException)
        {
        }

        if (fallbackFillSprite == null)
        {
            Texture2D whiteTexture = Texture2D.whiteTexture;
            fallbackFillSprite = Sprite.Create(whiteTexture,
                new Rect(0f, 0f, whiteTexture.width, whiteTexture.height), new Vector2(0.5f, 0.5f), 100f);
            fallbackFillSprite.hideFlags = HideFlags.HideAndDontSave;
        }
        return fallbackFillSprite;
    }
}