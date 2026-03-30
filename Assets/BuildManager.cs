using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    public GameObject towerPrefab;
    public TowerData[] availableTowers;

    private int  _selectedIndex  = 0;
    private bool _hasSelection   = false;

    public bool HasTowerSelected => _hasSelection;

    /// <summary>Devuelve el TowerData actualmente seleccionado, o null si no hay selección.</summary>
    public TowerData SelectedTower =>
        _hasSelection && availableTowers != null && _selectedIndex < availableTowers.Length
            ? availableTowers[_selectedIndex]
            : null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SelectTower(int index)
    {
        if (index < availableTowers.Length)
        {
            _selectedIndex = index;
            _hasSelection  = true;
        }
    }

    public void CancelSelection() => _hasSelection = false;

    // ── Construcción libre (llamado por FreeBuildManager) ─────────────────────

    /// <summary>Construye la torre seleccionada en <paramref name="pos"/> y devuelve la instancia, o null si falló.</summary>
    public Tower BuildAt(Vector3 pos)
    {
        if (!_hasSelection || availableTowers.Length == 0) return null;

        TowerData selected = availableTowers[_selectedIndex];

        if (GameManager.Instance.SpendGold(selected.cost))
        {
            GameObject go    = Instantiate(towerPrefab, pos, Quaternion.identity);
            Tower      tower = go.GetComponent<Tower>();
            tower.Init(selected);   // Init sincrónico: evita race condition con Start()
            AudioManager.PlayBuildTower();
            return tower;
        }
        // No cancelamos la selección: el jugador puede seguir colocando torres
        return null;
    }

    // ── Compatibilidad con BuildSlots existentes (no se usa con FreeBuildManager)

    public void BuildOnSlot(BuildSlot slot)
    {
        if (FreeBuildManager.Instance != null) return;   // sistema nuevo activo
        if (!_hasSelection || slot.IsOccupied() || availableTowers.Length == 0) return;

        TowerData selected = availableTowers[_selectedIndex];

        if (GameManager.Instance.SpendGold(selected.cost))
        {
            Vector3    pos   = slot.transform.position + Vector3.up * 1.5f;
            GameObject go    = Instantiate(towerPrefab, pos, Quaternion.identity);
            Tower      tower = go.GetComponent<Tower>();
            tower.mySlot = slot;
            tower.Init(selected);   // Init sincrónico: evita race condition con Start()
            slot.SetOccupied();
            AudioManager.PlayBuildTower();
        }
    }
}
