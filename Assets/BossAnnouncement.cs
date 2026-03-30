using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Muestra "¡JEFE!" en pantalla durante 2 segundos al aparecer el boss.
/// Uso: BossAnnouncement.Show();
/// </summary>
public class BossAnnouncement : MonoBehaviour
{
    public static void Show()
    {
        GameObject go = new GameObject("BossAnnouncement");
        go.AddComponent<BossAnnouncement>();
    }

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        // ── Canvas ────────────────────────────────────────────────────────
        Canvas canvas       = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        gameObject.AddComponent<CanvasScaler>();

        // ── Texto ─────────────────────────────────────────────────────────
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(transform, false);

        RectTransform rt = txtGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "¡JEFE!";
        tmp.fontSize  = 130f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        // Sombra de texto para legibilidad
        var shadow = txtGO.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4f, -4f);

        // ── Animación: escalar + fade ──────────────────────────────────────
        const float duration   = 2f;
        const float fadeStart  = 0.55f;   // fracción del tiempo en que empieza el fade

        Color colorFull = new Color(1f, 0.12f, 0.08f, 1f);
        Color colorFade = new Color(1f, 0.12f, 0.08f, 0f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Escala: arranca en 0.4, llega a 1 en el 30% del tiempo, luego baja a 1.08 suavemente
            float scaleT  = Mathf.Clamp01(t / 0.3f);
            float scale   = Mathf.Lerp(0.4f, 1.08f, Mathf.SmoothStep(0f, 1f, scaleT));
            txtGO.transform.localScale = Vector3.one * scale;

            // Fade out en la segunda mitad
            float alpha = t < fadeStart ? 1f
                        : Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));
            tmp.color = Color.Lerp(colorFade, colorFull, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}
