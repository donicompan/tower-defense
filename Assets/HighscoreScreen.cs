using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de mejores puntuaciones (top 5). Se construye completamente en código.
/// Uso: HighscoreScreen.Show() / HighscoreScreen.Hide()
/// Se puede llamar desde MainMenu o desde el juego.
/// </summary>
public class HighscoreScreen : MonoBehaviour
{
    private static GameObject _instance;

    public static void Show()
    {
        if (_instance != null) Destroy(_instance);

        _instance = new GameObject("HighscoreScreen");
        _instance.AddComponent<HighscoreScreen>().Build();
    }

    public static void Hide()
    {
        if (_instance != null) Destroy(_instance);
    }

    void Build()
    {
        // ── Canvas overlay ────────────────────────────────────────────────
        Canvas canvas       = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // ── Fondo semi-transparente ───────────────────────────────────────
        MakeImage(transform, "BG", new Color(0f, 0f, 0f, 0.88f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Panel central ─────────────────────────────────────────────────
        GameObject panel = MakePanel();

        // ── Título ────────────────────────────────────────────────────────
        MakeLabel(panel.transform, "MEJORES PUNTUACIONES",
            new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(560f, 60f),
            50f, FontStyles.Bold, new Color(1f, 0.85f, 0.15f));

        // ── Cabecera de tabla ─────────────────────────────────────────────
        float tableTop = -95f;
        MakeRowText(panel.transform, "#   Puntos     Oleada  Kills   Oro    Fecha",
            tableTop, 20f, new Color(0.75f, 0.75f, 0.75f), FontStyles.Bold);

        // Línea separadora
        MakeSeparator(panel.transform, tableTop + 26f);

        // ── Filas de datos ────────────────────────────────────────────────
        Leaderboard lb = ScoreManager.LoadLeaderboard();
        Color gold   = new Color(1f, 0.85f, 0.15f);
        Color silver = new Color(0.8f, 0.82f, 0.86f);
        Color bronze = new Color(0.8f, 0.55f, 0.25f);

        for (int i = 0; i < 5; i++)
        {
            float rowY = tableTop + 50f + i * 38f;
            Color rowColor = i == 0 ? gold : i == 1 ? silver : i == 2 ? bronze : Color.white;

            if (i < lb.entries.Count)
            {
                var e = lb.entries[i];
                string line = string.Format(
                    "{0}.   {1,-9} {2,-7} {3,-7} {4,-6} {5}",
                    i + 1, e.score, e.wave, e.enemiesKilled, e.goldEarned, e.date);
                MakeRowText(panel.transform, line, rowY, 22f, rowColor, FontStyles.Normal);
            }
            else
            {
                MakeRowText(panel.transform, $"{i + 1}.   ---", rowY, 22f,
                    new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);
            }
        }

        // ── Botón cerrar ──────────────────────────────────────────────────
        MakeCloseButton(panel.transform);
    }

    // ── Helpers de construcción ───────────────────────────────────────────────

    GameObject MakePanel()
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(600f, 400f);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.06f, 0.97f);

        // Borde dorado
        MakeImage(rt, "Border", new Color(0.7f, 0.55f, 0.1f, 1f),
            Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));
        bg.transform.SetAsLastSibling();

        return go;
    }

    void MakeLabel(Transform parent, string text,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size,
        float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        TextMeshProUGUI tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
    }

    void MakeRowText(Transform parent, string text, float anchoredY,
        float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta        = new Vector2(-32f, 36f);

        TextMeshProUGUI tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        tmp.font      = TMP_Settings.defaultFontAsset;
    }

    void MakeSeparator(Transform parent, float anchoredY)
    {
        MakeImage(parent, "Sep", new Color(0.6f, 0.5f, 0.1f, 0.8f),
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
            new Vector2(0f, anchoredY - 2f), new Vector2(0f, anchoredY));
    }

    void MakeCloseButton(Transform parent)
    {
        GameObject go = new GameObject("CloseBtn", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-8f, -8f);
        rt.sizeDelta        = new Vector2(44f, 44f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.65f, 0.12f, 0.1f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.9f, 0.2f, 0.15f);
        btn.colors = colors;
        btn.onClick.AddListener(Hide);

        // "✕" label
        GameObject lbl = new GameObject("X", typeof(RectTransform));
        lbl.transform.SetParent(go.transform, false);
        RectTransform lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = "✕";
        tmp.fontSize  = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static GameObject MakeImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

        go.AddComponent<Image>().color = color;
        return go;
    }
}
