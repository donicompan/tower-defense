using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Tutorial paso a paso para jugadores nuevos.
/// Uso: TutorialManager.ShowIfNeeded()  (llamar desde GameManager.Start)
/// Usa PlayerPrefs "TutorialDone_v1" para mostrar solo la primera vez.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    const string PREFS_KEY = "TutorialDone_v1";

    public static void ShowIfNeeded()
    {
        if (PlayerPrefs.GetInt(PREFS_KEY, 0) != 0) return;
        new GameObject("TutorialManager").AddComponent<TutorialManager>();
    }

    // ── Contenido del tutorial ─────────────────────────────────────────────────

    static readonly string[] Steps =
    {
        "Selecciona una torre\ndesde el panel de botones.",
        "Haz click en el terreno\npara construir la torre.",
        "Las torres atacan enemigos\nautomaticamente.",
        "Mejora las torres\npara mas dano y alcance."
    };

    static readonly Color[] StepColors =
    {
        new Color(1f,  0.85f, 0.15f),   // amarillo dorado
        new Color(0.4f, 0.85f, 1f),     // celeste
        new Color(1f,  0.5f,  0.2f),    // naranja
        new Color(0.5f, 1f,   0.45f),   // verde
    };

    static readonly string[] StepLabels = { "01", "02", "03", "04" };

    // ── UI ─────────────────────────────────────────────────────────────────────

    CanvasGroup     _panelGroup;
    TextMeshProUGUI _stepNumText;
    TextMeshProUGUI _bodyText;
    TextMeshProUGUI _hintText;
    Image           _accentBar;
    Button          _nextBtn;
    TextMeshProUGUI _nextBtnText;

    bool _advanceRequested;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    void Start() => StartCoroutine(RunTutorial());

    IEnumerator RunTutorial()
    {
        yield return new WaitForSecondsRealtime(1.8f); // esperar a que la escena cargue
        BuildUI();

        for (int i = 0; i < Steps.Length; i++)
        {
            ShowStep(i);
            yield return StartCoroutine(FadePanel(0f, 1f, 0.3f));

            _advanceRequested = false;
            float minWait = 1.8f;
            float waited  = 0f;
            while (!(waited >= minWait && _advanceRequested))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return StartCoroutine(FadePanel(1f, 0f, 0.22f));
            yield return new WaitForSecondsRealtime(0.08f);
        }

        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();
        Destroy(gameObject);
    }

    void ShowStep(int i)
    {
        _stepNumText.text  = StepLabels[i];
        _bodyText.text     = Steps[i];
        _accentBar.color   = StepColors[i];
        _stepNumText.color = StepColors[i];
        _hintText.text     = i < Steps.Length - 1
            ? "Siguiente  >"
            : "Entendido  /";
    }

    IEnumerator FadePanel(float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur) { e += Time.unscaledDeltaTime; _panelGroup.alpha = Mathf.Lerp(from, to, e / dur); yield return null; }
        _panelGroup.alpha = to;
    }

    // ── Construcción de la UI ──────────────────────────────────────────────────

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // ── Panel ─────────────────────────────────────────────────────────────
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(transform, false);

        RectTransform panelRt = panelGO.AddComponent<RectTransform>();
        panelRt.anchorMin      = new Vector2(0f, 0f);
        panelRt.anchorMax      = new Vector2(0f, 0f);
        panelRt.pivot          = new Vector2(0f, 0f);
        panelRt.sizeDelta      = new Vector2(380f, 155f);
        panelRt.anchoredPosition = new Vector2(24f, 24f);

        _panelGroup             = panelGO.AddComponent<CanvasGroup>();
        _panelGroup.alpha       = 0f;
        _panelGroup.blocksRaycasts = true;

        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.06f, 0.10f, 0.93f);

        // Barra de acento (izquierda)
        GameObject accentGO = new GameObject("Accent");
        accentGO.transform.SetParent(panelGO.transform, false);
        RectTransform acRt  = accentGO.AddComponent<RectTransform>();
        acRt.anchorMin       = Vector2.zero;
        acRt.anchorMax       = new Vector2(0f, 1f);
        acRt.pivot           = new Vector2(0f, 0.5f);
        acRt.sizeDelta       = new Vector2(5f, 0f);
        acRt.anchoredPosition = Vector2.zero;
        _accentBar            = accentGO.AddComponent<Image>();

        // Número de paso
        GameObject numGO = new GameObject("StepNum");
        numGO.transform.SetParent(panelGO.transform, false);
        RectTransform numRt  = numGO.AddComponent<RectTransform>();
        numRt.anchorMin       = new Vector2(0f, 0.62f);
        numRt.anchorMax       = new Vector2(0f, 1f);
        numRt.pivot           = new Vector2(0f, 0.5f);
        numRt.sizeDelta       = new Vector2(52f, 0f);
        numRt.anchoredPosition = new Vector2(14f, 0f);
        _stepNumText           = numGO.AddComponent<TextMeshProUGUI>();
        _stepNumText.fontSize  = 40f;
        _stepNumText.fontStyle = FontStyles.Bold;
        _stepNumText.alignment = TextAlignmentOptions.Center;

        // Texto del cuerpo
        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(panelGO.transform, false);
        RectTransform bodyRt = bodyGO.AddComponent<RectTransform>();
        bodyRt.anchorMin      = Vector2.zero;
        bodyRt.anchorMax      = Vector2.one;
        bodyRt.offsetMin      = new Vector2(14f, 34f);
        bodyRt.offsetMax      = new Vector2(-14f, -10f);
        _bodyText             = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyText.fontSize    = 19f;
        _bodyText.fontStyle   = FontStyles.Bold;
        _bodyText.alignment   = TextAlignmentOptions.MidlineLeft;
        _bodyText.color       = Color.white;
        var bodyShadow = bodyGO.AddComponent<Shadow>();
        bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        bodyShadow.effectDistance = new Vector2(1f, -1f);

        // Botón "Siguiente"
        GameObject btnGO = new GameObject("NextBtn");
        btnGO.transform.SetParent(panelGO.transform, false);
        RectTransform btnRt  = btnGO.AddComponent<RectTransform>();
        btnRt.anchorMin       = new Vector2(1f, 0f);
        btnRt.anchorMax       = new Vector2(1f, 0f);
        btnRt.pivot           = new Vector2(1f, 0f);
        btnRt.sizeDelta       = new Vector2(140f, 30f);
        btnRt.anchoredPosition = new Vector2(-10f, 4f);

        Image btnBg   = btnGO.AddComponent<Image>();
        btnBg.color   = new Color(1f, 1f, 1f, 0.08f);
        _nextBtn      = btnGO.AddComponent<Button>();
        _nextBtn.targetGraphic = btnBg;
        ColorBlock cb = _nextBtn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
        cb.pressedColor     = new Color(1f, 1f, 1f, 0.35f);
        _nextBtn.colors = cb;
        _nextBtn.onClick.AddListener(() => _advanceRequested = true);

        GameObject hintGO = new GameObject("HintText");
        hintGO.transform.SetParent(btnGO.transform, false);
        RectTransform hintRt = hintGO.AddComponent<RectTransform>();
        hintRt.anchorMin      = Vector2.zero;
        hintRt.anchorMax      = Vector2.one;
        hintRt.offsetMin      = hintRt.offsetMax = Vector2.zero;
        _hintText             = hintGO.AddComponent<TextMeshProUGUI>();
        _hintText.fontSize    = 13f;
        _hintText.alignment   = TextAlignmentOptions.MidlineRight;
        _hintText.color       = new Color(1f, 1f, 1f, 0.55f);
        _hintText.fontStyle   = FontStyles.Italic;
    }
}
