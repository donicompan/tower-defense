using UnityEngine;

// Garantiza que GameManager se inicialice antes que cualquier otro script.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // HideInInspector evita que valores serializados obsoletos (ej. gold=1000)
    // confundan el Inspector — OnEnable los reinicia siempre al valor correcto.
    [HideInInspector] public int  gold;
    [HideInInspector] public int  lives;
    [HideInInspector] public int  wave;
    [HideInInspector] public bool gameOver;
    [HideInInspector] public bool victory;

    // ── Singleton ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ── Reset de estado (OnEnable se llama siempre tras Awake, y también si el
    //    GO se re-activa). Con DefaultExecutionOrder(-100) esto corre antes que
    //    cualquier otro manager, garantizando un estado limpio desde el frame 0. ──

    void OnEnable()
    {
        // Sobreescribe cualquier valor serializado que haya quedado de una
        // sesión anterior (p.ej. gameOver=true por auto-save del Editor).
        gameOver = false;
        victory  = false;
        wave     = 0;
        gold     = DifficultySettings.StartGold;
        lives    = DifficultySettings.StartLives;

        TerrainManager.Backup();
    }

    void Start()
    {
        TutorialManager.ShowIfNeeded();
    }

    public void AddGold(int amount)
    {
        if (gameOver) return;
        gold += amount;
        UIManager.Instance.UpdateGold(gold);
    }

    public bool SpendGold(int amount)
    {
        if (gameOver) return false;
        if (gold >= amount)
        {
            gold -= amount;
            UIManager.Instance.UpdateGold(gold);
            return true;
        }
        AudioManager.PlayNoGold();
        return false;
    }

    public void TriggerVictory()
    {
        if (gameOver || victory) return;
        victory = true;
        gameOver = true;
        bool isRecord = ScoreManager.Instance?.SaveRun(wave) ?? false;
        VictoryUI.Instance.ShowVictory(wave, gold, lives, isRecord);
        if (isRecord) NewRecordAnnouncement.Show(ScoreManager.Instance.CurrentScore);
    }

    public void LoseLife()
    {
        if (gameOver) return;
        if (wave == 0) return;   // el juego aún no comenzó; ignora daño prematuro
        lives--;
        UIManager.Instance.UpdateLives(lives);
        AudioManager.PlayLoseLife();
        CameraController.Instance?.Shake(0.15f, 0.3f);
        if (lives <= 0)
        {
            lives = 0;
            gameOver = true;
            UIManager.Instance.UpdateLives(0);
            bool isRecord = ScoreManager.Instance?.SaveRun(wave) ?? false;
            GameOverUI.Instance.ShowGameOver(wave);
            if (isRecord) NewRecordAnnouncement.Show(ScoreManager.Instance.CurrentScore);
        }
    }
}