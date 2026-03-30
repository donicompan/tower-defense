using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;
    public GameObject pausePanel;
    public Slider volumeSlider;

    private bool isPaused = false;

    private const string VOLUME_KEY = "MasterVolume";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        pausePanel.SetActive(false);

        // Cargar volumen guardado (default 0.8 si es la primera vez)
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.8f);
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        CreatePauseButton();
    }

    void CreatePauseButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Botón ≡ en esquina superior izquierda
        GameObject go = new GameObject("PauseBtn");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt  = go.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0f, 1f);
        rt.anchorMax       = new Vector2(0f, 1f);
        rt.pivot           = new Vector2(0f, 1f);
        rt.sizeDelta       = new Vector2(72f, 52f);
        rt.anchoredPosition = new Vector2(12f, -12f);

        Image bg           = go.AddComponent<Image>();
        bg.color           = new Color(0.08f, 0.09f, 0.18f, 0.90f);
        bg.sprite          = UIHelper.Bg;

        Button btn         = go.AddComponent<Button>();
        btn.targetGraphic  = bg;
        ColorBlock cb      = btn.colors;
        cb.highlightedColor = new Color(0.20f, 0.22f, 0.42f, 1f);
        cb.pressedColor    = new Color(0.05f, 0.06f, 0.12f, 1f);
        btn.colors         = cb;
        btn.onClick.AddListener(TogglePause);

        // Borde sutil
        GameObject border  = new GameObject("Border");
        border.transform.SetParent(go.transform, false);
        RectTransform brt  = border.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-1f, -1f); brt.offsetMax = new Vector2(1f, 1f);
        UIHelper.Img(border, new Color(0.30f, 0.33f, 0.60f, 0.70f));
        border.transform.SetAsFirstSibling();

        // Ícono ≡
        GameObject labelGO = new GameObject("Icon");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform lrt  = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI lbl = labelGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "≡";
        lbl.fontSize  = 28f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color     = new Color(0.85f, 0.88f, 1f);
    }

    void Update()
    {
        // ESC (PC) / botón Atrás (Android) para pausar
        // FreeBuildManager consume el ESC cuando está en modo colocación,
        // por eso coordinamos con HasTowerSelected
        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (escPressed)
        {
            if (FreeBuildManager.Instance != null && BuildManager.Instance != null
                && BuildManager.Instance.HasTowerSelected)
                return;   // ESC fue para cancelar construcción, no para pausar
            TogglePause();
        }
    }

    // Llamado por Unity cuando la app va a background (Android home, llamada, etc.)
    void OnApplicationPause(bool paused)
    {
        if (paused && !isPaused)
        {
            isPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        if (isPaused) Time.timeScale = 0f;
        else          ResumeSpeed();
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        ResumeSpeed();
    }

    // Restaura la velocidad (1x o 2x) que había antes de pausar
    void ResumeSpeed()
    {
        if (SpeedController.Instance != null)
            SpeedController.Instance.RestoreSpeed();
        else
            Time.timeScale = 1f;
    }

    public void GoToMenu()
    {
        TerrainManager.Restore();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
        PlayerPrefs.Save();
    }
}