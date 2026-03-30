using UnityEngine;

/// <summary>
/// Sistema de partículas de arena/polvo que sopla horizontalmente.
/// Intensidad del viento varía suavemente con el tiempo.
/// Colocá este componente en un GameObject vacío en el centro del mapa.
/// </summary>
public class DesertWindParticles : MonoBehaviour
{
    [Header("Área de emisión")]
    public float areaSize   = 120f;  // cubre todo el mapa
    public float areaHeight = 5f;    // hasta qué altura emite polvo

    [Header("Viento")]
    public Vector2 windDirXZ          = new Vector2(1f, 0.15f); // dirección principal
    public float   windSpeedMin       = 3f;
    public float   windSpeedMax       = 14f;
    public float   windChangeInterval = 12f;  // segundos entre cambios de viento

    [Header("Partículas")]
    public int maxParticles = 600;

    private ParticleSystem _ps;
    private float          _currentWind;
    private float          _targetWind;
    private float          _windTimer;

    void Start()
    {
        _currentWind = 0.35f;
        _targetWind  = Random.Range(0.3f, 0.7f);

        BuildParticleSystem();
    }

    void Update()
    {
        // Cambio periódico de intensidad de viento
        _windTimer += Time.deltaTime;
        if (_windTimer >= windChangeInterval)
        {
            _windTimer = 0f;

            // Ráfagas ocasionales (20% de probabilidad)
            _targetWind = Random.value < 0.2f
                ? Random.Range(0.75f, 1f)        // ráfaga fuerte
                : Random.Range(0.15f, 0.65f);    // viento normal
        }

        _currentWind = Mathf.Lerp(_currentWind, _targetWind, Time.deltaTime * 0.8f);

        ApplyWind();
    }

    void ApplyWind()
    {
        // Solo modificar emission.rateOverTime — no genera warning.
        // La velocidad de las partículas se setea una vez en BuildParticleSystem().
        var emission = _ps.emission;
        emission.rateOverTime = Mathf.Lerp(15f, 140f, _currentWind);
    }

    void BuildParticleSystem()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();
        // Detener antes de configurar evita "Setting duration while system is playing"
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main ─────────────────────────────────────────────────────────────
        var main = _ps.main;
        main.loop             = true;
        main.duration         = 5f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(1.8f, 5.5f);
        main.startSpeed       = 0f;           // velocidad vía velocityOverLifetime
        main.startSize        = new ParticleSystem.MinMaxCurve(0.04f, 0.22f);
        main.maxParticles     = maxParticles;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = 0.03f;        // casi sin gravedad: polvo suspendido
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        // Color arena: beige cálido
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.82f, 0.71f, 0.48f, 0.0f),
            new Color(0.96f, 0.89f, 0.68f, 0.22f));

        // ── Shape (caja grande a ras del suelo) ──────────────────────────────
        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(areaSize, areaHeight, areaSize);
        shape.position  = new Vector3(0f, areaHeight * 0.5f, 0f);

        // ── Velocity over lifetime (fija — solo emission varía en Update) ──────
        Vector2 dir = windDirXZ.normalized;
        float   s   = Mathf.Lerp(windSpeedMin, windSpeedMax, 0.5f); // velocidad media
        var vel = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(dir.x * s * 0.75f, dir.x * s);
        vel.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.15f);
        vel.z = new ParticleSystem.MinMaxCurve(dir.y * s * 0.75f, dir.y * s);

        // ── Color over lifetime: fade in/out ─────────────────────────────────
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.92f, 0.83f, 0.62f), 0f),
                new GradientColorKey(new Color(0.92f, 0.83f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(0.30f, 0.15f),
                new GradientAlphaKey(0.30f, 0.80f),
                new GradientAlphaKey(0.00f, 1.00f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // ── Size over lifetime: decrece al final ─────────────────────────────
        var sol = _ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

        // ── Noise: turbulencia suave ──────────────────────────────────────────
        var noise = _ps.noise;
        noise.enabled   = true;
        noise.strength  = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.3f;
        noise.quality   = ParticleSystemNoiseQuality.Low;

        // ── Renderer ─────────────────────────────────────────────────────────
        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        Material mat = GameMaterials.SandCloud;
        if (mat != null) rend.sharedMaterial = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = -1;     // que quede detrás de los personajes

        _ps.Play();
    }
}
