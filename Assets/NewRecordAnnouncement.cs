using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Muestra "¡NUEVO RÉCORD!" en pantalla con animación dorada al batir el récord.
/// Uso: NewRecordAnnouncement.Show(score);
/// </summary>
public class NewRecordAnnouncement : MonoBehaviour
{
    public static void Show(int score)
    {
        GameObject go = new GameObject("NewRecordAnnouncement");
        go.AddComponent<NewRecordAnnouncement>()._score = score;
    }

    private int _score;

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        // ── Canvas ────────────────────────────────────────────────────────
        Canvas canvas       = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        gameObject.AddComponent<CanvasScaler>();

        // ── Texto principal "¡NUEVO RÉCORD!" ─────────────────────────────
        GameObject mainGO = new GameObject("MainText");
        mainGO.transform.SetParent(transform, false);

        RectTransform mainRT = mainGO.AddComponent<RectTransform>();
        mainRT.anchorMin        = new Vector2(0.5f, 0.6f);
        mainRT.anchorMax        = new Vector2(0.5f, 0.6f);
        mainRT.pivot            = Vector2.one * 0.5f;
        mainRT.anchoredPosition = Vector2.zero;
        mainRT.sizeDelta        = new Vector2(700f, 120f);

        TextMeshProUGUI mainTMP  = mainGO.AddComponent<TextMeshProUGUI>();
        mainTMP.text      = "¡NUEVO RÉCORD!";
        mainTMP.fontSize  = 100f;
        mainTMP.fontStyle = FontStyles.Bold;
        mainTMP.alignment = TextAlignmentOptions.Center;

        var mainShadow = mainGO.AddComponent<Shadow>();
        mainShadow.effectColor    = new Color(0f, 0f, 0f, 0.9f);
        mainShadow.effectDistance = new Vector2(5f, -5f);

        // ── Texto de puntuación ───────────────────────────────────────────
        GameObject subGO = new GameObject("SubText");
        subGO.transform.SetParent(transform, false);

        RectTransform subRT = subGO.AddComponent<RectTransform>();
        subRT.anchorMin        = new Vector2(0.5f, 0.5f);
        subRT.anchorMax        = new Vector2(0.5f, 0.5f);
        subRT.pivot            = Vector2.one * 0.5f;
        subRT.anchoredPosition = Vector2.zero;
        subRT.sizeDelta        = new Vector2(500f, 60f);

        TextMeshProUGUI subTMP  = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text      = "Puntuación: " + _score;
        subTMP.fontSize  = 42f;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = Color.white;

        var subShadow = subGO.AddComponent<Shadow>();
        subShadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        subShadow.effectDistance = new Vector2(3f, -3f);

        // ── Animación: aparece rápido, se sostiene, se desvanece ──────────
        const float totalDuration = 3.2f;
        const float growEnd       = 0.25f;   // fracción en que termina el zoom in
        const float holdEnd       = 0.70f;   // fracción en que empieza el fade

        Color gold    = new Color(1f, 0.85f, 0.1f, 1f);
        Color goldFade = new Color(1f, 0.85f, 0.1f, 0f);

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalDuration;

            // Escala: zoom in rápido, luego leve pulso
            float scaleT = Mathf.Clamp01(t / growEnd);
            float scale  = Mathf.Lerp(0.2f, 1.05f, Mathf.SmoothStep(0f, 1f, scaleT));
            // Pequeño pulso de latido en la zona de hold
            if (t > growEnd && t < holdEnd)
            {
                float pulse = Mathf.Sin((t - growEnd) / (holdEnd - growEnd) * Mathf.PI * 2f) * 0.03f;
                scale += pulse;
            }
            mainGO.transform.localScale = Vector3.one * scale;
            subGO.transform.localScale  = Vector3.one * Mathf.Clamp01(scale);

            // Alpha: visible durante hold, fade out en la última parte
            float alpha = t < holdEnd
                ? 1f
                : Mathf.Lerp(1f, 0f, (t - holdEnd) / (1f - holdEnd));

            mainTMP.color = Color.Lerp(goldFade, gold, alpha);
            subTMP.color  = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}
