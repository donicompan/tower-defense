using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de carga con barra de progreso y fade-out al terminar.
/// Uso: LoadingScreen.LoadScene("SampleScene");
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static void LoadScene(string sceneName)
    {
        // Destruir cualquier LoadingScreen previo que haya quedado huérfano
        // (puede ocurrir si el coroutine fue interrumpido o se llamó dos veces).
        var existing = Object.FindFirstObjectByType<LoadingScreen>();
        if (existing != null) Object.Destroy(existing.gameObject);

        GameObject go = new GameObject("LoadingScreen");
        DontDestroyOnLoad(go);
        go.AddComponent<LoadingScreen>()._targetScene = sceneName;
    }

    private string _targetScene;
    private Image _progressFill;
    private TextMeshProUGUI _loadingText;
    private CanvasGroup _canvasGroup;

    void Start() => StartCoroutine(LoadAsync());

    IEnumerator LoadAsync()
    {
        BuildUI();
        yield return null; // dejar que el canvas renderice

        AsyncOperation op = SceneManager.LoadSceneAsync(_targetScene);
        op.allowSceneActivation = false;

        float displayed = 0f;

        // Avanzar la barra mientras carga (0 → 0.9 = listo para activar)
        while (op.progress < 0.9f)
        {
            displayed = Mathf.MoveTowards(displayed, op.progress / 0.9f, Time.deltaTime * 0.8f);
            SetProgress(displayed);
            yield return null;
        }

        // Rellenar hasta 100 %
        while (displayed < 1f)
        {
            displayed = Mathf.MoveTowards(displayed, 1f, Time.deltaTime * 1.2f);
            SetProgress(displayed);
            yield return null;
        }

        _loadingText.text = "Cargando...  100%";
        yield return new WaitForSecondsRealtime(0.25f);

        op.allowSceneActivation = true;
        yield return op; // esperar a que la escena se active

        // Fade out sobre la nueva escena
        float elapsed = 0f;
        const float FADE = 0.55f;
        while (elapsed < FADE)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FADE);
            yield return null;
        }

        Destroy(gameObject);
    }

    void SetProgress(float t)
    {
        if (_progressFill != null) _progressFill.fillAmount = t;
        if (_loadingText  != null) _loadingText.text = "Cargando...  " + Mathf.RoundToInt(t * 100f) + "%";
    }

    void BuildUI()
    {
        // Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
        _canvasGroup       = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        // Fondo negro total
        GameObject bgGO    = new GameObject("BG");
        bgGO.transform.SetParent(transform, false);
        RectTransform bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin     = Vector2.zero;
        bgRt.anchorMax     = Vector2.one;
        bgRt.offsetMin     = bgRt.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = Color.black;

        // Contenedor central
        GameObject center    = new GameObject("Center");
        center.transform.SetParent(transform, false);
        RectTransform centerRt = center.AddComponent<RectTransform>();
        centerRt.anchorMin     = new Vector2(0.2f, 0.44f);
        centerRt.anchorMax     = new Vector2(0.8f, 0.58f);
        centerRt.offsetMin     = centerRt.offsetMax = Vector2.zero;

        // Fondo de la barra (gris oscuro)
        GameObject barBgGO    = new GameObject("BarBG");
        barBgGO.transform.SetParent(center.transform, false);
        RectTransform barBgRt = barBgGO.AddComponent<RectTransform>();
        barBgRt.anchorMin     = new Vector2(0f, 0f);
        barBgRt.anchorMax     = new Vector2(1f, 0.42f);
        barBgRt.offsetMin     = barBgRt.offsetMax = Vector2.zero;
        barBgGO.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f);

        // Relleno de la barra
        GameObject fillGO    = new GameObject("Fill");
        fillGO.transform.SetParent(barBgGO.transform, false);
        RectTransform fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin     = Vector2.zero;
        fillRt.anchorMax     = Vector2.one;
        fillRt.offsetMin     = fillRt.offsetMax = Vector2.zero;
        _progressFill        = fillGO.AddComponent<Image>();
        _progressFill.color  = new Color(1f, 0.85f, 0.15f);
        _progressFill.type   = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillAmount = 0f;

        // Texto "Cargando..."
        GameObject txtGO     = new GameObject("Text");
        txtGO.transform.SetParent(center.transform, false);
        RectTransform txtRt  = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin      = new Vector2(0f, 0.50f);
        txtRt.anchorMax      = Vector2.one;
        txtRt.offsetMin      = txtRt.offsetMax = Vector2.zero;
        _loadingText         = txtGO.AddComponent<TextMeshProUGUI>();
        _loadingText.text    = "Cargando...  0%";
        _loadingText.fontSize     = 26f;
        _loadingText.fontStyle    = FontStyles.Bold;
        _loadingText.alignment    = TextAlignmentOptions.Center;
        _loadingText.color        = Color.white;
    }
}
