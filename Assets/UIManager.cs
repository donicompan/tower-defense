using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI goldText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI waveText;

    [Tooltip("Opcional — se crea en código si no está asignado")]
    public TextMeshProUGUI countdownText;

    private TextMeshProUGUI _announceText;
    private Image           _announceBg;
    private Coroutine       _announceCoroutine;
    private bool            _countdownVisible;

    // ── Paleta ────────────────────────────────────────────────────────────────
    static readonly Color C_Gold    = new Color(1.00f, 0.85f, 0.15f);
    static readonly Color C_Red     = new Color(1.00f, 0.28f, 0.28f);
    static readonly Color C_Cyan    = new Color(0.35f, 0.90f, 1.00f);
    static readonly Color C_Announce= new Color(1.00f, 0.85f, 0.08f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Aplicar estilos visuales a los textos del HUD asignados desde la escena
        StyleHudText(goldText,  C_Gold, 28f);
        StyleHudText(livesText, C_Red,  28f);
        StyleHudText(waveText,  C_Cyan, 28f);

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();

        // ── Countdown ─────────────────────────────────────────────────────────
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        else if (canvas != null)
        {
            // Fondo del countdown
            GameObject bgGO = new GameObject("CountdownBg", typeof(RectTransform));
            bgGO.transform.SetParent(canvas.transform, false);
            RectTransform bgRT    = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin        = new Vector2(0.5f, 0.5f);
            bgRT.anchorMax        = new Vector2(0.5f, 0.5f);
            bgRT.pivot            = new Vector2(0.5f, 0.5f);
            bgRT.anchoredPosition = new Vector2(0f, 82f);
            bgRT.sizeDelta        = new Vector2(380f, 54f);
            UIHelper.Img(bgGO, new Color(0f, 0f, 0f, 0.62f));
            bgGO.SetActive(false);

            // Línea de acento superior del countdown
            GameObject lineGO = new GameObject("CountdownLine");
            lineGO.transform.SetParent(bgGO.transform, false);
            RectTransform lrt = lineGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot     = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(0f, 3f);
            lrt.anchoredPosition = Vector2.zero;
            UIHelper.Img(lineGO, C_Cyan);

            // Texto del countdown
            GameObject go = new GameObject("CountdownText", typeof(RectTransform));
            go.transform.SetParent(bgGO.transform, false);
            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = new Vector2(8f, 0f);
            rt.offsetMax        = new Vector2(-8f, 0f);

            countdownText           = go.AddComponent<TextMeshProUGUI>();
            countdownText.fontSize  = 26f;
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color     = C_Cyan;

            var shadow            = go.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.90f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // Guardar referencia al bgGO para activar/desactivar junto con el texto
            bgGO.SetActive(false);
            // Reasignar countdownText al hijo para que ShowCountdown pueda activar el padre
            countdownText.gameObject.SetActive(false);
            // Nota: activar countdownText activa también bgGO porque countdownText es hijo de bgGO
        }

        // ── Announcement (Modo Endless, boss, etc.) ───────────────────────────
        if (canvas != null)
        {
            // Fondo semitransparente del announcement
            GameObject bgGO = new GameObject("AnnounceBg", typeof(RectTransform));
            bgGO.transform.SetParent(canvas.transform, false);
            RectTransform bgRT    = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin        = new Vector2(0.5f, 0.5f);
            bgRT.anchorMax        = new Vector2(0.5f, 0.5f);
            bgRT.pivot            = new Vector2(0.5f, 0.5f);
            bgRT.anchoredPosition = new Vector2(0f, 42f);
            bgRT.sizeDelta        = new Vector2(600f, 96f);
            _announceBg           = UIHelper.Img(bgGO, new Color(0f, 0f, 0f, 0.70f));

            // Línea de acento superior del announcement
            GameObject lineGO = new GameObject("AnnounceLine");
            lineGO.transform.SetParent(bgGO.transform, false);
            RectTransform lrt = lineGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot     = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(0f, 3f);
            lrt.anchoredPosition = Vector2.zero;
            UIHelper.Img(lineGO, C_Announce);

            // Texto principal
            GameObject go = new GameObject("AnnouncementText", typeof(RectTransform));
            go.transform.SetParent(bgGO.transform, false);
            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = new Vector2(12f, 4f);
            rt.offsetMax        = new Vector2(-12f, -4f);

            _announceText           = go.AddComponent<TextMeshProUGUI>();
            _announceText.fontSize  = 50f;
            _announceText.fontStyle = FontStyles.Bold;
            _announceText.alignment = TextAlignmentOptions.Center;
            _announceText.color     = C_Announce;

            var shadow            = go.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(3f, -3f);

            bgGO.SetActive(false);
        }
    }

    static void StyleHudText(TextMeshProUGUI tmp, Color color, float fontSize)
    {
        if (tmp == null) return;
        tmp.color     = color;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
    }

    // ── HUD updates ───────────────────────────────────────────────────────────

    public void UpdateGold(int amount)
    {
        if (goldText != null) goldText.text = "ORO  " + amount;
    }

    public void UpdateLives(int amount)
    {
        if (livesText != null) livesText.text = "VIDAS  " + amount;
    }

    public void UpdateWave(int wave)
    {
        if (waveText != null) waveText.text = "OLEADA  " + wave;
    }

    // ── Countdown ─────────────────────────────────────────────────────────────

    public bool IsCountdownVisible() => _countdownVisible;

    public void ShowCountdown(int seconds)
    {
        if (countdownText == null) return;
        _countdownVisible = true;
        countdownText.text = seconds > 0
            ? $"Proxima oleada en  {seconds}s"
            : "Preparate!";
        // Activar el padre (que incluye el fondo) si existe
        Transform parent = countdownText.transform.parent;
        if (parent != null && parent.name == "CountdownBg")
            parent.gameObject.SetActive(true);
        else if (!countdownText.gameObject.activeSelf)
            countdownText.gameObject.SetActive(true);
    }

    public void HideCountdown()
    {
        _countdownVisible = false;
        if (countdownText == null) return;
        Transform parent = countdownText.transform.parent;
        if (parent != null && parent.name == "CountdownBg")
            parent.gameObject.SetActive(false);
        else if (countdownText.gameObject.activeSelf)
            countdownText.gameObject.SetActive(false);
    }

    // ── Announcement ──────────────────────────────────────────────────────────

    public void ShowAnnouncement(string text, float duration = 2.5f, Color color = default)
    {
        if (_announceText == null) return;
        if (_announceCoroutine != null) StopCoroutine(_announceCoroutine);
        _announceText.color = color.a > 0f ? color : C_Announce;
        _announceCoroutine = StartCoroutine(AnnounceRoutine(text, duration));
    }

    IEnumerator AnnounceRoutine(string text, float duration)
    {
        _announceText.text = text;
        if (_announceBg != null) _announceBg.gameObject.SetActive(true);
        else _announceText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (_announceBg != null) _announceBg.gameObject.SetActive(false);
        else _announceText.gameObject.SetActive(false);
        _announceCoroutine = null;
    }
}
