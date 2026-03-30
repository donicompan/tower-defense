using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la puntuación de la partida actual y el leaderboard persistente (PlayerPrefs + JSON).
/// Puntos por kill = goldReward × 10  (Boss 50g = 500 pts, Básico 10g = 100 pts).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private const string PREFS_KEY     = "HighscoreData";
    private const int    MAX_ENTRIES   = 5;
    private const int    PTS_PER_GOLD  = 10;

    // ── Estado de la partida actual ───────────────────────────────────────────
    private int _score         = 0;
    private int _enemiesKilled = 0;
    private int _goldEarned    = 0;

    private TextMeshProUGUI _scoreText;

    public int CurrentScore  => _score;
    public int EnemiesKilled => _enemiesKilled;
    public int GoldEarned    => _goldEarned;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start() => CreateScoreHUD();

    // ── HUD de puntuación (creado en código sobre el Canvas existente) ─────────

    void CreateScoreHUD()
    {
        // Reutilizar el Canvas overlay de UIManager si existe
        Canvas hud = null;
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder < 50)
            { hud = c; break; }
        }

        if (hud == null)
        {
            hud = new GameObject("ScoreCanvas").AddComponent<Canvas>();
            hud.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.gameObject.AddComponent<CanvasScaler>();
        }

        // Texto centrado en la parte superior del HUD
        GameObject tgo  = new GameObject("ScoreText");
        tgo.transform.SetParent(hud.transform, false);

        RectTransform rt = tgo.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta        = new Vector2(300f, 44f);

        _scoreText           = tgo.AddComponent<TextMeshProUGUI>();
        _scoreText.text      = "Puntos: 0";
        _scoreText.fontSize  = 26f;
        _scoreText.fontStyle = FontStyles.Bold;
        _scoreText.alignment = TextAlignmentOptions.Center;
        _scoreText.color     = new Color(1f, 0.92f, 0.28f);

        var shadow = tgo.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    // ── Tracking durante la partida ───────────────────────────────────────────

    /// <summary>Registra un enemigo eliminado y suma puntos según su valor en oro.</summary>
    public void AddKill(EnemyData data)
    {
        if (data == null) return;

        int pts         = data.goldReward * PTS_PER_GOLD;
        _score         += pts;
        _enemiesKilled += 1;
        _goldEarned    += data.goldReward;

        if (_scoreText != null)
            _scoreText.text = "Puntos: " + _score;
    }

    // ── Guardado al terminar la partida ───────────────────────────────────────

    /// <summary>
    /// Guarda la partida en el leaderboard.
    /// Retorna true si la puntuación supera el récord anterior.
    /// </summary>
    public bool SaveRun(int wave)
    {
        int previousBest = GetBestScore();

        Leaderboard lb = LoadLeaderboard();

        var entry = new RunRecord
        {
            score         = _score,
            wave          = wave,
            goldEarned    = _goldEarned,
            enemiesKilled = _enemiesKilled,
            date          = System.DateTime.Now.ToString("dd/MM/yyyy")
        };

        lb.entries.Add(entry);
        lb.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (lb.entries.Count > MAX_ENTRIES)
            lb.entries.RemoveRange(MAX_ENTRIES, lb.entries.Count - MAX_ENTRIES);

        PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(lb));
        PlayerPrefs.Save();

        return _score > 0 && _score > previousBest;
    }

    // ── Acceso estático al leaderboard (funciona sin instancia en escena) ─────

    public static Leaderboard LoadLeaderboard()
    {
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(json)) return new Leaderboard();
        return JsonUtility.FromJson<Leaderboard>(json) ?? new Leaderboard();
    }

    public static int GetBestScore()
    {
        Leaderboard lb = LoadLeaderboard();
        return lb.entries.Count > 0 ? lb.entries[0].score : 0;
    }

    /// <summary>Retorna la entrada de mayor puntuación, o null si no hay partidas.</summary>
    public static RunRecord GetBestRecord()
    {
        Leaderboard lb = LoadLeaderboard();
        return lb.entries.Count > 0 ? lb.entries[0] : null;
    }
}
