using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class TowerUpgradeUI : MonoBehaviour
{
    public static TowerUpgradeUI Instance { get; private set; }

    private Tower          selectedTower;
    private GameObject     panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI upgradeBtnText;
    private Button          upgradeBtn;
    private Image           upgradeBtnImage;
    private Button          sellBtn;
    private Image           sellBtnImage;
    private TextMeshProUGUI sellBtnText;
    private TextMeshProUGUI _targetingLabel;

    private static readonly string[] _targetingNames =
        { "Más cercano", "El primero", "El último", "El más fuerte", "El más rápido" };

    private static readonly Color COLOR_PANEL      = new Color(0.08f, 0.08f, 0.10f, 0.96f);
    private static readonly Color COLOR_CAN_BUY    = new Color(0.18f, 0.62f, 0.22f, 1f);
    private static readonly Color COLOR_CANT_BUY   = new Color(0.72f, 0.15f, 0.15f, 1f);
    private static readonly Color COLOR_MAX        = new Color(0.30f, 0.30f, 0.35f, 1f);
    private static readonly Color COLOR_DIVIDER    = new Color(0.35f, 0.35f, 0.40f, 1f);
    private static readonly Color COLOR_SELL       = new Color(0.52f, 0.12f, 0.12f, 1f);

    // Crea (o devuelve) la instancia singleton
    public static TowerUpgradeUI GetOrCreate()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("TowerUpgradeUI");
        return go.AddComponent<TowerUpgradeUI>();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        BuildUI();
        panel.SetActive(false);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void Show(Tower tower)
    {
        selectedTower = tower;
        bool maxed = !tower.CanUpgrade();
        if (maxed) AudioManager.PlayMaxLevel();
        tower.ShowRangeIndicator();
        Refresh();
        panel.SetActive(true);
    }

    public void Hide()
    {
        selectedTower?.HideRangeIndicator();
        panel.SetActive(false);
        selectedTower = null;
    }

    // ── Update: cerrar al clickear fuera del panel ────────────────────────────

    void Update()
    {
        if (!panel.activeSelf) return;

        // Si la torre fue destruida externamente (p. ej. enemigo) cerrar el panel
        if (selectedTower == null) { Hide(); return; }

        // Actualizar color del botón en tiempo real (el oro puede cambiar mientras el panel está abierto)
        Refresh();

        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        Vector2 pointerPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        if (!clicked && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    clicked    = true;
                    pointerPos = touch.position.ReadValue();
                    break;
                }
            }
        }
        if (!clicked) return;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, pointerPos))
            Hide();
    }

    // ── Lógica de upgrade ─────────────────────────────────────────────────────

    void OnUpgradeClicked()
    {
        if (selectedTower == null || !selectedTower.CanUpgrade()) return;
        int cost = selectedTower.GetUpgradeCost();
        if (!GameManager.Instance.SpendGold(cost)) return;

        selectedTower.ApplyUpgrade();
        AudioManager.PlayUpgrade();
        // Actualizar el círculo de rango por si cambió con el upgrade
        selectedTower.ShowRangeIndicator();
        Refresh();
    }

    void OnSellClicked()
    {
        if (selectedTower == null) return;
        AudioManager.PlaySellTower();
        selectedTower.SellTower();
        Hide();
    }

    void CycleTargeting(int dir)
    {
        if (selectedTower == null) return;
        int count = System.Enum.GetValues(typeof(TargetingMode)).Length;
        selectedTower.targeting = (TargetingMode)(((int)selectedTower.targeting + dir + count) % count);
        if (_targetingLabel != null)
            _targetingLabel.text = _targetingNames[(int)selectedTower.targeting];
    }

    void Refresh()
    {
        if (selectedTower == null) return;

        bool canUpgrade = selectedTower.CanUpgrade();
        int  cost       = canUpgrade ? selectedTower.GetUpgradeCost() : 0;
        bool canAfford  = canUpgrade && GameManager.Instance.gold >= cost;

        // Título y nivel
        titleText.text = selectedTower.data.towerName;
        levelText.text = $"Niv. {selectedTower.upgradeLevel} / {Tower.MAX_LEVEL}";

        // Stats actuales
        statsText.text =
            $"Daño:           {selectedTower.currentDamage}\n" +
            $"Rango:          {selectedTower.currentRange:F1}\n" +
            $"Vel. disparo:   {selectedTower.data.fireRate:F1} / s";

        // Targeting label
        if (_targetingLabel != null)
            _targetingLabel.text = _targetingNames[(int)selectedTower.targeting];

        // Botón
        if (!canUpgrade)
        {
            upgradeBtnText.text      = "NIVEL MAXIMO";
            upgradeBtnImage.color    = COLOR_MAX;
            upgradeBtn.interactable  = false;
        }
        else
        {
            upgradeBtnText.text     = $"Mejorar  -  {cost} oro";
            upgradeBtnImage.color   = canAfford ? COLOR_CAN_BUY : COLOR_CANT_BUY;
            upgradeBtn.interactable = canAfford;
        }

        if (sellBtnText != null)
            sellBtnText.text = $"Vender  +  {selectedTower.GetSellPrice()} oro";
    }

    // ── Construcción de la UI en código ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas (Screen Space Overlay)
        Canvas canvas        = gameObject.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 99;
        CanvasScaler scaler  = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode   = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // Asegurar EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Panel centrado 680 x 730
        panel = MakeImage("Panel", transform, COLOR_PANEL, new Vector2(680, 730));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;

        // Franja superior oscura (header)
        GameObject header = MakeImage("Header", panel.transform,
            new Color(0.04f, 0.04f, 0.06f, 1f), new Vector2(680, 100));
        SetAnchors(header, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1));
        header.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Título
        titleText = MakeText("Title", header.transform, "Torre", 40, FontStyles.Bold,
            new Vector2(0, 0), new Vector2(640, 100), TextAlignmentOptions.Center);

        // Cuerpo del panel
        Transform body = panel.transform;

        // Nivel + estrellas
        levelText = MakeText("Level", body, "", 26, FontStyles.Normal,
            new Vector2(0, 160), new Vector2(620, 40), TextAlignmentOptions.Center);
        levelText.color = new Color(0.8f, 0.75f, 0.3f);

        // Línea divisoria
        GameObject div = MakeImage("Div", body, COLOR_DIVIDER, new Vector2(600, 3));
        div.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 118);
        SetAnchors(div, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                        new Vector2(0.5f,0.5f));

        // Stats
        statsText = MakeText("Stats", body, "", 28, FontStyles.Normal,
            new Vector2(0, 45), new Vector2(600, 160), TextAlignmentOptions.Left);
        statsText.color = new Color(0.85f, 0.85f, 0.85f);
        statsText.lineSpacing = 12;

        // ── Fila de targeting ─────────────────────────────────────────────────
        // Divisor
        GameObject div2 = MakeImage("Div2", body, COLOR_DIVIDER, new Vector2(600, 2));
        div2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -52);
        SetAnchors(div2, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Botón anterior ◄
        GameObject prevGO = MakeImage("TargetPrev", body,
            new Color(0.18f, 0.18f, 0.24f, 1f), new Vector2(48, 40));
        prevGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-264, -88);
        SetAnchors(prevGO, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        Button prevBtn = prevGO.AddComponent<Button>();
        prevBtn.targetGraphic = prevGO.GetComponent<Image>();
        MakeText("Txt", prevGO.transform, "◄", 22, FontStyles.Bold,
            Vector2.zero, new Vector2(48, 40), TextAlignmentOptions.Center)
            .color = new Color(0.78f, 0.85f, 1f);
        prevBtn.onClick.AddListener(() => CycleTargeting(-1));

        // Label del modo
        _targetingLabel = MakeText("TargetLabel", body, "Más cercano", 23, FontStyles.Normal,
            new Vector2(0, -88), new Vector2(450, 40), TextAlignmentOptions.Center);
        _targetingLabel.color = new Color(0.75f, 0.88f, 1f);

        // Botón siguiente ►
        GameObject nextGO = MakeImage("TargetNext", body,
            new Color(0.18f, 0.18f, 0.24f, 1f), new Vector2(48, 40));
        nextGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(264, -88);
        SetAnchors(nextGO, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        Button nextBtn = nextGO.AddComponent<Button>();
        nextBtn.targetGraphic = nextGO.GetComponent<Image>();
        MakeText("Txt", nextGO.transform, "►", 22, FontStyles.Bold,
            Vector2.zero, new Vector2(48, 40), TextAlignmentOptions.Center)
            .color = new Color(0.78f, 0.85f, 1f);
        nextBtn.onClick.AddListener(() => CycleTargeting(1));

        // Divisor inferior
        GameObject div3 = MakeImage("Div3", body, COLOR_DIVIDER, new Vector2(600, 2));
        div3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -118);
        SetAnchors(div3, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Botón de mejora
        GameObject btnGO = MakeImage("UpgradeBtn", body, COLOR_CAN_BUY, new Vector2(580, 80));
        btnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
        SetAnchors(btnGO, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                          new Vector2(0.5f,0.5f));

        upgradeBtnImage = btnGO.GetComponent<Image>();
        upgradeBtn      = btnGO.AddComponent<Button>();
        upgradeBtn.targetGraphic = upgradeBtnImage;

        ColorBlock cb    = upgradeBtn.colors;
        cb.highlightedColor = new Color(1,1,1,0.15f);
        cb.pressedColor     = new Color(0,0,0,0.2f);
        upgradeBtn.colors   = cb;
        upgradeBtn.onClick.AddListener(OnUpgradeClicked);

        upgradeBtnText = MakeText("BtnText", btnGO.transform, "Mejorar", 28, FontStyles.Bold,
            Vector2.zero, new Vector2(580, 80), TextAlignmentOptions.Center);
        upgradeBtnText.color = Color.white;

        // Separador entre upgrade y sell
        GameObject sellDiv = MakeImage("SellDiv", body, COLOR_DIVIDER, new Vector2(580, 2));
        sellDiv.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -272);
        SetAnchors(sellDiv, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // Botón de venta
        GameObject sellGO = MakeImage("SellBtn", body, COLOR_SELL, new Vector2(580, 70));
        sellGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -326);
        SetAnchors(sellGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        sellBtnImage = sellGO.GetComponent<Image>();
        sellBtn      = sellGO.AddComponent<Button>();
        sellBtn.targetGraphic = sellBtnImage;

        ColorBlock scb       = sellBtn.colors;
        scb.highlightedColor = new Color(0.75f, 0.22f, 0.22f, 1f);
        scb.pressedColor     = new Color(0.35f, 0.05f, 0.05f, 1f);
        sellBtn.colors       = scb;
        sellBtn.onClick.AddListener(OnSellClicked);

        sellBtnText = MakeText("SellText", sellGO.transform, "Vender", 26, FontStyles.Bold,
            Vector2.zero, new Vector2(580, 70), TextAlignmentOptions.Center);
        sellBtnText.color = Color.white;
    }

    // ── Helpers de construcción ───────────────────────────────────────────────

    static GameObject MakeImage(string goName, Transform parent, Color color, Vector2 size)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return go;
    }

    static TextMeshProUGUI MakeText(string goName, Transform parent, string text,
        int fontSize, FontStyles style, Vector2 anchoredPos, Vector2 size,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color     = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = size;
        rt.anchoredPosition = anchoredPos;
        return tmp;
    }

    static void SetAnchors(GameObject go, Vector2 min, Vector2 max, Vector2 pivot)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot     = pivot;
    }
}
