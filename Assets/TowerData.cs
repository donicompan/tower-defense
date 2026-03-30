using UnityEngine;

public enum TowerType { Normal, Cactus, Arena, Sol, Tormenta }

[CreateAssetMenu(fileName = "TowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    public string    towerName;
    public TowerType towerType = TowerType.Normal;
    public int       cost;
    public float     range;
    public float     fireRate;
    public int       damage;
    public Color     towerColor;

    [Header("Cactus — Veneno")]
    public int   poisonDps      = 0;
    public float poisonDuration = 0f;

    [Header("Arena — Ralentización")]
    [Range(0f, 1f)]
    public float slowFactor   = 0f;
    public float slowDuration = 0f;

    // Sol usa damage + range + fireRate directamente (sin campos extra)

    [Header("Tormenta — Cadena")]
    public int   chainCount  = 0;   // saltos extra (0 = sin cadena)
    public float chainRange  = 7f;
    public int   chainDamage = 0;

    [Header("Modelo 3D")]
    [Tooltip("Modelo Kenney que se instancia como hijo de la torre. Si es null se usa el cubo por defecto.")]
    public GameObject modelPrefab;
    [Tooltip("Escala del modelo respecto al hijo instanciado.")]
    public float modelScale = 3f;

    [Header("Animación Idle")]
    [Tooltip("FlagWave=bandera, CannonBob=cañón sube/baja, SniperScan=gira buscando. None=solo apunta.")]
    public TowerAnim idleAnim = TowerAnim.None;
}

public enum TowerAnim { None, FlagWave, CannonBob, SniperScan }
