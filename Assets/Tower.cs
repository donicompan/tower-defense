using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public enum TargetingMode { Nearest, First, Last, Strongest, Fastest }

public class Tower : MonoBehaviour
{
    public TowerData data;
    public int       maxTowerHealth = 150;

    [HideInInspector] public int       currentDamage;
    [HideInInspector] public float     currentRange;
    [HideInInspector] public int       upgradeLevel = 0;
    [HideInInspector] public BuildSlot mySlot;

    private bool _initialized = false;

    public const int MAX_LEVEL = 3;

    public  TargetingMode targeting   = TargetingMode.Nearest;

    private float nextFireTime = 0f;
    private Enemy currentTarget;
    private int   towerHealth;
    private LineRenderer _rangeCircle;

    // ── Animación procedural ──────────────────────────────────────────────────
    private Transform _model;             // hijo "TowerModel" (modelo Kenney)
    private Vector3   _modelBaseScale;    // escala actual (incluye upgrade) para pulso Sol
    private Vector3   _initialModelScale; // escala en Init() antes de upgrades
    private Vector3   _modelBasePos;      // posición local original para recoil
    private bool      _recoiling;         // evita solapar corrutinas de recoil

    // Francotirador (SniperScan)
    private float _scanAngle = 0f;

    // ── Inicialización ────────────────────────────────────────────────────────

    // ── Registro estático (evita FindObjectsByType en cada frame) ────────────
    public static readonly List<Tower> All = new List<Tower>();
    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Snapshot sin allocación: seguro aunque All se modifique durante la iteración
    private static readonly List<Tower> _snapshot = new List<Tower>();
    public static List<Tower> Snapshot()
    {
        _snapshot.Clear();
        _snapshot.AddRange(All);
        return _snapshot;
    }

    /// <summary>
    /// Inicialización explícita llamada por BuildManager inmediatamente después de Instantiate,
    /// antes de que Unity ejecute Start(). Esto evita el race condition donde Start() puede
    /// correr en el mismo frame que la instanciación, antes de que data sea asignado.
    /// </summary>
    public void Init(TowerData towerData)
    {
        if (_initialized) return;
        data         = towerData;
        _initialized = true;

        towerHealth   = maxTowerHealth;
        currentDamage = data != null ? data.damage : 0;
        currentRange  = data != null ? data.range  : 0f;

        if (data != null)
        {
            if (data.modelPrefab != null)
            {
                Renderer cubeRend = GetComponent<Renderer>();
                if (cubeRend != null) cubeRend.enabled = false;

                GameObject model = Instantiate(data.modelPrefab, transform);
                model.name = "TowerModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                float s = data.modelScale > 0f ? data.modelScale : 1f;
                model.transform.localScale = Vector3.one * s;

                // Los FBX de Kenney pueden traer Rigidbody/Collider embebidos:
                // un Rigidbody con gravedad hace que el modelo caiga y desaparezca
                // en zonas con pendiente o vacíos del terreno.
                foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true))
                    Destroy(rb);
                foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
                    Destroy(c);
            }
            else
            {
                Renderer r = GetComponent<Renderer>();
                if (r != null) r.material.color = data.towerColor;
            }
        }

        // Cachear referencia al modelo (prefab Kenney o cubo propio)
        _model             = transform.Find("TowerModel") ?? transform;
        _modelBaseScale    = _model.localScale;
        _initialModelScale = _model.localScale;   // referencia fija para escalar upgrades
        _modelBasePos      = _model.localPosition;
        _scanAngle      = transform.eulerAngles.y;

        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();
    }

    void Start()
    {
        // Fallback para torres pre-colocadas en escena o si alguien asignó tower.data
        // directamente sin llamar Init() — garantiza que siempre se inicialicen.
        if (!_initialized) Init(data);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        HandleClick();
        if (data == null) return;

        currentTarget = FindClosestEnemy();

        UpdateAnimations();

        if (currentTarget != null && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / data.fireRate;
        }
    }

    // ── Animaciones procedurales ──────────────────────────────────────────────

    void UpdateAnimations()
    {
        bool hasTarget = currentTarget != null;

        // ── Torre Tormenta: gira libre sin objetivo, apunta con objetivo ─────
        if (data.towerType == TowerType.Tormenta)
        {
            if (!hasTarget)
            {
                _model.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
            }
            else
            {
                // Resetear suavemente la rotación local del modelo al apuntar
                _model.localRotation = Quaternion.Slerp(
                    _model.localRotation, Quaternion.identity, 10f * Time.deltaTime);
                AimAtTarget();
            }
            goto AfterAim;
        }

        // ── Todas las demás torres apuntan al enemigo ─────────────────────────
        if (hasTarget)
            AimAtTarget();

        // ── Torre Sol: pulso de escala (0.95 ↔ 1.05) ─────────────────────────
        if (data.towerType == TowerType.Sol)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            _model.localScale = _modelBaseScale * pulse;
        }

        // ── Animaciones idle según TowerAnim ──────────────────────────────────

        // CannonBob: inclina el modelo levemente en X cuando no hay objetivo
        if (data.idleAnim == TowerAnim.CannonBob)
        {
            float targetX = hasTarget ? 0f : Mathf.Sin(Time.time * 1.2f) * 6f;
            Vector3 euler = _model.localEulerAngles;
            float currentX = euler.x > 180f ? euler.x - 360f : euler.x;
            float newX = Mathf.Lerp(currentX, targetX, 6f * Time.deltaTime);
            _model.localEulerAngles = new Vector3(newX, euler.y, euler.z);
        }

        // SniperScan: gira lentamente buscando cuando no hay objetivo
        if (data.idleAnim == TowerAnim.SniperScan)
        {
            if (!hasTarget)
            {
                _scanAngle += 25f * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.Euler(0f, _scanAngle, 0f),
                    4f * Time.deltaTime);
            }
            else
            {
                // Al apuntar, sincronizamos _scanAngle con la rotación actual
                _scanAngle = transform.eulerAngles.y;
            }
        }

        AfterAim: ;
    }

    void AimAtTarget()
    {
        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, target, 8f * Time.deltaTime);
        }
    }

    // ── Recoil al disparar ────────────────────────────────────────────────────

    IEnumerator Recoil()
    {
        if (_model == null || _recoiling) yield break;
        _recoiling = true;

        const float kickBack  = 0.18f;   // unidades hacia atrás (local -Z)
        const float kickTime  = 0.06f;   // tiempo del golpe
        const float returnTime = 0.14f;  // tiempo de vuelta

        Vector3 kickPos = _modelBasePos + Vector3.back * kickBack;

        // Retroceso rápido
        for (float t = 0f; t < kickTime; t += Time.deltaTime)
        {
            _model.localPosition = Vector3.Lerp(_modelBasePos, kickPos, t / kickTime);
            yield return null;
        }
        // Regreso suave
        for (float t = 0f; t < returnTime; t += Time.deltaTime)
        {
            _model.localPosition = Vector3.Lerp(kickPos, _modelBasePos, t / returnTime);
            yield return null;
        }

        _model.localPosition = _modelBasePos;
        _recoiling = false;
    }

    // ── Daño recibido ─────────────────────────────────────────────────────────

    void HandleClick()
    {
        bool leftClick  = Mouse.current?.leftButton.wasPressedThisFrame  ?? false;
        bool rightClick = Mouse.current?.rightButton.wasPressedThisFrame ?? false;
        Vector2 clickPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        // Touch: tap con un dedo cuenta como click izquierdo
        if (!leftClick && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    leftClick = true;
                    clickPos  = touch.position.ReadValue();
                    break;
                }
            }
        }

        if (!leftClick && !rightClick) return;
        // Click derecho ignorado si FreeBuildManager está en modo colocación
        if (rightClick && BuildManager.Instance != null && BuildManager.Instance.HasTowerSelected) return;
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(clickPos);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            TowerUpgradeUI.GetOrCreate().Show(this);
    }

    public void TakeDamage(int dmg)
    {
        towerHealth -= dmg;
        StartCoroutine(FlashRed());
        if (towerHealth <= 0) DestroyTower();
    }

    IEnumerator FlashRed()
    {
        // Cuando hay modelo 3D el cubo padre tiene el Renderer desactivado;
        // buscamos el primer Renderer activo entre el modelo y sus hijos.
        Renderer rend = null;
        if (_model != null)
            rend = _model.GetComponentInChildren<Renderer>(false);
        if (rend == null)
            rend = GetComponent<Renderer>();
        if (rend == null) yield break;

        Color original = rend.material.color;
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        rend.material.color = original;
    }

    void DestroyTower()
    {
        if (mySlot != null) mySlot.SetEmpty();
        Destroy(gameObject);
    }

    // ── Upgrade ───────────────────────────────────────────────────────────────

    public bool CanUpgrade() => upgradeLevel < MAX_LEVEL;

    public int GetUpgradeCost()
    {
        if (!CanUpgrade()) return 0;
        float[] multipliers = { 0.5f, 0.75f, 1.0f };
        return Mathf.RoundToInt(data.cost * multipliers[upgradeLevel]);
    }

    /// <summary>Devuelve el 50 % del oro total gastado en esta torre (build + upgrades).</summary>
    public int GetSellPrice()
    {
        if (data == null) return 0;
        float[] upgradeMults = { 0.5f, 0.75f, 1.0f };
        float spent = data.cost;
        for (int i = 0; i < upgradeLevel; i++)
            spent += data.cost * upgradeMults[i];
        return Mathf.RoundToInt(spent * 0.5f);
    }

    public void SellTower()
    {
        GameManager.Instance.AddGold(GetSellPrice());
        if (mySlot != null) mySlot.SetEmpty();
        Destroy(gameObject);
    }

    public void ApplyUpgrade()
    {
        if (!CanUpgrade()) return;
        upgradeLevel++;
        currentDamage = Mathf.RoundToInt(currentDamage * 1.5f);
        currentRange *= 1.2f;
        towerHealth   = maxTowerHealth;
        UpdateUpgradeVisuals();
    }

    // ── Visuales de nivel de upgrade ─────────────────────────────────────────

    void UpdateUpgradeVisuals()
    {
        // 1. Escalar modelo — nivel 1: 1.0x  2: 1.2x  3: 1.4x  4: 1.6x
        float[] scaleMultis = { 1.0f, 1.2f, 1.4f, 1.6f };
        float sm = upgradeLevel < scaleMultis.Length ? scaleMultis[upgradeLevel] : 1.6f;
        if (_model != null)
        {
            _model.localScale = _initialModelScale * sm;
            _modelBaseScale   = _model.localScale;
        }

        // Actualizar el collider para que el click siga funcionando tras el upgrade
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null) bc.size = Vector3.one * sm;

        // 2. Limpiar ornamentos del upgrade anterior
        Transform oldRing  = transform.Find("UpgradeRing");
        Transform oldLight = transform.Find("UpgradeLight");
        if (oldRing  != null) Destroy(oldRing.gameObject);
        if (oldLight != null) Destroy(oldLight.gameObject);

        if (upgradeLevel <= 0) return;

        // 3. Anillo dorado debajo de la torre
        //    diámetro crece con el nivel: 1→1.8  2→2.6  3→3.4
        float[] ringDiams = { 0f, 1.8f, 2.6f, 3.4f };
        float diam = upgradeLevel < ringDiams.Length ? ringDiams[upgradeLevel] : 3.4f;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "UpgradeRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, -2.5f, 0f);  // cerca del suelo
        ring.transform.localScale    = new Vector3(diam, 0.05f, diam);
        Destroy(ring.GetComponent<Collider>());
        ring.GetComponent<Renderer>().sharedMaterial =
            GameMaterials.GetLitCached(new Color(1f, 0.82f, 0.08f));

        // 4. Luz puntual dorada encima (nivel 3+)
        if (upgradeLevel >= 2)
        {
            GameObject lightGO = new GameObject("UpgradeLight");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 3.5f, 0f);

            Light lt  = lightGO.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.color     = new Color(1f, 0.88f, 0.28f);
            lt.intensity = upgradeLevel >= 3 ? 3.5f : 2.5f;
            lt.range     = upgradeLevel >= 3 ? 8f   : 6f;
        }
    }

    // ── Disparo ───────────────────────────────────────────────────────────────

    Enemy FindClosestEnemy()
    {
        var enemies = Enemy.Snapshot();
        Enemy best  = null;

        switch (targeting)
        {
            case TargetingMode.First:
                int maxWP = -1;
                foreach (Enemy e in enemies)
                {
                    if (Vector3.Distance(transform.position, e.transform.position) > currentRange) continue;
                    if (e.WaypointIndex > maxWP) { maxWP = e.WaypointIndex; best = e; }
                }
                return best;

            case TargetingMode.Last:
                int minWP = int.MaxValue;
                foreach (Enemy e in enemies)
                {
                    if (Vector3.Distance(transform.position, e.transform.position) > currentRange) continue;
                    if (e.WaypointIndex < minWP) { minWP = e.WaypointIndex; best = e; }
                }
                return best;

            case TargetingMode.Strongest:
                int maxHP = -1;
                foreach (Enemy e in enemies)
                {
                    if (Vector3.Distance(transform.position, e.transform.position) > currentRange) continue;
                    if (e.health > maxHP) { maxHP = e.health; best = e; }
                }
                return best;

            case TargetingMode.Fastest:
                float maxSpd = -1f;
                foreach (Enemy e in enemies)
                {
                    if (Vector3.Distance(transform.position, e.transform.position) > currentRange) continue;
                    if (e.speed > maxSpd) { maxSpd = e.speed; best = e; }
                }
                return best;

            default: // Nearest
                float minDist = currentRange;
                foreach (Enemy e in enemies)
                {
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < minDist) { minDist = d; best = e; }
                }
                return best;
        }
    }

    void Shoot()
    {
        StartCoroutine(Recoil());
        SpawnMuzzleFlash();

        if (data.towerType == TowerType.Sol)
        {
            ShootHeat();
            AudioManager.PlayShoot();
            return;
        }

        float bulletSize = 0.3f;
        if      (data.towerType == TowerType.Arena)    bulletSize = 0.45f;
        else if (data.towerType == TowerType.Tormenta) bulletSize = 0.25f;

        Material mat = GameMaterials.GetBulletMaterial(data.towerType);
        GameObject go = BulletPool.Instance.Get(bulletSize, mat);
        go.transform.position = transform.position;

        Bullet b         = go.GetComponent<Bullet>();
        b.speed          = 15f;
        b.damage         = currentDamage;
        b.effectType     = data.towerType;
        b.poisonDps      = data.poisonDps;
        b.poisonDuration = data.poisonDuration;
        b.slowFactor     = data.slowFactor;
        b.slowDuration   = data.slowDuration;
        b.chainCount     = data.chainCount;
        b.chainRange     = data.chainRange;
        b.chainDamage    = data.chainDamage;
        b.SetTarget(currentTarget);

        AudioManager.PlayShoot();
    }

    // ── Indicador de rango ────────────────────────────────────────────────────

    public void ShowRangeIndicator()
    {
        if (_rangeCircle == null)
        {
            GameObject go = new GameObject("RangeCircle");
            go.transform.SetParent(transform, false);
            _rangeCircle = go.AddComponent<LineRenderer>();
            _rangeCircle.loop             = true;
            _rangeCircle.useWorldSpace    = false;
            _rangeCircle.widthMultiplier  = 0.12f;
            _rangeCircle.positionCount    = 48;
            _rangeCircle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rangeCircle.receiveShadows   = false;

            Color c = new Color(1f, 0.88f, 0.2f, 0.7f);
            Material m = GameMaterials.MakeUnlit(c);
            if (m != null) _rangeCircle.sharedMaterial = m;
            _rangeCircle.startColor = _rangeCircle.endColor = new Color(1f, 0.88f, 0.2f, 0.7f);
        }

        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            _rangeCircle.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * currentRange,
                -1.8f,
                Mathf.Sin(angle) * currentRange));
        }
        _rangeCircle.enabled = true;
    }

    public void HideRangeIndicator()
    {
        if (_rangeCircle != null) _rangeCircle.enabled = false;
    }

    // ── Muzzle flash al disparar ──────────────────────────────────────────────

    void SpawnMuzzleFlash()
    {
        if (data == null || data.towerType == TowerType.Sol) return;

        Vector3 muzzlePos = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
        GameObject go = new GameObject("MuzzleFlash");
        go.transform.position = muzzlePos;
        go.transform.rotation = transform.rotation;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = false;
        main.duration      = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.13f);
        main.maxParticles  = 10;
        main.stopAction    = ParticleSystemStopAction.Destroy;

        Color flashColor = data.towerType == TowerType.Tormenta ? new Color(0.6f, 0.65f, 1f) :
                           data.towerType == TowerType.Cactus   ? new Color(0.45f, 1f,  0.3f) :
                           data.towerType == TowerType.Arena    ? new Color(1f,  0.85f, 0.4f) :
                           new Color(1f, 0.8f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(flashColor, Color.white);

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 28f;
        shape.radius    = 0.04f;

        // Asignar material URP explícitamente — el default usa Particles/Standard Unlit (pink en Android)
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (GameMaterials.DeathParticle != null) rend.sharedMaterial = GameMaterials.DeathParticle;

        Destroy(go, 0.3f);
    }

    void ShootHeat()
    {
        foreach (Enemy e in Enemy.Snapshot())
        {
            if (Vector3.Distance(transform.position, e.transform.position) <= currentRange)
            {
                e.TakeDamage(currentDamage);
                SpawnHeatRay(transform.position, e.transform.position);
            }
        }
    }

    static void SpawnHeatRay(Vector3 from, Vector3 to)
    {
        GameObject go = new GameObject("HeatRay");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = 0.12f;
        lr.endWidth   = 0.04f;

        if (GameMaterials.HeatRay != null)
            lr.sharedMaterial = GameMaterials.HeatRay;
        lr.startColor = new Color(1f, 0.55f, 0.05f, 0.9f);
        lr.endColor   = new Color(1f, 0.25f, 0.0f,  0.0f);

        Destroy(go, 0.12f);
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,
            Application.isPlaying ? currentRange : data.range);
    }
}
