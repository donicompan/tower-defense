using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    // Si se asigna un EnemyData, sobreescribe los campos individuales en Start
    public EnemyData data;

    public float speed      = 3f;
    public int   health     = 50;
    public int   goldReward = 10;
    public int   damage     = 1;
    public GameObject shadowPrefab;

    private Transform[]    waypoints;
    private int            waypointIndex = 0;
    public  int            WaypointIndex => waypointIndex;
    private bool           isDead        = false;
    private GameObject     shadow;
    private int            terrainMask;
    private EnemyHealthBar healthBar;
    private float          nextTowerAttackTime;

    // ── Estado de efectos ──────────────────────────────────────────────────────
    private float _originalSpeed;
    private bool  _isSlowed    = false;
    private bool  _isPoisoned  = false;
    private int   _poisonDps   = 0;
    private float     _poisonEndTime  = 0f;
    private Coroutine _slowCoroutine  = null;

    // ── Flash de hit ──────────────────────────────────────────────────────────
    private Renderer[] _renderers;
    private Color[]    _originalColors;

    // ── Registro estático (evita FindObjectsByType en cada frame) ────────────
    public static readonly List<Enemy> All = new List<Enemy>();
    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Snapshot sin allocación: seguro aunque All se modifique durante la iteración
    private static readonly List<Enemy> _snapshot = new List<Enemy>();
    public static List<Enemy> Snapshot()
    {
        _snapshot.Clear();
        _snapshot.AddRange(All);
        return _snapshot;
    }

    void Start()
    {
        // Aplicar stats del ScriptableObject si está asignado
        if (data != null)
        {
            speed       = data.speed;
            health      = data.health;
            goldReward  = data.goldReward;
            damage      = data.damage;
            transform.localScale = Vector3.one * data.scale;
        }
        _originalSpeed = speed;

        waypoints   = WaypointPath.Instance.points;
        terrainMask = LayerMask.GetMask("Terrain");

        // Barra de vida
        GameObject barGO = new GameObject("HealthBar");
        barGO.transform.SetParent(transform, false);
        barGO.transform.localPosition = new Vector3(0f, 3.0f / (data?.scale ?? 1f), 0f);
        healthBar = barGO.AddComponent<EnemyHealthBar>();
        healthBar.Init(health, data?.isBoss ?? false, data?.scale ?? 1f);

        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, transform.position, shadowPrefab.transform.rotation);
            shadow.transform.SetParent(transform);
            UpdateShadowY();
        }

        ApplyEnemyColor();
        CacheRenderers();
    }

    void ApplyEnemyColor()
    {
        if (data == null) return;
        Color c;
        string n = data.enemyName;
        if      (n.Contains("Fugitivo"))  c = new Color(1.00f, 0.90f, 0.05f);   // amarillo brillante
        else if (n.Contains("Carreta"))   c = new Color(0.32f, 0.32f, 0.32f);   // gris oscuro
        else if (n.Contains("Pistolero")) c = new Color(1.00f, 0.45f, 0.05f);   // naranja
        else if (n == "Boss")             c = new Color(0.85f, 0.04f, 0.04f);   // rojo intenso
        else return;                                                               // básico: color original

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (shadow != null && r.transform.IsChildOf(shadow.transform)) continue;
            r.material.color = c;
        }
    }

    void CacheRenderers()
    {
        var allR = GetComponentsInChildren<Renderer>();
        var list = new System.Collections.Generic.List<Renderer>(allR.Length);
        foreach (var r in allR)
        {
            // Excluir la sombra y la barra de vida
            if (shadow != null && r.transform.IsChildOf(shadow.transform)) continue;
            if (r.GetComponentInParent<EnemyHealthBar>() != null) continue;
            list.Add(r);
        }
        _renderers = list.ToArray();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].material.color;
    }

    void UpdateShadowY()
    {
        if (shadow == null) return;
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, terrainMask))
            shadow.transform.position = new Vector3(transform.position.x, hit.point.y + 0.02f, transform.position.z);
    }

    void Update()
    {
        if (isDead) return;
        if (waypoints == null || waypoints.Length == 0) return;

        if (waypointIndex >= waypoints.Length)
        {
            ReachEnd();
            return;
        }

        // Mover solo en XZ hacia el waypoint — la Y siempre viene del terreno,
        // así el enemigo nunca flota aunque los waypoints estén mal posicionados.
        Vector3 wayPos   = waypoints[waypointIndex].position;
        Vector3 targetXZ = new Vector3(wayPos.x, transform.position.y, wayPos.z);
        Vector3 dir      = targetXZ - transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position, targetXZ, speed * Time.deltaTime);

        // Anclar Y al terreno y actualizar sombra en un solo raycast
        if (Physics.Raycast(transform.position + Vector3.up * 10f,
                            Vector3.down, out RaycastHit hit, 30f, terrainMask))
        {
            float groundY = hit.point.y;
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            if (shadow != null)
                shadow.transform.position = new Vector3(transform.position.x, groundY + 0.02f, transform.position.z);
        }

        // Orientar solo en XZ (sin inclinar hacia arriba/abajo)
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(flatDir), 10f * Time.deltaTime);

        // Avanzar al siguiente waypoint por distancia XZ
        float distXZ = new Vector2(transform.position.x - wayPos.x,
                                   transform.position.z - wayPos.z).magnitude;
        if (distXZ < 0.15f)
            waypointIndex++;

        if (data != null && data.attacksTowers)
            HandleTowerAttack();
    }

    // ── Forajido Pistolero ────────────────────────────────────────────────────

    void HandleTowerAttack()
    {
        if (Time.time < nextTowerAttackTime) return;

        Tower closest = null;
        float minDist = data.towerDetectRange;

        foreach (Tower t in Tower.Snapshot())
        {
            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDist) { minDist = dist; closest = t; }
        }

        if (closest != null)
        {
            closest.TakeDamage(data.towerDamage);
            nextTowerAttackTime = Time.time + 1f / data.towerAttackRate;
        }
    }

    // ── Efectos de torres especiales ──────────────────────────────────────────

    public void ApplyPoison(int dps, float duration)
    {
        _poisonDps     = dps;
        _poisonEndTime = Mathf.Max(_poisonEndTime, Time.time + duration);
        if (!_isPoisoned) StartCoroutine(PoisonTick());
    }

    IEnumerator PoisonTick()
    {
        _isPoisoned = true;
        while (Time.time < _poisonEndTime && !isDead)
        {
            yield return new WaitForSeconds(0.5f);
            if (!isDead)
            {
                int dmg = Mathf.RoundToInt(_poisonDps * 0.5f);
                TakeDamage(dmg);
                // Número de daño verde para diferenciar veneno
                ShowPoisonNumber(dmg);
            }
        }
        _isPoisoned = false;
    }

    void ShowPoisonNumber(int dmg)
    {
        GameObject go = new GameObject("PoisonNumber");
        go.transform.position = transform.position + Vector3.up * 2f;
        DamageNumber dn = go.AddComponent<DamageNumber>();
        dn.Init(dmg, new Color(0.1f, 0.9f, 0.1f));
    }

    public void ApplySlow(float factor, float duration)
    {
        // Si ya hay un slow activo y el nuevo es más débil, no pisarlo
        if (_isSlowed && speed <= _originalSpeed * (1f - factor) + 0.001f) return;
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        _slowCoroutine = StartCoroutine(SlowCoroutine(factor, duration));
    }

    IEnumerator SlowCoroutine(float factor, float duration)
    {
        _isSlowed = true;
        speed     = _originalSpeed * (1f - factor);
        yield return new WaitForSeconds(duration);
        speed          = _originalSpeed;
        _isSlowed      = false;
        _slowCoroutine = null;
    }

    // ── Daño y muerte ─────────────────────────────────────────────────────────

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        health -= dmg;
        healthBar?.Refresh(health);

        GameObject go = new GameObject("DamageNumber");
        go.transform.position = transform.position + Vector3.up * 1.5f;
        go.AddComponent<DamageNumber>().Init(dmg);

        AudioManager.PlayEnemyHit();
        if (_renderers != null && _renderers.Length > 0)
            StartCoroutine(HitFlash());

        if (health <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].material.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].material.color = _originalColors[i];
    }

    void Die()
    {
        isDead = true;
        if (shadow != null) Destroy(shadow);
        if (data != null && data.isBoss)
            SpawnBossDeathParticles(transform.position + Vector3.up * 1.0f);
        else
            SpawnDeathParticles(transform.position + Vector3.up * 0.5f);
        AudioManager.PlayEnemyDeath();
        GameManager.Instance.AddGold(goldReward);
        ScoreManager.Instance?.AddKill(data);
        Destroy(gameObject);
    }

    void ReachEnd()
    {
        isDead = true;
        if (shadow != null) Destroy(shadow);
        for (int i = 0; i < damage; i++)
            GameManager.Instance.LoseLife();
        Destroy(gameObject);
    }

    // ── Partículas de muerte ──────────────────────────────────────────────────

    static void SpawnDeathParticles(Vector3 position)
    {
        GameObject go = new GameObject("DeathParticles");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = false;
        main.duration        = 0.4f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 7f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.gravityModifier = 2.8f;
        main.maxParticles    = 30;
        main.stopAction      = ParticleSystemStopAction.Destroy;
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.55f, 0f), new Color(1f, 0.08f, 0.02f));

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.25f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (GameMaterials.DeathParticle != null)
            rend.sharedMaterial = GameMaterials.DeathParticle;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
    }

    // ── Partículas de muerte del Boss ─────────────────────────────────────────

    static void SpawnBossDeathParticles(Vector3 position)
    {
        GameObject go = new GameObject("BossDeathParticles");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = false;
        main.duration        = 0.7f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 14f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.gravityModifier = 1.2f;
        main.maxParticles    = 60;
        main.stopAction      = ParticleSystemStopAction.Destroy;
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.1f), new Color(1f, 0.55f, 0.0f));

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 55) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.8f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                    new GradientColorKey(Color.white,               1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (GameMaterials.BossDeathParticle != null)
            rend.sharedMaterial = GameMaterials.BossDeathParticle;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
