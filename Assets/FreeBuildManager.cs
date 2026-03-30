using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Sistema de construcción libre estilo Warcraft 3.
/// Al seleccionar una torre y mover el mouse sobre el terreno aparece un fantasma
/// verde (válido) o rojo (inválido). Click izquierdo construye; click derecho / ESC cancela.
///
/// Reglas de validación:
///   - Distancia mínima al camino de waypoints (XZ)
///   - Pendiente máxima permitida
///
/// Al construir, coloca un quad plano debajo de la torre como base visual.
/// El terreno real nunca se modifica.
/// </summary>
[DefaultExecutionOrder(60)]
public class FreeBuildManager : MonoBehaviour
{
    public static FreeBuildManager Instance;

    [Header("Validación")]
    public float minDistToPath    = 3f;   // distancia mínima al camino (unidades mundo)
    public float maxSlopeAngle    = 20f;  // ángulo máximo de pendiente (grados)
    public float minDistToTower   = 2f;   // distancia mínima entre torres

    [Header("Construcción")]
    public float towerYOffset     = 3.0f;  // altura sobre el suelo al instanciar
    public float baseSize         = 2.4f;  // tamaño del quad de base bajo la torre
    public Color baseColor        = new Color(0.55f, 0.42f, 0.28f, 1f); // arena compacta

    [Header("Terreno dinámico")]
    public float flatRadius  = 1.5f;   // zona completamente plana (unidades mundo)
    public float blendRadius = 1.5f;   // anillo de transición suave

    // ── Estado interno ────────────────────────────────────────────────────────
    private GameObject _ghost;
    private Renderer   _ghostRend;
    private Material   _matOk;
    private Material   _matBad;
    private int        _terrainMask;
    private bool       _isValid;
    private bool       _slopeTooSteep;
    private TextMeshProUGUI _slopeText;

    // ── Inicialización ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _terrainMask = LayerMask.GetMask("Terrain");

        _matOk  = MakeGhostMat(new Color(0.1f, 1f, 0.15f, 0.45f));
        _matBad = MakeGhostMat(new Color(1f,   0.1f, 0.1f, 0.45f));

        CreateSlopeWarningText();
    }

    void CreateSlopeWarningText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("SlopeWarning");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 40f);
        rt.anchoredPosition = new Vector2(0f, -80f);   // bajo el centro de pantalla

        _slopeText           = go.AddComponent<TextMeshProUGUI>();
        _slopeText.text      = "Terreno muy inclinado";
        _slopeText.fontSize  = 20f;
        _slopeText.fontStyle = FontStyles.Bold;
        _slopeText.alignment = TextAlignmentOptions.Center;
        _slopeText.color     = new Color(1f, 0.25f, 0.15f, 1f);
        _slopeText.gameObject.SetActive(false);
    }

    static Material MakeGhostMat(Color c)
    {
        var m = GameMaterials.MakeUnlit(c);
        if (m == null) return null;
        m.renderQueue = 3000;
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite",   0);
        m.EnableKeyword("_ALPHABLEND_ON");
        return m;
    }

    // ── Helpers de input unificado (Mouse + Touch) ────────────────────────────

    Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.None &&
                    phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                    phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                    return touch.position.ReadValue();
            }
        }
        return Mouse.current?.position.ReadValue() ?? Vector2.zero;
    }

    bool WasPrimaryPressedThisFrame(out Vector2 pressPos)
    {
        pressPos = Vector2.zero;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressPos = Mouse.current.position.ReadValue();
            return true;
        }
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    pressPos = touch.position.ReadValue();
                    return true;
                }
            }
        }
        return false;
    }

    bool WasSecondaryPressedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
               (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
    }

    /// <summary>
    /// Comprueba si una posición de pantalla está sobre algún elemento de UI.
    /// Usa RaycastAll para ser compatible con mouse y touch sin depender de pointer IDs.
    /// </summary>
    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        var ped = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current) { position = screenPos };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }

    // ── Update: raycast + preview + click ────────────────────────────────────

    void Update()
    {
        if (BuildManager.Instance == null || !BuildManager.Instance.HasTowerSelected)
        {
            DestroyGhost();
            return;
        }

        // No raycastear a través de la UI (funciona con mouse y touch)
        if (IsPointerOverUI(GetPointerPosition()))
        {
            HideGhost();
            return;
        }

        // Fix: click sobre torre existente → cancelar construcción y ceder el click a Tower
        if (WasPrimaryPressedThisFrame(out Vector2 towerCheckPos) && Camera.main != null)
        {
            Ray tr = Camera.main.ScreenPointToRay(towerCheckPos);
            if (Physics.Raycast(tr, out RaycastHit th, 500f) && th.collider.GetComponent<Tower>() != null)
            {
                BuildManager.Instance.CancelSelection();
                DestroyGhost();
                return;
            }
        }

        if (Camera.main == null) { HideGhost(); return; }
        Ray ray = Camera.main.ScreenPointToRay(GetPointerPosition());

        if (!RaycastTerrain(ray, out RaycastHit hit))
        {
            HideGhost();
            return;
        }

        // Posicionar ghost
        EnsureGhost();
        _ghost.transform.position = hit.point + Vector3.up * towerYOffset;
        _ghost.SetActive(true);

        _isValid = ValidatePlacement(hit);
        _ghostRend.sharedMaterial = _isValid ? _matOk : _matBad;

        if (_slopeText != null)
            _slopeText.gameObject.SetActive(!_isValid && _slopeTooSteep);

        // ── Click / tap: construir ────────────────────────────────────────────
        if (WasPrimaryPressedThisFrame(out _) && _isValid)
        {
            // Verificar oro ANTES de modificar terreno o crear objetos
            TowerData selected = BuildManager.Instance.SelectedTower;
            if (selected == null || GameManager.Instance.gold < selected.cost)
            {
                AudioManager.PlayNoGold();
                UIManager.Instance.ShowAnnouncement(
                    "¡Oro insuficiente!", 1.5f, new Color(1f, 0.20f, 0.20f));
                return;   // quad y terreno intactos
            }

            TerrainManager.FlattenAt(hit.point, flatRadius, blendRadius);
            GameObject towerBase = SpawnBase(hit.point);
            Tower tower = BuildManager.Instance.BuildAt(hit.point + Vector3.up * towerYOffset);
            if (tower != null && towerBase != null)
                towerBase.transform.SetParent(tower.transform);
            DestroyGhost();
            return;
        }

        // ── Click derecho / ESC / back: cancelar ──────────────────────────────
        if (WasSecondaryPressedThisFrame())
        {
            BuildManager.Instance.CancelSelection();
            DestroyGhost();
            return;
        }
    }

    // ── Raycast al terreno con fallback ───────────────────────────────────────

    bool RaycastTerrain(Ray ray, out RaycastHit hit)
    {
        if (_terrainMask != 0 && Physics.Raycast(ray, out hit, 500f, _terrainMask))
            return true;

        if (Physics.Raycast(ray, out hit, 500f))
            return hit.collider is TerrainCollider;

        hit = default;
        return false;
    }

    // ── Validación de posición ────────────────────────────────────────────────

    bool ValidatePlacement(RaycastHit hit)
    {
        // 1. Pendiente
        _slopeTooSteep = Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle;
        if (_slopeTooSteep) return false;

        // 2. Distancia al camino de waypoints (XZ)
        if (WaypointPath.Instance?.points != null)
        {
            var pts = WaypointPath.Instance.points;
            Vector3 p = new Vector3(hit.point.x, 0f, hit.point.z);

            for (int i = 0; i < pts.Length - 1; i++)
            {
                if (pts[i] == null || pts[i + 1] == null) continue;
                float d = DistToSegmentXZ(p,
                    new Vector3(pts[i].position.x,     0f, pts[i].position.z),
                    new Vector3(pts[i + 1].position.x, 0f, pts[i + 1].position.z));
                if (d < minDistToPath) return false;
            }

            // También verificar el último waypoint como punto individual
            if (pts.Length > 0 && pts[pts.Length - 1] != null)
            {
                float dx = pts[pts.Length - 1].position.x - hit.point.x;
                float dz = pts[pts.Length - 1].position.z - hit.point.z;
                if (Mathf.Sqrt(dx * dx + dz * dz) < minDistToPath) return false;
            }
        }

        // 3. Distancia mínima entre torres
        foreach (Tower t in Tower.Snapshot())
        {
            float dx = t.transform.position.x - hit.point.x;
            float dz = t.transform.position.z - hit.point.z;
            if (Mathf.Sqrt(dx * dx + dz * dz) < minDistToTower) return false;
        }

        return true;
    }

    static float DistToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        if (ab.sqrMagnitude < 0.001f)
            return new Vector2(p.x - a.x, p.z - a.z).magnitude;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector3 c = a + t * ab;
        return new Vector2(p.x - c.x, p.z - c.z).magnitude;
    }

    // ── Base visual bajo la torre (no modifica el terreno) ───────────────────

    /// <summary>
    /// Crea el quad de base y lo devuelve sin padre.
    /// El caller debe parentarlo a la torre para que se destruya con ella.
    /// </summary>
    GameObject SpawnBase(Vector3 groundPos)
    {
        // Quad orientado horizontalmente, ligeramente sobre el suelo para evitar z-fighting
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "TowerBase";
        go.transform.position   = groundPos + Vector3.up * 0.02f;
        go.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * baseSize;

        // Sin collider — no interfiere con raycasts ni enemigos
        Destroy(go.GetComponent<Collider>());

        Material mat = GameMaterials.MakeLit(baseColor);
        if (mat != null) go.GetComponent<Renderer>().material = mat;

        return go;
    }

    // ── Ghost visual ──────────────────────────────────────────────────────────

    void EnsureGhost()
    {
        if (_ghost != null) return;

        _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ghost.name = "TowerGhost";
        _ghost.transform.localScale = new Vector3(1f, 2f, 1f);

        // El ghost no debe interferir con raycasts ni física
        Destroy(_ghost.GetComponent<Collider>());

        _ghostRend = _ghost.GetComponent<Renderer>();
        _ghostRend.sharedMaterial = _matOk;
        _ghost.SetActive(false);
    }

    /// <summary>Oculta el ghost temporalmente (cursor sobre UI, sin hit de terreno).</summary>
    void HideGhost()
    {
        if (_ghost != null) _ghost.SetActive(false);
        if (_slopeText != null) _slopeText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Destruye el ghost por completo al salir del modo build.
    /// Evita que quede geometría huérfana (forma roja) visible en escena.
    /// </summary>
    void DestroyGhost()
    {
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        if (_slopeText != null) _slopeText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_ghost != null) Destroy(_ghost);
    }
}
