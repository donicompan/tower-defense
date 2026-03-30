using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public float    speed         = 15f;
    public int      damage        = 20;

    // Efecto al impactar
    public TowerType effectType   = TowerType.Normal;
    public int       poisonDps    = 0;
    public float     poisonDuration = 0f;
    public float     slowFactor   = 0f;
    public float     slowDuration = 0f;

    // Cadena (Tormenta)
    public int   chainCount  = 0;
    public float chainRange  = 7f;
    public int   chainDamage = 0;

    // Enemigos ya golpeados en esta cadena (para no rebotar al mismo)
    public List<Enemy> hitEnemies = new List<Enemy>();

    private Enemy target;

    public void SetTarget(Enemy t)
    {
        target = t;
        if (t != null) hitEnemies.Add(t);
    }

    void Update()
    {
        if (target == null) { ReturnToPool(); return; }

        Vector3 dir = target.transform.position - transform.position;
        transform.position += dir.normalized * speed * Time.deltaTime;

        if (dir.magnitude <= 0.35f)
            OnHit();
    }

    void OnHit()
    {
        if (target == null || !target.gameObject.activeInHierarchy) { ReturnToPool(); return; }
        target.TakeDamage(damage);
        ApplySpecialEffect(target);

        if (effectType == TowerType.Tormenta && chainCount > 0)
            Chain(target.transform.position);
        else if (effectType == TowerType.Arena)
            SpawnSandCloud(transform.position);

        ReturnToPool();
    }

    void ReturnToPool()
    {
        // Limpiar estado antes de devolver al pool
        target = null;
        hitEnemies.Clear();
        speed        = 15f;
        damage       = 0;
        effectType   = TowerType.Normal;
        poisonDps    = 0;
        poisonDuration = 0f;
        slowFactor   = 0f;
        slowDuration = 0f;
        chainCount   = 0;
        chainRange   = 7f;
        chainDamage  = 0;
        BulletPool.Instance.Return(gameObject);
    }

    // ── Efectos especiales ────────────────────────────────────────────────────

    void ApplySpecialEffect(Enemy e)
    {
        if (effectType == TowerType.Cactus && poisonDps > 0)
            e.ApplyPoison(poisonDps, poisonDuration);
        else if (effectType == TowerType.Arena && slowFactor > 0f)
            e.ApplySlow(slowFactor, slowDuration);
    }

    // ── Cadena eléctrica ──────────────────────────────────────────────────────

    void Chain(Vector3 fromPos)
    {
        Enemy next = FindNextChainTarget(fromPos);
        if (next == null) return;

        SpawnLightningArc(fromPos, next.transform.position);

        GameObject go = BulletPool.Instance.Get(0.2f, GameMaterials.BulletTormenta);
        go.transform.position = fromPos;

        Bullet chainBullet   = go.GetComponent<Bullet>();
        chainBullet.speed        = 22f;
        chainBullet.damage       = chainDamage;
        chainBullet.effectType   = TowerType.Tormenta;
        chainBullet.chainCount   = chainCount - 1;
        chainBullet.chainRange   = chainRange;
        chainBullet.chainDamage  = chainDamage;
        chainBullet.hitEnemies   = new List<Enemy>(hitEnemies);
        chainBullet.SetTarget(next);
    }

    Enemy FindNextChainTarget(Vector3 fromPos)
    {
        Enemy closest = null;
        float minDist = chainRange;

        foreach (Enemy e in Enemy.Snapshot())
        {
            if (hitEnemies.Contains(e)) continue;
            float d = Vector3.Distance(fromPos, e.transform.position);
            if (d < minDist) { minDist = d; closest = e; }
        }
        return closest;
    }

    static void SpawnLightningArc(Vector3 from, Vector3 to)
    {
        GameObject go = new GameObject("LightningArc");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = lr.endWidth = 0.06f;

        if (GameMaterials.LightningArc != null)
            lr.sharedMaterial = GameMaterials.LightningArc;
        lr.startColor = lr.endColor = new Color(0.7f, 0.7f, 1f, 0.9f);
        Destroy(go, 0.1f);
    }

    // ── Nube de arena (Arena) ─────────────────────────────────────────────────

    static void SpawnSandCloud(Vector3 position)
    {
        GameObject go = new GameObject("SandCloud");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop          = false;
        main.duration      = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.gravityModifier = 0.2f;
        main.maxParticles  = 20;
        main.stopAction    = ParticleSystemStopAction.Destroy;
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.82f, 0.45f),
            new Color(1f,    0.92f, 0.60f));

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.3f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.55f), 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f,   1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (GameMaterials.SandCloud != null)
            rend.sharedMaterial = GameMaterials.SandCloud;
    }
}
