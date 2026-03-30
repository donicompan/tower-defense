using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    // ── Paleta de colores ─────────────────────────────────────────────────────
    static readonly Color C_Easy   = new Color(0.09f, 0.56f, 0.27f);
    static readonly Color C_Normal = new Color(0.09f, 0.33f, 0.72f);
    static readonly Color C_Hard   = new Color(0.68f, 0.09f, 0.09f);
    static readonly Color C_Gold   = new Color(1.00f, 0.85f, 0.15f);
    static readonly Color C_Panel  = new Color(0.03f, 0.05f, 0.14f, 0.90f);
    static readonly Color C_Accent = new Color(0.25f, 0.55f, 1.00f, 0.70f);

    [Tooltip("Texto donde se muestra el récord actual. Si es null se crea automáticamente.")]
    public TextMeshProUGUI recordText;

    void Start()
    {
        // Hide legacy GameObjects baked into the scene that conflict with dynamic UI
        foreach (string n in new[]{"PlayButton","QuitButton","Background","TitleText","SubtitleText"})
        {
            GameObject old = GameObject.Find(n);
            if (old != null) old.SetActive(false);
        }

        Canvas canvas = EnsureCanvas();
        BuildBackdrop(canvas);
        CreateDifficultyButtons(canvas);
        ShowRecord(canvas);
    }

    Canvas EnsureCanvas()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("MenuCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
        }

        // Upgrade the canvas scaler to ScaleWithScreenSize so UI elements are
        // positioned consistently across resolutions. Reference matches the
        // scene's existing 800×600 canvas so anchor positions stay correct.
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ── Panel de fondo semitransparente detrás de los botones ────────────────

    void BuildBackdrop(Canvas canvas)
    {
        GameObject panel = new GameObject("MenuBackdrop");
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsFirstSibling();

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(320f, 340f);
        rt.anchoredPosition = new Vector2(0f, -20f);

        // Borde exterior con color accent
        GameObject borderGO = new GameObject("PanelBorder");
        borderGO.transform.SetParent(panel.transform, false);
        RectTransform brt = borderGO.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-2f, -2f);
        brt.offsetMax = new Vector2( 2f,  2f);
        UIHelper.Img(borderGO, C_Accent);

        // Fondo principal oscuro
        UIHelper.Img(panel, C_Panel);

        // Línea brillante en la parte superior
        GameObject topLine = new GameObject("TopAccentLine");
        topLine.transform.SetParent(panel.transform, false);
        RectTransform tlrt = topLine.AddComponent<RectTransform>();
        tlrt.anchorMin      = new Vector2(0f, 1f);
        tlrt.anchorMax      = new Vector2(1f, 1f);
        tlrt.pivot          = new Vector2(0.5f, 1f);
        tlrt.sizeDelta      = new Vector2(0f, 3f);
        tlrt.anchoredPosition = Vector2.zero;
        UIHelper.Img(topLine, new Color(0.35f, 0.70f, 1f, 1f));
    }

    // ── Botones de dificultad ─────────────────────────────────────────────────

    void CreateDifficultyButtons(Canvas canvas)
    {
        MakeDiffBtn(canvas, "FACIL",   "30 vidas  |  300 oro", C_Easy,   new Vector2(0f,  70f), PlayEasy);
        MakeDiffBtn(canvas, "NORMAL",  "20 vidas  |  200 oro", C_Normal, new Vector2(0f,   0f), PlayNormal);
        MakeDiffBtn(canvas, "DIFICIL", "10 vidas  |  100 oro", C_Hard,   new Vector2(0f, -70f), PlayHard);
        MakeSmallBtn(canvas, "RECORDS", new Color(0.35f, 0.26f, 0.05f), new Vector2(0f, -135f), ShowHighscores);
    }

    void MakeDiffBtn(Canvas canvas, string title, string subtitle,
                     Color color, Vector2 anchoredPos, System.Action callback)
    {
        Vector2 size = new Vector2(280f, 58f);

        // Sombra desplazada (ilusión de profundidad)
        MakeBoxRaw("Shadow_" + title, canvas.transform, size + new Vector2(8f, 8f),
            anchoredPos + new Vector2(4f, -4f), new Color(0f, 0f, 0f, 0.4f));

        // Borde (versión oscura del color)
        Color border = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
        MakeBoxRaw("Border_" + title, canvas.transform, size + new Vector2(4f, 4f), anchoredPos, border);

        // Botón principal
        GameObject go = new GameObject("DiffBtn_" + title);
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = size;
        rt.anchoredPosition = anchoredPos;

        Image bg = UIHelper.Img(go, color);

        // Reflejo superior (hace que parezca sólido/3D)
        GameObject hl = new GameObject("Highlight");
        hl.transform.SetParent(go.transform, false);
        RectTransform hlrt = hl.AddComponent<RectTransform>();
        hlrt.anchorMin = new Vector2(0f, 0.6f);
        hlrt.anchorMax = Vector2.one;
        hlrt.offsetMin = new Vector2(2f,  0f);
        hlrt.offsetMax = new Vector2(-2f, -2f);
        UIHelper.Img(hl, new Color(1f, 1f, 1f, 0.10f));

        // Acento izquierdo (barra blanca brillante)
        GameObject accent = new GameObject("LeftAccent");
        accent.transform.SetParent(go.transform, false);
        RectTransform art = accent.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 0f);
        art.anchorMax = new Vector2(0f, 1f);
        art.pivot     = new Vector2(0f, 0.5f);
        art.sizeDelta = new Vector2(5f, -6f);
        art.anchoredPosition = new Vector2(0f, 0f);
        UIHelper.Img(accent, new Color(1f, 1f, 1f, 0.60f));

        // Componente Button
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(Mathf.Min(color.r * 1.45f, 1f),
                                         Mathf.Min(color.g * 1.45f, 1f),
                                         Mathf.Min(color.b * 1.45f, 1f), 1f);
        cb.pressedColor  = new Color(color.r * 0.60f, color.g * 0.60f, color.b * 0.60f, 1f);
        cb.fadeDuration  = 0.07f;
        btn.colors       = cb;
        btn.onClick.AddListener(() => callback());

        // Título
        GameObject tGO    = new GameObject("Title");
        tGO.transform.SetParent(go.transform, false);
        RectTransform trt = tGO.AddComponent<RectTransform>();
        trt.anchorMin     = new Vector2(0f, 0.48f);
        trt.anchorMax     = Vector2.one;
        trt.offsetMin     = new Vector2(18f, 0f);
        trt.offsetMax     = new Vector2(-8f, 0f);
        var ttmp          = tGO.AddComponent<TextMeshProUGUI>();
        ttmp.text         = title;
        ttmp.fontSize     = 25f;
        ttmp.fontStyle    = FontStyles.Bold;
        ttmp.alignment    = TextAlignmentOptions.MidlineLeft;
        ttmp.color        = Color.white;

        // Subtítulo
        GameObject sGO    = new GameObject("Sub");
        sGO.transform.SetParent(go.transform, false);
        RectTransform srt = sGO.AddComponent<RectTransform>();
        srt.anchorMin     = new Vector2(0f, 0f);
        srt.anchorMax     = new Vector2(1f, 0.50f);
        srt.offsetMin     = new Vector2(18f, 0f);
        srt.offsetMax     = new Vector2(-8f, 0f);
        var stmp          = sGO.AddComponent<TextMeshProUGUI>();
        stmp.text         = subtitle;
        stmp.fontSize     = 13f;
        stmp.alignment    = TextAlignmentOptions.MidlineLeft;
        stmp.color        = new Color(1f, 1f, 1f, 0.62f);
    }

    void MakeSmallBtn(Canvas canvas, string label, Color color, Vector2 anchoredPos, System.Action callback)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(190f, 36f);
        rt.anchoredPosition = anchoredPos;

        Image bg      = UIHelper.Img(go, color);
        Button btn    = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = color * 1.5f;
        cb.pressedColor     = color * 0.6f;
        cb.fadeDuration     = 0.07f;
        btn.colors    = cb;
        btn.onClick.AddListener(() => callback());

        GameObject lGO    = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        RectTransform lrt = lGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp       = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = C_Gold;
    }

    // ── Récord ────────────────────────────────────────────────────────────────

    void ShowRecord(Canvas canvas)
    {
        RunRecord best = ScoreManager.GetBestRecord();
        string recordStr = best != null
            ? "MEJOR:  " + best.score + " pts  |  Oleada " + best.wave
            : "Sin record aun";
        bool hasBest = best != null;

        if (recordText != null)
        {
            recordText.text  = recordStr;
            recordText.color = hasBest ? C_Gold : Color.gray;
            recordText.fontStyle = FontStyles.Bold;
        }
        else
        {
            GameObject go = new GameObject("RecordText", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta        = new Vector2(460f, 34f);
            var tmp       = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = recordStr;
            tmp.fontSize  = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = hasBest ? C_Gold : Color.gray;
            var shadow            = go.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    static void MakeBoxRaw(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = size;
        rt.anchoredPosition = pos;
        UIHelper.Img(go, color);
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    public void PlayEasy()   { DifficultySettings.Current = DifficultySettings.Level.Easy;   PlayGame(); }
    public void PlayNormal() { DifficultySettings.Current = DifficultySettings.Level.Normal; PlayGame(); }
    public void PlayHard()   { DifficultySettings.Current = DifficultySettings.Level.Hard;   PlayGame(); }
    public void PlayGame()       { LoadingScreen.LoadScene("SampleScene"); }
    public void ShowHighscores() { HighscoreScreen.Show(); }
    public void QuitGame()       { Application.Quit(); }
}
