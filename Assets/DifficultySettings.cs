/// <summary>
/// Configuración de dificultad elegida en el menú principal.
/// Persiste en memoria estática entre escenas durante la misma sesión.
/// </summary>
public static class DifficultySettings
{
    public enum Level { Easy, Normal, Hard }

    public static Level Current = Level.Normal;

    // ── Condiciones iniciales ─────────────────────────────────────────────────
    public static int StartLives => Current == Level.Easy ? 30  : Current == Level.Hard ? 10  : 20;
    public static int StartGold  => Current == Level.Easy ? 300 : Current == Level.Hard ? 100 : 150;

    // ── Multiplicadores de stats de enemigos ──────────────────────────────────
    public static float EnemySpeedMult  => Current == Level.Easy ? 0.80f : Current == Level.Hard ? 1.30f : 1.00f;
    public static float EnemyHealthMult => Current == Level.Easy ? 0.70f : Current == Level.Hard ? 1.40f : 1.00f;
    public static float EnemyGoldMult   => Current == Level.Easy ? 1.20f : Current == Level.Hard ? 0.80f : 1.00f;

    public static string DisplayName    => Current == Level.Easy ? "Fácil" : Current == Level.Hard ? "Difícil" : "Normal";
}
