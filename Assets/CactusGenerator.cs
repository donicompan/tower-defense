using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera cactus decorativos proceduralmente al iniciar la escena.
/// Evita el camino de enemigos (waypoints) y los BuildSlots.
/// Sin colisionadores: puramente decorativos.
/// </summary>
public class CactusGenerator : MonoBehaviour
{
    [Header("Cantidad")]
    public int cactusCount = 28;

    [Header("Límites del mapa")]
    public float mapMinX = -225f;
    public float mapMaxX =  275f;
    public float mapMinZ = -200f;
    public float mapMaxZ =  300f;

    [Header("Clearance (distancias mínimas)")]
    public float pathClearance   = 5.5f;   // desde segmento del camino
    public float slotClearance   = 4.5f;   // desde BuildSlots
    public float cactusSpacing   = 4.0f;   // entre cactus
    public float edgeMargin      = 3.0f;   // desde el borde del mapa

    [Header("Validación de terreno")]
    [Tooltip("Normal Y mínima del suelo (1=plano, 0=vertical). Rechaza pendientes pronunciadas.")]
    [Range(0.5f, 1f)]
    public float minSlopeNormalY = 0.85f;

    private int       _terrainMask;
    private Transform _cactusRoot;

    void Start()
    {
        _terrainMask = LayerMask.GetMask("Terrain");
        Generate();
    }

    /// <summary>
    /// Destruye todos los cactus existentes y los regenera sobre el terreno actual.
    /// Funciona tanto en Play como desde el Inspector (ContextMenu).
    /// </summary>
    [ContextMenu("Regenerar Flora (Cactus)")]
    public void RegenerarFlora()
    {
        Transform existing = transform.Find("CactusRoot");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }
        _cactusRoot  = null;
        _terrainMask = LayerMask.GetMask("Terrain");
        Generate();
    }

    void Generate()
    {
        // Contenedor raíz: agrupa todos los cactus para poder destruirlos de golpe
        var rootGO = new GameObject("CactusRoot");
        rootGO.transform.SetParent(transform);
        _cactusRoot = rootGO.transform;

        Vector3[] waypoints = GetWaypointPositions();
        Vector3[] slots     = GetBuildSlotPositions();
        List<Vector3> placed = new List<Vector3>(cactusCount);

        int attempts = 0;
        int maxAttempts = cactusCount * 30;

        while (placed.Count < cactusCount && attempts < maxAttempts)
        {
            attempts++;

            Vector3 candidate = new Vector3(
                Random.Range(mapMinX + edgeMargin, mapMaxX - edgeMargin),
                0f,
                Random.Range(mapMinZ + edgeMargin, mapMaxZ - edgeMargin));

            if (!IsValidPosition(candidate, waypoints, slots, placed))
                continue;

            // Verificar que el suelo sea terreno real y no una pendiente excesiva
            if (!SampleTerrain(candidate, _terrainMask, minSlopeNormalY, out float groundY))
                continue;

            candidate.y = groundY;
            SpawnCactus(candidate);
            placed.Add(candidate);
        }

    }

    // ── Validación de posición ────────────────────────────────────────────────

    bool IsValidPosition(Vector3 pos, Vector3[] waypoints, Vector3[] slots, List<Vector3> placed)
    {
        Vector3 flatPos = Flat(pos);

        // Distancia a segmentos del camino
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (DistanceToSegmentXZ(flatPos, Flat(waypoints[i]), Flat(waypoints[i + 1])) < pathClearance)
                return false;
        }
        // Si solo hay un waypoint, compara contra él directamente
        if (waypoints.Length == 1 && Vector3.Distance(flatPos, Flat(waypoints[0])) < pathClearance)
            return false;

        // Distancia a BuildSlots
        foreach (Vector3 s in slots)
            if (Vector3.Distance(flatPos, Flat(s)) < slotClearance)
                return false;

        // Distancia a otros cactus ya colocados
        foreach (Vector3 p in placed)
            if (Vector3.Distance(flatPos, Flat(p)) < cactusSpacing)
                return false;

        return true;
    }

    // ── Construcción del cactus ───────────────────────────────────────────────

    void SpawnCactus(Vector3 pos)
    {
        GameObject root = new GameObject("Cactus");
        root.transform.SetParent(_cactusRoot);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float trunkH  = Random.Range(2.2f, 5.0f);
        float trunkR  = Random.Range(0.16f, 0.30f);

        Color green = new Color(
            Random.Range(0.06f, 0.16f),
            Random.Range(0.32f, 0.52f),
            Random.Range(0.06f, 0.16f));

        // Tronco principal (cilindro de Unity escala Y = semialtura)
        MakePart(root.transform,
            new Vector3(0f, trunkH * 0.5f, 0f),
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.identity, green);

        // Brazos laterales (0, 1 o 2)
        int armCount = Random.Range(0, 3);
        float[] sides = { -1f, 1f };

        for (int i = 0; i < armCount; i++)
        {
            float side      = sides[i % 2];
            float attachH   = trunkH * Random.Range(0.40f, 0.65f);   // altura del codo
            float armHLen   = Random.Range(0.7f, 1.4f);              // longitud horizontal
            float armVLen   = trunkH * Random.Range(0.25f, 0.42f);   // longitud vertical
            float armR      = trunkR * Random.Range(0.75f, 0.90f);
            Color armGreen  = green * Random.Range(0.88f, 1.08f);
            armGreen.a = 1f;

            // Segmento horizontal
            MakePart(root.transform,
                new Vector3(side * armHLen * 0.5f, attachH, 0f),
                new Vector3(armR * 2f, armHLen * 0.5f, armR * 2f),
                Quaternion.Euler(0f, 0f, 90f), armGreen);

            // Segmento vertical (sube desde el extremo del brazo horizontal)
            MakePart(root.transform,
                new Vector3(side * armHLen, attachH + armVLen * 0.5f, 0f),
                new Vector3(armR * 2f, armVLen * 0.5f, armR * 2f),
                Quaternion.identity, armGreen);
        }
    }

    static void MakePart(Transform parent, Vector3 localPos, Vector3 scale,
                          Quaternion localRot, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.transform.localRotation = localRot;

        // Sin colisionador — puramente decorativo
        Destroy(go.GetComponent<Collider>());

        Material mat = GameMaterials.MakeLit(color);
        if (mat != null) go.GetComponent<Renderer>().material = mat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispara un Raycast hacia abajo y valida que:
    ///   1. El collider golpeado sea un TerrainCollider o tenga tag "Terrain".
    ///   2. La normal del punto de impacto sea casi vertical (normalY >= minNormalY).
    /// Devuelve true y escribe la Y del suelo si pasa ambas condiciones.
    /// </summary>
    static bool SampleTerrain(Vector3 pos, int mask, float minNormalY, out float groundY)
    {
        groundY = 0f;
        Vector3 origin = new Vector3(pos.x, 200f, pos.z);

        // Intentar primero con la máscara de layer "Terrain"
        RaycastHit hit = default;
        bool found = mask != 0 && Physics.Raycast(origin, Vector3.down, out hit, 400f, mask);

        // Fallback: raycast contra todo si la máscara no produjo resultado
        if (!found)
            found = Physics.Raycast(origin, Vector3.down, out hit, 400f);

        if (!found) return false;

        // 1. ¿Es un TerrainCollider o tiene tag "Terrain"?
        bool isTerrain = hit.collider is TerrainCollider;
        if (!isTerrain) return false;

        // 2. ¿La normal del suelo es suficientemente vertical?
        if (hit.normal.y < minNormalY) return false;

        groundY = hit.point.y;
        return true;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    static float DistanceToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        if (ab.sqrMagnitude < 0.0001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(p, a + t * ab);
    }

    Vector3[] GetWaypointPositions()
    {
        if (WaypointPath.Instance == null || WaypointPath.Instance.points == null)
            return new Vector3[0];

        var pts = WaypointPath.Instance.points;
        var result = new Vector3[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            result[i] = pts[i] != null ? pts[i].position : Vector3.zero;
        return result;
    }

    Vector3[] GetBuildSlotPositions()
    {
        var slots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        var result = new Vector3[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            result[i] = slots[i].transform.position;
        return result;
    }
}
