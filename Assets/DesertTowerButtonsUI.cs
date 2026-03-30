using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Crea los TowerData de las 4 torres desierto y los botones de selección.
/// Agregá SOLO este script a cualquier GameObject de la escena.
/// </summary>
[DefaultExecutionOrder(50)]   // corre después de BuildManager.Awake() (order 0)
public class DesertTowerButtonsUI : MonoBehaviour
{
    // ── Modelos 3D (asignar en el Inspector arrastrando los FBX de Kenney) ─────
    [Header("Modelos 3D (Kenney Tower Defense Kit)")]
    [Tooltip("detail-tree-large.fbx")]
    public GameObject modelCactus;
    [Tooltip("tower-round-bottom-a.fbx")]
    public GameObject modelArena;
    [Tooltip("tower-round-crystals.fbx")]
    public GameObject modelSol;
    [Tooltip("weapon-ballista.fbx")]
    public GameObject modelTormenta;

    // ── Datos de cada torre ───────────────────────────────────────────────────

    private struct TowerDef
    {
        public string   name;
        public string   costLabel;
        public Color    accent;
        public TowerData data;
    }

    private TowerDef[] _defs;

    // ── Seguimiento de botones para actualización en tiempo real ─────────────
    private struct ButtonEntry { public Button btn; public Image bg; public int cost; public Color baseColor; }
    private readonly List<ButtonEntry> _entries = new List<ButtonEntry>();

    // ── Botón de cancelar selección ──────────────────────────────────────────
    private GameObject _cancelBtnGO;
    private GameObject _skipBtnGO;

    // ── Layout — más grandes para toque cómodo ────────────────────────────────
    private static readonly Vector2 BtnSize    = new Vector2(210f, 64f);  // subido de 175x52
    private const float BtnGap      = 12f;
    private const float MarginRight  = 16f;
    private const float MarginBottom = 70f;

    // ── Paleta ────────────────────────────────────────────────────────────────
    private static readonly Color C_DarkBg   = new Color(0.05f, 0.06f, 0.10f, 0.88f);
    private static readonly Color C_Disabled = new Color(0.4f,  0.4f,  0.4f,  0.55f);
    private static readonly Color C_Gold     = new Color(1.00f, 0.85f, 0.15f, 1f);
    private static readonly Color C_White    = Color.white;
    private static readonly Color C_Cancel   = new Color(0.90f, 0.22f, 0.18f, 1f);
    private static readonly Color C_Skip     = new Color(0.20f, 0.75f, 0.35f, 1f);

    // ── 1. Crear TowerData ────────────────────────────────────────────────────

    static void AssignModel(TowerData td, GameObject prefab, float scale)
    {
        td.modelPrefab = prefab;
        td.modelScale  = scale;
    }

    void Awake()
    {
        TowerData cactusData   = MakeCactus();
        TowerData arenaData    = MakeArena();
        TowerData solData      = MakeSol();
        TowerData tormentaData = MakeTormenta();

        AssignModel(cactusData,   modelCactus,   3f);
        AssignModel(arenaData,    modelArena,    3f);
        AssignModel(solData,      modelSol,      3f);
        AssignModel(tormentaData, modelTormenta, 3f);

        _defs = new[]
        {
            new TowerDef { name = "CACTUS",   costLabel = "80",  accent = new Color(0.18f, 0.72f, 0.30f), data = cactusData   },
            new TowerDef { name = "ARENA",    costLabel = "100", accent = new Color(0.88f, 0.65f, 0.10f), data = arenaData    },
            new TowerDef { name = "SOL",      costLabel = "150", accent = new Color(1.00f, 0.40f, 0.05f), data = solData      },
            new TowerDef { name = "TORMENTA", costLabel = "200", accent = new Color(0.35f, 0.42f, 0.95f), data = tormentaData },
        };

        if (BuildManager.Instance == null) return;

        var towers = new List<TowerData>(BuildManager.Instance.availableTowers);
        foreach (var def in _defs)
            towers.Add(def.data);
        BuildManager.Instance.availableTowers = towers.ToArray();
    }

    // ── 2. Crear botones en el Canvas ─────────────────────────────────────────

    void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        int baseIndex = BuildManager.Instance.availableTowers.Length - _defs.Length;

        for (int i = 0; i < _defs.Length; i++)
        {
            float y     = MarginBottom + ((_defs.Length - 1 - i) * (BtnSize.y + BtnGap));
            Vector2 pos = new Vector2(-(BtnSize.x * 0.5f + MarginRight), y);
            CreateButton(canvas.transform, _defs[i], baseIndex + i, pos);
        }

        CreateCancelButton(canvas.transform);
        CreateSkipButton(canvas.transform);
    }

    // ── Botón CANCELAR selección (visible solo con torre seleccionada) ────────

    void CreateCancelButton(Transform parent)
    {
        float totalH = _defs.Length * (BtnSize.y + BtnGap) - BtnGap;
        float y      = MarginBottom + totalH + BtnGap + 4f;
        Vector2 pos  = new Vector2(-(BtnSize.x * 0.5f + MarginRight), y);

        // Sombra
        GameObject shadowGO = new GameObject("CancelShadow");
        shadowGO.transform.SetParent(parent, false);
        RectTransform srt = shadowGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(1f, 0f); srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(1f, 0f);
        srt.sizeDelta = new Vector2(BtnSize.x + 6f, 46f);
        srt.anchoredPosition = pos + new Vector2(3f, -3f);
        UIHelper.Img(shadowGO, new Color(0f, 0f, 0f, 0.4f));

        // Botón
        _cancelBtnGO = new GameObject("CancelSelectionBtn");
        _cancelBtnGO.transform.SetParent(parent, false);
        RectTransform rt = _cancelBtnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(BtnSize.x, 46f);
        rt.anchoredPosition = pos;

        Image bg   = UIHelper.Img(_cancelBtnGO, new Color(0.45f, 0.07f, 0.07f, 0.92f));
        Button btn = _cancelBtnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.65f, 0.10f, 0.10f, 1f);
        cb.pressedColor     = new Color(0.25f, 0.04f, 0.04f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => BuildManager.Instance?.CancelSelection());

        // Barra de acento izquierda roja
        GameObject stripe = new GameObject("Stripe");
        stripe.transform.SetParent(_cancelBtnGO.transform, false);
        RectTransform strt = stripe.AddComponent<RectTransform>();
        strt.anchorMin = new Vector2(0f, 0f); strt.anchorMax = new Vector2(0f, 1f);
        strt.pivot = new Vector2(0f, 0.5f);
        strt.sizeDelta = new Vector2(8f, -4f);
        strt.anchoredPosition = Vector2.zero;
        UIHelper.Img(stripe, C_Cancel);

        // Texto
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_cancelBtnGO.transform, false);
        RectTransform lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI lbl = labelGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "✕  CANCELAR";
        lbl.fontSize  = 15f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.color     = new Color(1f, 0.70f, 0.70f);

        _cancelBtnGO.SetActive(false);  // oculto por defecto
        shadowGO.SetActive(false);

        // Guardar sombra como hijo para poder mostrar/ocultar juntos
        shadowGO.transform.SetParent(_cancelBtnGO.transform.parent, false);
        _cancelBtnGO.name = "CancelSelectionBtn";
        // Guardar referencia a sombra vía tag
        shadowGO.tag = "CancelShadow";
    }

    // ── Botón SKIP countdown (visible solo durante el countdown) ─────────────

    void CreateSkipButton(Transform parent)
    {
        // Centro inferior de pantalla
        _skipBtnGO = new GameObject("SkipWaveBtn");
        _skipBtnGO.transform.SetParent(parent, false);

        RectTransform rt = _skipBtnGO.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0f);
        rt.anchorMax       = new Vector2(0.5f, 0f);
        rt.pivot           = new Vector2(0.5f, 0f);
        rt.sizeDelta       = new Vector2(240f, 52f);
        rt.anchoredPosition = new Vector2(0f, 20f);

        Image bg   = UIHelper.Img(_skipBtnGO, new Color(0.05f, 0.28f, 0.12f, 0.90f));
        Button btn = _skipBtnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.10f, 0.50f, 0.22f, 1f);
        cb.pressedColor     = new Color(0.03f, 0.15f, 0.07f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => WaveManager.Instance?.RequestSkip());

        // Barra acento verde
        GameObject stripe = new GameObject("Stripe");
        stripe.transform.SetParent(_skipBtnGO.transform, false);
        RectTransform strt = stripe.AddComponent<RectTransform>();
        strt.anchorMin = new Vector2(0f, 0f); strt.anchorMax = new Vector2(0f, 1f);
        strt.pivot = new Vector2(0f, 0.5f);
        strt.sizeDelta = new Vector2(8f, -4f);
        strt.anchoredPosition = Vector2.zero;
        UIHelper.Img(stripe, C_Skip);

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_skipBtnGO.transform, false);
        RectTransform lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(16f, 0f); lrt.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI lbl = labelGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "▶  ENVIAR OLEADA";
        lbl.fontSize  = 16f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.color     = new Color(0.65f, 1f, 0.72f);

        _skipBtnGO.SetActive(false);
    }

    // ── Construcción de un botón de torre ─────────────────────────────────────

    void CreateButton(Transform parent, TowerDef def, int towerIndex, Vector2 anchoredPos)
    {
        // Sombra
        GameObject shadowGO = new GameObject("Shadow_" + def.name);
        shadowGO.transform.SetParent(parent, false);
        RectTransform shdRT = shadowGO.AddComponent<RectTransform>();
        shdRT.anchorMin = new Vector2(1f, 0f);
        shdRT.anchorMax = new Vector2(1f, 0f);
        shdRT.pivot     = new Vector2(1f, 0f);
        shdRT.sizeDelta = BtnSize + new Vector2(6f, 6f);
        shdRT.anchoredPosition = anchoredPos + new Vector2(3f, -3f);
        UIHelper.Img(shadowGO, new Color(0f, 0f, 0f, 0.4f));

        // Borde exterior
        GameObject borderGO = new GameObject("Border_" + def.name);
        borderGO.transform.SetParent(parent, false);
        RectTransform bRT = borderGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(1f, 0f);
        bRT.anchorMax = new Vector2(1f, 0f);
        bRT.pivot     = new Vector2(1f, 0f);
        bRT.sizeDelta = BtnSize + new Vector2(2f, 2f);
        bRT.anchoredPosition = anchoredPos;
        Color borderColor = new Color(def.accent.r * 0.6f, def.accent.g * 0.6f, def.accent.b * 0.6f, 0.8f);
        UIHelper.Img(borderGO, borderColor);

        // Fondo principal oscuro
        GameObject btnGO = new GameObject("BtnDesierto_" + def.name);
        btnGO.transform.SetParent(parent, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta = BtnSize;
        rt.anchoredPosition = anchoredPos;

        Image bg = UIHelper.Img(btnGO, C_DarkBg);

        // Barra de acento izquierda
        GameObject stripeGO = new GameObject("AccentStripe");
        stripeGO.transform.SetParent(btnGO.transform, false);
        RectTransform stRT = stripeGO.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f);
        stRT.anchorMax = new Vector2(0f, 1f);
        stRT.pivot     = new Vector2(0f, 0.5f);
        stRT.sizeDelta = new Vector2(8f, -4f);
        stRT.anchoredPosition = Vector2.zero;
        UIHelper.Img(stripeGO, def.accent);

        // Componente Button
        Button btn    = btnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = C_DarkBg;
        cb.highlightedColor = new Color(def.accent.r * 0.25f, def.accent.g * 0.25f, def.accent.b * 0.30f, 1f);
        cb.pressedColor     = new Color(def.accent.r * 0.15f, def.accent.g * 0.15f, def.accent.b * 0.20f, 1f);
        cb.selectedColor    = cb.highlightedColor;
        cb.disabledColor    = C_Disabled;
        cb.fadeDuration     = 0.08f;
        btn.colors          = cb;

        int capturedIndex = towerIndex;
        btn.onClick.AddListener(() =>
        {
            if (BuildManager.Instance != null)
                BuildManager.Instance.SelectTower(capturedIndex);
        });

        // Nombre de la torre
        GameObject nameGO = new GameObject("TowerName");
        nameGO.transform.SetParent(btnGO.transform, false);
        RectTransform nrt = nameGO.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0.45f);
        nrt.anchorMax = Vector2.one;
        nrt.offsetMin = new Vector2(18f, 0f);
        nrt.offsetMax = new Vector2(-8f, 0f);
        var nameTmp   = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = def.name;
        nameTmp.fontSize  = 17f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.color     = C_White;

        // Costo
        GameObject costGO = new GameObject("CostLabel");
        costGO.transform.SetParent(btnGO.transform, false);
        RectTransform crt = costGO.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 0.50f);
        crt.offsetMin = new Vector2(18f, 0f);
        crt.offsetMax = new Vector2(-8f, 0f);
        var costTmp   = costGO.AddComponent<TextMeshProUGUI>();
        costTmp.text      = def.costLabel + " oro";
        costTmp.fontSize  = 14f;
        costTmp.alignment = TextAlignmentOptions.MidlineLeft;
        costTmp.color     = C_Gold;

        _entries.Add(new ButtonEntry { btn = btn, bg = bg, cost = def.data.cost, baseColor = C_DarkBg });
    }

    // ── Update: interactable según oro + mostrar/ocultar Cancelar y Skip ──────

    void Update()
    {
        if (GameManager.Instance == null) return;

        int gold = GameManager.Instance.gold;

        // Actualizar estado de botones de torre
        foreach (var e in _entries)
        {
            if (e.btn == null) continue;
            e.btn.interactable = gold >= e.cost;
        }

        // Mostrar botón Cancelar solo cuando hay torre seleccionada
        bool hasSelection = BuildManager.Instance != null && BuildManager.Instance.HasTowerSelected;
        if (_cancelBtnGO != null && _cancelBtnGO.activeSelf != hasSelection)
        {
            _cancelBtnGO.SetActive(hasSelection);
            // Sombra
            var shadow = GameObject.FindWithTag("CancelShadow");
            if (shadow != null) shadow.SetActive(hasSelection);
        }

        // Mostrar botón Skip solo durante el countdown (UIManager tiene el countdown activo)
        if (_skipBtnGO != null)
        {
            bool countdownVisible = UIManager.Instance != null &&
                                    UIManager.Instance.IsCountdownVisible();
            if (_skipBtnGO.activeSelf != countdownVisible)
                _skipBtnGO.SetActive(countdownVisible);
        }
    }

    // ── Fábrica de TowerData ──────────────────────────────────────────────────

    static TowerData Make(string name, TowerType type,
                          int cost, float range, float fireRate, int damage, Color color)
    {
        TowerData d  = ScriptableObject.CreateInstance<TowerData>();
        d.towerName  = name;
        d.towerType  = type;
        d.cost       = cost;
        d.range      = range;
        d.fireRate   = fireRate;
        d.damage     = damage;
        d.towerColor = color;
        return d;
    }

    static TowerData MakeCactus()
    {
        TowerData d      = Make("Torre Cactus", TowerType.Cactus, 80, 8f, 1.2f, 5,
                                new Color(0.14f, 0.55f, 0.14f));
        d.poisonDps      = 8;
        d.poisonDuration = 3f;
        return d;
    }

    static TowerData MakeArena()
    {
        TowerData d    = Make("Torre Arena", TowerType.Arena, 100, 9f, 0.8f, 8,
                              new Color(0.85f, 0.72f, 0.22f));
        d.slowFactor   = 0.60f;
        d.slowDuration = 2f;
        return d;
    }

    static TowerData MakeSol()
    {
        return Make("Torre Sol", TowerType.Sol, 150, 7f, 0.45f, 22,
                    new Color(1f, 0.45f, 0.05f));
    }

    static TowerData MakeTormenta()
    {
        TowerData d   = Make("Torre Tormenta", TowerType.Tormenta, 200, 10f, 0.7f, 28,
                             new Color(0.38f, 0.38f, 0.85f));
        d.chainCount  = 2;
        d.chainRange  = 7f;
        d.chainDamage = 14;
        return d;
    }
}
