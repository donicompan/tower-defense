using UnityEngine;

/// <summary>
/// Genera 2-3 buitres procedurales que vuelan en círculos lentos a gran altura.
/// Cada buitre es un conjunto de primitivas de Unity (cuerpo + alas + cabeza).
/// Las alas aletean suavemente usando una función seno.
/// No requiere assets externos.
/// Añadir este script a cualquier GameObject vacío en la escena (p.ej. "Ambient").
/// </summary>
public class VultureFlock : MonoBehaviour
{
    [Header("Bandada")]
    [Range(1, 5)] public int vultureCount = 3;

    [Header("Altitud")]
    public float minAltitude = 15f;
    public float maxAltitude = 25f;

    [Header("Órbita")]
    public float minOrbitRadius  = 10f;
    public float maxOrbitRadius  = 30f;
    /// <summary>Radianes por segundo. Negativo = sentido horario.</summary>
    [Range(0.01f, 0.20f)] public float minAngularSpeed = 0.022f;
    [Range(0.01f, 0.20f)] public float maxAngularSpeed = 0.060f;

    [Header("Aleteo")]
    [Range(0.3f, 5f)]  public float wingFlapSpeed = 1.6f;
    [Range(5f, 40f)]   public float wingFlapAngle = 20f;
    /// <summary>Ángulo base de las alas en reposo (positivo = alas ligeramente elevadas).</summary>
    [Range(0f, 30f)]   public float wingRestAngle = 14f;

    // ── Datos internos ────────────────────────────────────────────────────────

    struct VultureData
    {
        public Transform root;
        public Transform leftWing;
        public Transform rightWing;
        public Vector3   orbitCenter;
        public float     orbitRadius;
        public float     angularSpeed;   // rad/s (puede ser negativo)
        public float     angle;          // ángulo actual en la órbita
        public float     flapPhase;      // desfase de aleteo
        public float     driftFreq;      // frecuencia de deriva vertical suave
        public float     driftAmp;       // amplitud de deriva vertical
    }

    private VultureData[] _vultures;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        _vultures = new VultureData[vultureCount];
        for (int i = 0; i < vultureCount; i++)
            _vultures[i] = BuildVulture(i);

        Debug.Log($"[VultureFlock] {vultureCount} buitres creados a Y={minAltitude}-{maxAltitude}.");
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < _vultures.Length; i++)
        {
            ref VultureData v = ref _vultures[i];

            // Avanzar ángulo orbital
            v.angle += v.angularSpeed * Time.deltaTime;

            // Posición en la órbita + deriva vertical suave
            float x = v.orbitCenter.x + Mathf.Cos(v.angle) * v.orbitRadius;
            float z = v.orbitCenter.z + Mathf.Sin(v.angle) * v.orbitRadius;
            float y = v.orbitCenter.y + Mathf.Sin(t * v.driftFreq + v.flapPhase) * v.driftAmp;
            v.root.position = new Vector3(x, y, z);

            // Orientación: mirando en la dirección tangente a la órbita
            float sign = Mathf.Sign(v.angularSpeed);
            Vector3 forward = new Vector3(
                -Mathf.Sin(v.angle) * sign,
                0f,
                 Mathf.Cos(v.angle) * sign);
            if (forward.sqrMagnitude > 0.001f)
                v.root.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // Aleteo: rotación local en eje Z para cada ala
            float flap = Mathf.Sin(t * wingFlapSpeed + v.flapPhase) * wingFlapAngle;
            v.leftWing.localRotation  = Quaternion.Euler(0f, 0f,  flap + wingRestAngle);
            v.rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap - wingRestAngle);
        }
    }

    // ── Construcción del buitre ───────────────────────────────────────────────

    VultureData BuildVulture(int index)
    {
        float bodyScale = Random.Range(2.0f, 3.2f);

        // Colores: cuerpo oscuro, cabeza rojiza/naranja (buitre típico)
        Color bodyColor = new Color(
            Random.Range(0.10f, 0.22f),
            Random.Range(0.08f, 0.18f),
            Random.Range(0.06f, 0.13f));
        Color wingColor = bodyColor * Random.Range(0.80f, 1.15f); wingColor.a = 1f;
        Color headColor = new Color(
            Random.Range(0.58f, 0.78f),
            Random.Range(0.26f, 0.40f),
            Random.Range(0.15f, 0.28f));

        GameObject root = new GameObject($"Vulture_{index}");
        root.transform.SetParent(transform);

        // — Cuerpo (elipsoide) —
        var body = MakePart(root.transform, PrimitiveType.Sphere, "Body",
            Vector3.zero,
            new Vector3(bodyScale * 0.45f, bodyScale * 0.28f, bodyScale),
            Quaternion.identity, bodyColor);

        // — Cabeza (esfera pequeña adelante y arriba) —
        MakePart(root.transform, PrimitiveType.Sphere, "Head",
            new Vector3(0f, bodyScale * 0.18f, bodyScale * 0.54f),
            Vector3.one * bodyScale * 0.20f,
            Quaternion.identity, headColor);

        // — Cola (esfera achatada en la parte trasera) —
        MakePart(root.transform, PrimitiveType.Sphere, "Tail",
            new Vector3(0f, -bodyScale * 0.06f, -bodyScale * 0.58f),
            new Vector3(bodyScale * 0.22f, bodyScale * 0.10f, bodyScale * 0.30f),
            Quaternion.identity, bodyColor);

        // — Ala izquierda (cubo plano, pivota desde el cuerpo) —
        // El pivot está en el extremo interior (unión con el cuerpo), así que desplazamos el cubo
        // medio ala hacia afuera.
        float wingHalfLen = bodyScale * 0.85f;
        Transform leftWing = MakePart(root.transform, PrimitiveType.Cube, "WingL",
            new Vector3(-(bodyScale * 0.2f + wingHalfLen), 0f, bodyScale * 0.08f),
            new Vector3(wingHalfLen * 2f, bodyScale * 0.035f, bodyScale * 0.38f),
            Quaternion.identity, wingColor);

        Transform rightWing = MakePart(root.transform, PrimitiveType.Cube, "WingR",
            new Vector3( (bodyScale * 0.2f + wingHalfLen), 0f, bodyScale * 0.08f),
            new Vector3(wingHalfLen * 2f, bodyScale * 0.035f, bodyScale * 0.38f),
            Quaternion.identity, wingColor);

        // — Parámetros de vuelo —
        float altitude = Random.Range(minAltitude, maxAltitude);
        Vector3 center = new Vector3(
            Random.Range(-18f, 18f),
            altitude,
            Random.Range(-18f, 18f));

        float radius = Random.Range(minOrbitRadius, maxOrbitRadius);
        float speed  = Random.Range(minAngularSpeed, maxAngularSpeed);
        if (Random.value > 0.5f) speed = -speed;   // sentido horario o antihorario

        return new VultureData
        {
            root         = root.transform,
            leftWing     = leftWing,
            rightWing    = rightWing,
            orbitCenter  = center,
            orbitRadius  = radius,
            angularSpeed = speed,
            angle        = Random.Range(0f, Mathf.PI * 2f),
            flapPhase    = Random.Range(0f, Mathf.PI * 2f),
            driftFreq    = Random.Range(0.25f, 0.55f),
            driftAmp     = Random.Range(1.5f, 4.0f),
        };
    }

    static Transform MakePart(Transform parent, PrimitiveType type, string partName,
                               Vector3 localPos, Vector3 scale,
                               Quaternion localRot, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.transform.localRotation = localRot;

        Destroy(go.GetComponent<Collider>());

        Material mat = GameMaterials.MakeLit(color);
        if (mat != null)
        {
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
            go.GetComponent<Renderer>().material = mat;
        }

        return go.transform;
    }
}
