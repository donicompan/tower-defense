using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform  spawnPoint;
    public float      timeBetweenWaves  = 5f;
    public float      timeBetweenSpawns = 0.8f;

    [Header("Tipos de enemigo")]
    [Tooltip("Índice 0 = Básico, 1 = Fugitivo, 2 = Carreta, 3 = Forajido Pistolero")]
    public EnemyData[] enemyTypes;

    [Header("Boss")]
    [Tooltip("Asignar TT_demo_female para usar el modelo ToonyTinyPeople")]
    public GameObject bossPrefab;

    public static WaveManager Instance { get; private set; }

    private int       waveNumber = 0;
    private EnemyData _bossData;
    private bool      _skipCountdown = false;

    /// <summary>Llama desde un botón UI para adelantar la próxima oleada.</summary>
    public void RequestSkip() => _skipCountdown = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _bossData = CreateBossData();
        StartCoroutine(RunWaves());
    }

    EnemyData CreateBossData()
    {
        EnemyData d  = ScriptableObject.CreateInstance<EnemyData>();
        d.enemyName  = "Boss";
        d.speed      = 2.5f;
        d.health     = 250;
        d.goldReward = 50;
        d.damage     = 3;
        d.scale      = 2.5f;
        d.isBoss     = true;
        d.modelPrefab = bossPrefab;
        d.modelScale  = 1f;
        return d;
    }

    // ── Bucle principal ───────────────────────────────────────────────────────

    IEnumerator RunWaves()
    {
        while (true)
        {
            // Preview de la próxima oleada al inicio del countdown
            if (!GameManager.Instance.gameOver && waveNumber < 10)
                UIManager.Instance?.ShowAnnouncement(
                    BuildWavePreview(waveNumber + 1), 2.8f,
                    new Color(0.45f, 0.88f, 1f));

            // Countdown visible entre oleadas
            _skipCountdown = false;
            for (float t = timeBetweenWaves; t > 0f; t -= 1f)
            {
                UIManager.Instance.ShowCountdown(Mathf.CeilToInt(t));
                yield return new WaitForSeconds(Mathf.Min(1f, t));
                if (GameManager.Instance.gameOver || _skipCountdown) break;
            }
            _skipCountdown = false;
            UIManager.Instance.HideCountdown();
            if (GameManager.Instance.gameOver) yield break;

            waveNumber++;
            GameManager.Instance.wave = waveNumber;
            UIManager.Instance.UpdateWave(waveNumber);
            AudioManager.PlayWaveStart();

            // Anunciar inicio del modo endless al llegar a la oleada 11
            if (waveNumber == 11)
                UIManager.Instance.ShowAnnouncement("¡ MODO ENDLESS !", 3f);

            // Boss cada 5 oleadas (aparece ANTES que los regulares)
            if (waveNumber % 5 == 0)
                yield return StartCoroutine(SpawnBoss());

            // Oleada principal
            int enemyCount = 3 + waveNumber * 2;
            yield return StartCoroutine(SpawnWave(enemyCount));

            AudioManager.PlayWaveComplete();

            // Victoria: completar las 10 oleadas (incluido el boss de la oleada 10)
            if (waveNumber == 10)
            {
                yield return StartCoroutine(WaitForAllEnemiesDead());
                if (!GameManager.Instance.gameOver)
                    GameManager.Instance.TriggerVictory();
                yield break;
            }

            // Modo endless: sin condición de victoria — el juego continúa mientras
            // queden vidas. Game Over lo maneja GameManager.LoseLife().
        }
    }

    IEnumerator WaitForAllEnemiesDead()
    {
        while (Enemy.All.Count > 0)
            yield return new WaitForSeconds(0.5f);
    }

    // ── Scaling dinámico ──────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve una copia de <paramref name="original"/> con stats escalados según
    /// la dificultad elegida y el escalado progresivo del modo endless (post oleada 10).
    /// Si no hay escala que aplicar devuelve el original sin copiar.
    /// Las instancias creadas en tiempo de ejecución no se serializan al disco.
    /// </summary>
    EnemyData ApplyScaling(EnemyData original)
    {
        float sm = DifficultySettings.EnemySpeedMult;
        float hm = DifficultySettings.EnemyHealthMult;
        float gm = DifficultySettings.EnemyGoldMult;

        // Endless: +4 % velocidad, +10 % vida, +5 % recompensa por oleada sobre 10
        if (waveNumber > 10)
        {
            int over = waveNumber - 10;
            sm *= 1f + over * 0.04f;
            hm *= 1f + over * 0.10f;
            gm *= 1f + over * 0.05f;
        }

        // Optimización: devolver original si no hay cambio
        if (Mathf.Approximately(sm, 1f) &&
            Mathf.Approximately(hm, 1f) &&
            Mathf.Approximately(gm, 1f))
            return original;

        EnemyData d = ScriptableObject.CreateInstance<EnemyData>();
        if (original != null)
        {
            d.enemyName        = original.enemyName;
            d.speed            = original.speed       * sm;
            d.health           = Mathf.RoundToInt(original.health      * hm);
            d.goldReward       = Mathf.RoundToInt(original.goldReward  * gm);
            d.damage           = original.damage;
            d.scale            = original.scale;
            d.isBoss           = original.isBoss;
            d.attacksTowers    = original.attacksTowers;
            d.towerDetectRange = original.towerDetectRange;
            d.towerDamage      = original.towerDamage;
            d.towerAttackRate  = original.towerAttackRate;
        }
        else
        {
            d.speed      = 3f * sm;
            d.health     = Mathf.RoundToInt(50 * hm);
            d.goldReward = Mathf.RoundToInt(10 * gm);
        }
        return d;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    // Intervalo de spawn: empieza en timeBetweenSpawns, reduce 0.025s por oleada, mínimo 0.25s
    float GetSpawnInterval() => Mathf.Max(0.25f, timeBetweenSpawns - waveNumber * 0.025f);

    IEnumerator SpawnWave(int count)
    {
        float interval = GetSpawnInterval();
        for (int i = 0; i < count; i++)
        {
            if (GameManager.Instance.gameOver) yield break;
            GameObject go = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            go.GetComponent<Enemy>().data = ApplyScaling(PickEnemyType());
            yield return new WaitForSeconds(interval);
        }
    }

    string BuildWavePreview(int nextWave)
    {
        int  count   = 3 + nextWave * 2;
        bool hasBoss = nextWave % 5 == 0;
        string boss  = hasBoss ? "¡BOSS! + " : "";
        return $"Oleada {nextWave}  —  {boss}{count} enemigos";
    }

    IEnumerator SpawnBoss()
    {
        if (GameManager.Instance.gameOver) yield break;
        yield return new WaitForSeconds(1.5f);
        if (GameManager.Instance.gameOver) yield break;

        BossAnnouncement.Show();

        yield return new WaitForSeconds(0.8f);
        if (GameManager.Instance.gameOver) yield break;

        GameObject go = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        go.GetComponent<Enemy>().data = ApplyScaling(_bossData);
    }

    // ── Selección de tipo ─────────────────────────────────────────────────────
    // Oleadas 1-2 : solo básico.
    // Oleada 3-4  : básico + fugitivo.
    // Oleada 5-6  : básico + fugitivo + carreta.
    // Oleada 7+   : todos los tipos.

    EnemyData PickEnemyType()
    {
        if (enemyTypes == null || enemyTypes.Length == 0) return null;

        int available;
        if      (waveNumber <= 2) available = 1;
        else if (waveNumber <= 4) available = Mathf.Min(2, enemyTypes.Length);
        else if (waveNumber <= 6) available = Mathf.Min(3, enemyTypes.Length);
        else                      available = enemyTypes.Length;

        return enemyTypes[Random.Range(0, available)];
    }
}
