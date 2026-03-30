using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    private GameObject      _panel;
    private CanvasGroup     _canvasGroup;
    private RectTransform   _titleRT;
    private Vector2         _titleBasePos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        BuildUI();
        _panel.SetActive(false);
        _canvasGroup.alpha = 0f;    // fuerza estado limpio aunque coroutines previas hayan quedado a 1
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void ShowGameOver(int waves)
    {
        if (_panel == null) return;
        _panel.SetActive(true);
        Time.timeScale = 0f;

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore  : 0;
        int kills = ScoreManager.Instance != null ? ScoreManager.Instance.EnemiesKilled : 0;
        int gold  = GameManager.Instance  != null ? GameManager.Instance.gold           : 0;

        SetStat("StatWave",  $"Oleadas sobrevividas:   {waves}");
        SetStat("StatScore", $"Puntuacion:             {score:N0}");
        SetStat("StatGold",  $"Oro acumulado:          {gold}");
        SetStat("StatKills", $"Enemigos eliminados:    {kills}");

        StartCoroutine(FadeIn());
        StartCoroutine(ShakeTitle());
    }

    // ── Construcción de UI ────────────────────────────────────────────────────

    void BuildUI()
    {
        Canvas canvas       = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        // Raíz con CanvasGroup para fade
        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(transform, false);
        _canvasGroup       = _panel.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        UIHelper.Img(_panel, new Color(0f, 0f, 0f, 0.85f));

        // Sombra detrás de la caja
        MakeBox("Shadow", _panel.transform, new Vector2(762f, 692f), new Vector2(5f, -5f),
            new Color(0f, 0f, 0f, 0.55f));

        // Borde rojo brillante
        MakeBox("Border", _panel.transform, new Vector2(752f, 682f), Vector2.zero,
            new Color(0.80f, 0.08f, 0.08f, 1f));

        // Caja central oscura
        GameObject box = MakeBox("Box", _panel.transform, new Vector2(740f, 670f), Vector2.zero,
            new Color(0.07f, 0.04f, 0.04f, 1f));

        // Franja header con gradiente simulado
        GameObject header = MakeBox("Header", box.transform, new Vector2(740f, 120f),
            new Vector2(0f, 275f), new Color(0.18f, 0.05f, 0.05f, 1f));
        SetFullAnchors(header, new Vector2(0f, 1f), new Vector2(1f, 1f));

        // Línea de acento inferior del header
        MakeBox("HeaderAccent", box.transform, new Vector2(740f, 3f),
            new Vector2(0f, 215f), new Color(0.85f, 0.12f, 0.12f, 1f));

        // Título (se va a sacudir)
        GameObject titleGO = MakeText("Title", box.transform,
            "GAME OVER", 84, FontStyles.Bold,
            new Color(1.00f, 0.18f, 0.18f), new Vector2(0f, 230f), new Vector2(700f, 120f));
        _titleRT       = titleGO.GetComponent<RectTransform>();
        _titleBasePos  = _titleRT.anchoredPosition;

        // Separador
        MakeBox("Sep", box.transform, new Vector2(660f, 2f), new Vector2(0f, 158f),
            new Color(0.75f, 0.10f, 0.10f, 0.70f));

        // Stats con mejor espaciado
        var statNames  = new[] { "StatWave", "StatScore", "StatGold", "StatKills" };
        var statColors = new[] {
            new Color(1.00f, 0.65f, 0.65f),
            new Color(1.00f, 0.95f, 0.45f),
            new Color(1.00f, 0.82f, 0.20f),
            new Color(0.55f, 0.85f, 1.00f),
        };
        float[] statY = { 108f, 52f, -8f, -68f };

        for (int i = 0; i < statNames.Length; i++)
            MakeText(statNames[i], box.transform, "", 28, FontStyles.Bold,
                statColors[i], new Vector2(0f, statY[i]), new Vector2(660f, 42f));

        // Botones
        MakeButton("BtnRetry", box.transform, "REINTENTAR",
            new Vector2(-185f, -248f), new Color(0.55f, 0.09f, 0.09f, 1f), Restart);
        MakeButton("BtnMenu", box.transform, "MENU",
            new Vector2( 185f, -248f), new Color(0.10f, 0.20f, 0.52f, 1f), GoToMenu);
    }

    // ── Animaciones ───────────────────────────────────────────────────────────

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    IEnumerator ShakeTitle()
    {
        if (_titleRT == null) yield break;

        float elapsed   = 0f;
        const float dur = 0.7f;
        const float freq = 28f;    // Hz del shake
        const float amp  = 18f;    // píxeles de desplazamiento máximo

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay  = 1f - Mathf.Clamp01(elapsed / dur);
            float offset = Mathf.Sin(elapsed * freq) * amp * decay;
            _titleRT.anchoredPosition = _titleBasePos + new Vector2(offset, 0f);
            yield return null;
        }
        _titleRT.anchoredPosition = _titleBasePos;
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    public void Restart()
    {
        TerrainManager.Restore();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        TerrainManager.Restore();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SetStat(string name, string text)
    {
        Transform t = _panel.transform.Find("Box/" + name);
        if (t != null) t.GetComponent<TextMeshProUGUI>().text = text;
    }

    static GameObject MakeBox(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = size;
        rt.anchoredPosition = pos;
        UIHelper.Img(go, color);
        return go;
    }

    static GameObject MakeText(string name, Transform parent, string text,
        float fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = size;
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void MakeButton(string name, Transform parent, string label,
        Vector2 pos, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        // Sombra del botón
        GameObject shadowGO = new GameObject(name + "_Shd");
        shadowGO.transform.SetParent(parent, false);
        RectTransform shRT = shadowGO.AddComponent<RectTransform>();
        shRT.anchorMin = shRT.anchorMax = shRT.pivot = new Vector2(0.5f, 0.5f);
        shRT.sizeDelta       = new Vector2(346f, 80f);
        shRT.anchoredPosition = pos + new Vector2(3f, -3f);
        UIHelper.Img(shadowGO, new Color(0f, 0f, 0f, 0.4f));

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(340f, 74f);
        rt.anchoredPosition = pos;

        Image img = UIHelper.Img(go, bgColor);

        // Reflejo superior
        GameObject hlGO = new GameObject("HL");
        hlGO.transform.SetParent(go.transform, false);
        RectTransform hlRT = hlGO.AddComponent<RectTransform>();
        hlRT.anchorMin = new Vector2(0f, 0.55f);
        hlRT.anchorMax = Vector2.one;
        hlRT.offsetMin = new Vector2(2f, 0f);
        hlRT.offsetMax = new Vector2(-2f, -2f);
        UIHelper.Img(hlGO, new Color(1f, 1f, 1f, 0.10f));

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        Color highlight = new Color(Mathf.Min(bgColor.r * 1.4f, 1f),
                                     Mathf.Min(bgColor.g * 1.4f, 1f),
                                     Mathf.Min(bgColor.b * 1.4f, 1f), 1f);
        Color pressed   = new Color(bgColor.r * 0.55f, bgColor.g * 0.55f, bgColor.b * 0.55f, 1f);
        cb.highlightedColor = highlight;
        cb.pressedColor     = pressed;
        cb.fadeDuration     = 0.07f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        MakeText("Label", go.transform, label, 28, FontStyles.Bold,
            Color.white, Vector2.zero, new Vector2(320f, 60f));
    }

    static void SetFullAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot     = new Vector2(0.5f, 1f);
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
