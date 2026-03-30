using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("General")]
    public string enemyName   = "Enemigo";
    public float  speed       = 3f;
    public int    health      = 50;
    public int    goldReward  = 10;
    public int    damage      = 1;      // vidas que quita al llegar al final
    public float  scale       = 1f;    // escala del modelo (Carreta > 1)

    [Header("Forajido Pistolero")]
    public bool  attacksTowers     = false;
    public float towerDetectRange  = 8f;
    public int   towerDamage       = 10;
    public float towerAttackRate   = 0.8f; // ataques por segundo

    [Header("Boss")]
    public bool isBoss = false;

    [Header("Modelo 3D")]
    public GameObject modelPrefab;
    public float      modelScale = 1f;
}
