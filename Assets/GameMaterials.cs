using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Materiales compartidos para bullets, VFX y decoraciones de torres.
/// Cargados desde Resources/GameMaterials/ para garantizar que los shaders URP
/// están incluidos en el build Android (Shader.Find falla si el shader fue stripped).
/// </summary>
public static class GameMaterials
{
    // ── Bases cargadas desde Resources ───────────────────────────────────────
    // Los .mat en Resources/GameMaterials/ son el ancla: obligan al build a
    // incluir URP/Lit, URP/Unlit y URP/Particles/Unlit en el APK.

    static Material _litBase;
    static Material _unlitBase;
    static Material _particlesBase;

    static Material LitBase
    {
        get
        {
            if (_litBase == null)
                _litBase = Resources.Load<Material>("GameMaterials/Lit_White");
            return _litBase;
        }
    }

    static Material UnlitBase
    {
        get
        {
            if (_unlitBase == null)
                _unlitBase = Resources.Load<Material>("GameMaterials/Unlit_White");
            return _unlitBase;
        }
    }

    static Material ParticlesBase
    {
        get
        {
            if (_particlesBase == null)
                _particlesBase = Resources.Load<Material>("GameMaterials/ParticlesUnlit_White");
            return _particlesBase;
        }
    }

    // ── Bullets (por tipo de torre) ───────────────────────────────────────────
    private static Material _bulletNormal;
    private static Material _bulletCactus;
    private static Material _bulletArena;
    private static Material _bulletTormenta;

    public static Material BulletNormal    => _bulletNormal    ??= MakeLit(Color.yellow);
    public static Material BulletCactus    => _bulletCactus    ??= MakeLit(new Color(0.15f, 0.9f,  0.15f));
    public static Material BulletArena     => _bulletArena     ??= MakeLit(new Color(1f,    0.85f, 0.3f));
    public static Material BulletTormenta  => _bulletTormenta  ??= MakeLit(new Color(0.5f,  0.5f,  1f));

    public static Material GetBulletMaterial(TowerType type) => type switch
    {
        TowerType.Cactus   => BulletCactus,
        TowerType.Arena    => BulletArena,
        TowerType.Tormenta => BulletTormenta,
        _                  => BulletNormal,
    };

    // ── VFX (LineRenderers y partículas) ──────────────────────────────────────
    private static Material _heatRay;
    private static Material _lightningArc;
    private static Material _deathParticle;
    private static Material _bossDeathParticle;
    private static Material _sandCloud;

    public static Material HeatRay           => _heatRay           ??= MakeUnlit(new Color(1f,   0.45f, 0.05f));
    public static Material LightningArc      => _lightningArc      ??= MakeUnlit(new Color(0.7f, 0.7f,  1f));
    public static Material DeathParticle     => _deathParticle     ??= MakeParticle(Color.white);
    public static Material BossDeathParticle => _bossDeathParticle ??= MakeParticle(Color.white);
    public static Material SandCloud         => _sandCloud         ??= MakeParticle(Color.white);

    // ── Decoraciones de torre ─────────────────────────────────────────────────
    private static readonly Dictionary<Color, Material> _litCache = new Dictionary<Color, Material>();

    public static Material GetLitCached(Color c)
    {
        if (!_litCache.TryGetValue(c, out Material m))
        {
            m = MakeLit(c);
            if (m != null) _litCache[c] = m;
        }
        return m;
    }

    // ── Fábricas internas ─────────────────────────────────────────────────────

    /// <summary>Material URP/Lit coloreado — para objetos 3D y bullets.</summary>
    public static Material MakeLit(Color c)
    {
        Material src = LitBase;
        if (src == null)
        {
            // Fallback de emergencia: Shader.Find solo funciona en Editor/PC
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) { Debug.LogWarning("[GameMaterials] Lit_White no encontrado en Resources y URP/Lit no disponible."); return null; }
            src = new Material(sh);
        }
        else
        {
            src = new Material(src);
        }
        src.SetColor("_BaseColor", c);
        src.color = c;
        return src;
    }

    /// <summary>Material URP/Unlit coloreado — para LineRenderers.</summary>
    public static Material MakeUnlit(Color c)
    {
        Material src = UnlitBase;
        if (src == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) { Debug.LogWarning("[GameMaterials] Unlit_White no encontrado en Resources y URP/Unlit no disponible."); return null; }
            src = new Material(sh);
        }
        else
        {
            src = new Material(src);
        }
        src.SetColor("_BaseColor", c);
        return src;
    }

    /// <summary>Material URP/Particles/Unlit coloreado — para ParticleSystemRenderer.</summary>
    static Material MakeParticle(Color c)
    {
        Material src = ParticlesBase;
        if (src == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) { Debug.LogWarning("[GameMaterials] ParticlesUnlit_White no encontrado en Resources y URP/Particles/Unlit no disponible."); return null; }
            src = new Material(sh);
        }
        else
        {
            src = new Material(src);
        }
        src.SetColor("_BaseColor", c);
        return src;
    }
}
