using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    public AudioClip backgroundMusic;

    [Header("Efectos de sonido — Torres")]
    public AudioClip shootSound;
    public AudioClip buildTowerSound;
    public AudioClip sellTowerSound;
    public AudioClip upgradeTowerSound;

    [Header("Efectos de sonido — Enemigos")]
    public AudioClip enemyDeathSound;
    public AudioClip enemyHitSound;

    [Header("Efectos de sonido — Oleadas")]
    public AudioClip waveStartSound;
    public AudioClip waveCompleteSound;

    [Header("Efectos de sonido — Jugador")]
    public AudioClip loseLifeSound;

    [Header("Efectos de sonido — UI")]
    public AudioClip noGoldSound;
    public AudioClip maxLevelSound;

    [Header("Volumen")]
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume   = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        musicSource             = gameObject.AddComponent<AudioSource>();
        musicSource.loop        = true;
        musicSource.playOnAwake = false;
        musicSource.volume      = musicVolume;

        sfxSource             = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume      = 1f;

        PlayMusic();
    }

    void PlayMusic()
    {
        if (backgroundMusic == null) return;
        if (musicSource == null) { Debug.LogError("[AudioManager] musicSource es null"); return; }
        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    // ── API estática ──────────────────────────────────────────────────────────

    public static void PlayShoot()
    {
        if (Instance == null || Instance.shootSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.shootSound, Instance.sfxVolume);
    }

    public static void PlayEnemyDeath()
    {
        if (Instance == null || Instance.enemyDeathSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.enemyDeathSound, Instance.sfxVolume);
    }

    public static void PlayBuildTower()
    {
        if (Instance == null || Instance.buildTowerSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.buildTowerSound, Instance.sfxVolume);
    }

    public static void PlaySellTower()
    {
        if (Instance == null || Instance.sellTowerSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.sellTowerSound, Instance.sfxVolume);
    }

    public static void PlayUpgrade()
    {
        if (Instance == null || Instance.upgradeTowerSound == null) return;
        if (Instance.sfxSource == null) return;
        Instance.sfxSource.PlayOneShot(Instance.upgradeTowerSound, Instance.sfxVolume);
    }

    public static void PlayNoGold()
    {
        if (Instance == null || Instance.noGoldSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.noGoldSound, Instance.sfxVolume);
    }

    public static void PlayMaxLevel()
    {
        if (Instance == null || Instance.maxLevelSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.maxLevelSound, Instance.sfxVolume);
    }

    public static void PlayWaveStart()
    {
        if (Instance == null || Instance.waveStartSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.waveStartSound, Instance.sfxVolume);
    }

    public static void PlayEnemyHit()
    {
        if (Instance == null || Instance.enemyHitSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.enemyHitSound, Instance.sfxVolume * 0.6f);
    }

    public static void PlayLoseLife()
    {
        if (Instance == null || Instance.loseLifeSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.loseLifeSound, Instance.sfxVolume);
    }

    public static void PlayWaveComplete()
    {
        if (Instance == null || Instance.waveCompleteSound == null) return;
        if (Instance.sfxSource == null) { Debug.LogError("[AudioManager] sfxSource es null"); return; }
        Instance.sfxSource.PlayOneShot(Instance.waveCompleteSound, Instance.sfxVolume);
    }
}
