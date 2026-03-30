using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botón de velocidad 2x en la HUD de juego.
/// Se crea automáticamente al iniciar la partida; no requiere ningún setup en escena.
/// PauseMenu llama a RestoreSpeed() al despausar para respetar el multiplicador activo.
/// </summary>
[DefaultExecutionOrder(10)]
public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance;

    private int             _multiplier = 1;   // 1 o 2
    private TextMeshProUGUI _label;

    public int CurrentMultiplier => _multiplier;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => CreateButton();

    void CreateButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Esquina superior derecha
        GameObject go = new GameObject("SpeedButton");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt    = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(110f, 52f);  // más grande para toque cómodo
        rt.anchoredPosition = new Vector2(-12f, -12f);

        Image bg            = go.AddComponent<Image>();
        bg.color            = new Color(0.12f, 0.12f, 0.30f, 0.92f);

        Button btn          = go.AddComponent<Button>();
        btn.targetGraphic   = bg;
        ColorBlock cb       = btn.colors;
        cb.highlightedColor = new Color(0.22f, 0.22f, 0.55f, 1f);
        cb.pressedColor     = new Color(0.06f, 0.06f, 0.18f, 1f);
        btn.colors          = cb;
        btn.onClick.AddListener(Toggle);

        GameObject labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform lrt   = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin       = Vector2.zero;
        lrt.anchorMax       = Vector2.one;
        lrt.offsetMin       = lrt.offsetMax = Vector2.zero;

        _label           = labelGO.AddComponent<TextMeshProUGUI>();
        _label.text      = "▶▶ 2x";
        _label.fontSize  = 17f;
        _label.fontStyle = FontStyles.Bold;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color     = Color.white;
    }

    /// <summary>Alterna entre 1x y 2x. Si el juego está pausado actualiza el multiplicador
    /// pero no modifica timeScale (PauseMenu.Resume lo aplica al despausar).</summary>
    public void Toggle()
    {
        _multiplier = _multiplier == 1 ? 2 : 1;
        ApplySpeed();
    }

    /// <summary>Aplica el multiplicador actual. Llamado por PauseMenu al despausar.</summary>
    public void RestoreSpeed() => ApplySpeed();

    void ApplySpeed()
    {
        if (Time.timeScale != 0f)   // no sobreescribir pausa
            Time.timeScale = _multiplier;
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (_label == null) return;
        _label.text = _multiplier == 1 ? "▶▶ 2x" : "▶ 1x";
    }
}
