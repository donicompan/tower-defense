using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera proceduralmente piedras, arbustos secos y huesos dispersos por el mapa.
/// Usa el mismo sistema de Raycast y validación de distancias que CactusGenerator:
/// respeta el camino de enemigos, los BuildSlots y el espaciado entre objetos.
/// Añadir este script al mismo GameObject que CactusGenerator (o a uno propio).
/// </summary>
public class FloraGenerator : MonoBehaviour
{
    [Header("Cantidades — flora original")]
    public int rockCount       = 22;
    public int bushCount       = 16;
    public int boneCount       = 10;

    [Header("Cantidades — nuevos elementos")]
    public int paloVerdeCount  = 10;   // árboles palo verde (tronco + ramas)
    public int rockClusterCount = 6;   // grupos de rocas grandes
    public int aridZoneCount   = 12;   // zonas de tierra seca con grietas

    [Header("Límites del mapa")]
    public float mapMinX = -225f;
    public float mapMaxX =  275f;
    public float mapMinZ = -200f;
    public float mapMaxZ =  300f;

    [Header("Clearance (distancias mínimas)")]
    public float pathClearance = 4.5f;   // distancia al camino de enemigos
    public float slotClearance = 3.5f;   // distancia a BuildSlots
    public float itemSpacing   = 2.2f;   // distancia entre cualquier objeto de flora
    public float edgeMargin    = 2.0f;   // margen desde el borde del mapa

    [Header("Validación de terreno")]
    [Range(0.5f, 1f)]
    public float minSlopeNormalY = 0.80f;

    private int       _terrainMask;
    private Transform _floraRoot;

    void Start()
    {
        _terrainMask = LayerMask.GetMask("Terrain");
        Generate();
    }

    /// <summary>
    /// Destruye toda la flora existente y la regenera sobre el terreno actual.
    /// Funciona tanto en Play como desde el Inspector (ContextMenu).
    /// </summary>
    [ContextMenu("Regenerar Flora")]
    public void RegenerarFlora()
    {
        Transform existing = transform.Find("FloraRoot");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }
        _floraRoot   = null;
        _terrainMask = LayerMask.GetMask("Terrain");
        Generate();
    }

    void Generate()
    {
        // Contenedor raíz: agrupa toda la flora para poder destruirla de golpe
        var rootGO = new GameObject("FloraRoot");
        rootGO.transform.SetParent(transform);
        _floraRoot = rootGO.transform;

        Vector3[] waypoints = GetWaypointPositions();
        Vector3[] slots     = GetBuildSlotPositions();
        // Lista compartida: ningún elemento de flora queda sobre otro
        int total = rockCount + bushCount + boneCount + paloVerdeCount + rockClusterCount + aridZoneCount;
        List<Vector3> placed = new List<Vector3>(total);

        SpawnItems(rockCount,        waypoints, slots, placed, SpawnRock);
        SpawnItems(bushCount,        waypoints, slots, placed, SpawnBush);
        SpawnItems(boneCount,        waypoints, slots, placed, SpawnBone);
        SpawnItems(paloVerdeCount,   waypoints, slots, placed, SpawnPaloVerde);
        SpawnItems(rockClusterCount, waypoints, slots, placed, SpawnRockCluster);
        SpawnItems(aridZoneCount,    waypoints, slots, placed, SpawnAridZone);
    }

    void SpawnItems(int count, Vector3[] waypoints, Vector3[] slots,
                   List<Vector3> placed, System.Action<Vector3> spawnFn)
    {
        int attempts = 0, maxAttempts = count * 40, spawned = 0;
        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 candidate = new Vector3(
                Random.Range(mapMinX + edgeMargin, mapMaxX - edgeMargin),
                0f,
                Random.Range(mapMinZ + edgeMargin, mapMaxZ - edgeMargin));

            if (!IsValidPosition(candidate, waypoints, slots, placed)) continue;
            if (!SampleTerrain(candidate, _terrainMask, minSlopeNormalY, out float groundY)) continue;

            candidate.y = groundY;
            spawnFn(candidate);
            placed.Add(candidate);
            spawned++;
        }
    }

    // ── Spawners ──────────────────────────────────────────────────────────────

    void SpawnRock(Vector3 pos)
    {
        GameObject root = new GameObject("Rock");
        root.transform.SetParent(_floraRoot);
        root.transform.position = pos;
        // Ligera inclinación aleatoria para aspecto natural
        root.transform.rotation = Quaternion.Euler(
            Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));

        float size = Random.Range(0.35f, 1.3f);
        Color col  = new Color(
            Random.Range(0.44f, 0.62f),
            Random.Range(0.40f, 0.54f),
            Random.Range(0.32f, 0.46f));

        // Piedra principal: esfera achatada (parece un canto rodado)
        MakePart(root.transform, PrimitiveType.Sphere,
            Vector3.up * size * 0.30f,
            new Vector3(size, size * Random.Range(0.40f, 0.65f), size * Random.Range(0.75f, 1.20f)),
            Quaternion.identity, col);

        // Piedra secundaria opcional (parcialmente embebida al lado)
        if (Random.value > 0.40f)
        {
            float s2 = size * Random.Range(0.35f, 0.60f);
            Color c2 = col * Random.Range(0.85f, 1.12f); c2.a = 1f;
            Vector3 offset = new Vector3(
                Random.Range(-size * 0.45f, size * 0.45f),
                size * 0.15f,
                Random.Range(-size * 0.35f, size * 0.35f));
            MakePart(root.transform, PrimitiveType.Sphere,
                offset, new Vector3(s2, s2 * Random.Range(0.45f, 0.75f), s2),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), c2);
        }
    }

    void SpawnBush(Vector3 pos)
    {
        GameObject root = new GameObject("Bush");
        root.transform.SetParent(_floraRoot);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float size = Random.Range(0.25f, 0.75f);
        // Colores áridos: marrón grisáceo / verde seco
        Color col = new Color(
            Random.Range(0.36f, 0.52f),
            Random.Range(0.30f, 0.44f),
            Random.Range(0.15f, 0.28f));

        int sphereCount = Random.Range(3, 7);
        for (int i = 0; i < sphereCount; i++)
        {
            float r = size * Random.Range(0.30f, 0.58f);
            Vector3 offset = new Vector3(
                Random.Range(-size * 0.55f, size * 0.55f),
                r * 0.70f + Random.Range(0f, size * 0.25f),
                Random.Range(-size * 0.55f, size * 0.55f));
            Color c = col * Random.Range(0.82f, 1.18f); c.a = 1f;
            MakePart(root.transform, PrimitiveType.Sphere,
                offset, Vector3.one * r, Quaternion.identity, c);
        }

        // Tallo central fino (cilindro)
        Color stemCol = new Color(0.38f, 0.28f, 0.15f);
        MakePart(root.transform, PrimitiveType.Cylinder,
            Vector3.up * size * 0.20f,
            new Vector3(size * 0.06f, size * 0.22f, size * 0.06f),
            Quaternion.identity, stemCol);
    }

    void SpawnBone(Vector3 pos)
    {
        GameObject root = new GameObject("Bone");
        root.transform.SetParent(_floraRoot);
        // Tumbado sobre el suelo (rot Z 90°) con rotación Y aleatoria
        root.transform.position = pos + Vector3.up * 0.04f;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 90f);

        float len  = Random.Range(0.40f, 0.95f);
        float endR = Random.Range(0.07f, 0.14f);
        float midR = endR * 0.42f;

        Color bone = new Color(
            Random.Range(0.82f, 0.96f),
            Random.Range(0.76f, 0.90f),
            Random.Range(0.62f, 0.80f));

        // Diáfisis (caña central)
        MakePart(root.transform, PrimitiveType.Cylinder,
            Vector3.zero,
            new Vector3(midR * 2f, len * 0.5f, midR * 2f),
            Quaternion.identity, bone);

        // Epífisis proximal (extremo superior)
        MakePart(root.transform, PrimitiveType.Sphere,
            Vector3.up * len * 0.5f,
            Vector3.one * endR * 2f,
            Quaternion.identity, bone);

        // Epífisis distal (extremo inferior)
        MakePart(root.transform, PrimitiveType.Sphere,
            Vector3.down * len * 0.5f,
            Vector3.one * endR * 2f,
            Quaternion.identity, bone);
    }

    // — Palo verde (árbol de desierto: tronco verde + ramas divergentes) ——————

    void SpawnPaloVerde(Vector3 pos)
    {
        GameObject root = new GameObject("PaloVerde");
        root.transform.SetParent(_floraRoot);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float trunkH = Random.Range(3.5f, 6.5f);
        float trunkR = Random.Range(0.10f, 0.18f);

        // Corteza verde-grisácea característica del palo verde
        Color bark = new Color(
            Random.Range(0.22f, 0.38f),
            Random.Range(0.38f, 0.55f),
            Random.Range(0.14f, 0.26f));

        // Tronco principal
        MakePart(root.transform, PrimitiveType.Cylinder,
            Vector3.up * trunkH * 0.5f,
            new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f),
            Quaternion.identity, bark);

        // Ramas divergentes (3-5), salen a distintas alturas con ángulo variable
        int branchCount = Random.Range(3, 6);
        for (int i = 0; i < branchCount; i++)
        {
            float attachH  = trunkH * Random.Range(0.30f, 0.78f);
            float branchLen = Random.Range(0.9f, 2.4f);
            float branchR  = trunkR * Random.Range(0.38f, 0.60f);
            float azimuth  = (360f / branchCount) * i + Random.Range(-25f, 25f);
            float elevation = Random.Range(28f, 68f); // grados sobre la horizontal

            // Dirección de la rama en espacio local del árbol
            float elevRad = elevation * Mathf.Deg2Rad;
            float azimRad = azimuth  * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(
                Mathf.Cos(elevRad) * Mathf.Sin(azimRad),
                Mathf.Sin(elevRad),
                Mathf.Cos(elevRad) * Mathf.Cos(azimRad));

            Vector3 branchCenter = Vector3.up * attachH + dir * branchLen * 0.5f;
            Quaternion cylRot    = Quaternion.FromToRotation(Vector3.up, dir);
            Color branchCol      = bark * Random.Range(0.88f, 1.08f); branchCol.a = 1f;

            MakePart(root.transform, PrimitiveType.Cylinder,
                branchCenter,
                new Vector3(branchR * 2f, branchLen * 0.5f, branchR * 2f),
                cylRot, branchCol);
        }
    }

    // — Grupo de rocas grandes ————————————————————————————————————————————————

    void SpawnRockCluster(Vector3 pos)
    {
        GameObject root = new GameObject("RockCluster");
        root.transform.SetParent(_floraRoot);
        root.transform.position = pos;

        int   count    = Random.Range(3, 6);
        float baseSize = Random.Range(1.0f, 2.2f);
        Color baseCol  = new Color(
            Random.Range(0.42f, 0.58f),
            Random.Range(0.38f, 0.50f),
            Random.Range(0.28f, 0.42f));

        for (int i = 0; i < count; i++)
        {
            float angle  = Random.Range(0f, Mathf.PI * 2f);
            float dist   = Random.Range(0f, baseSize * 0.7f);
            Vector3 off  = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            float   size = baseSize * Random.Range(0.45f, 1.0f);
            Color   col  = baseCol * Random.Range(0.85f, 1.15f); col.a = 1f;

            Quaternion tilt = Quaternion.Euler(
                Random.Range(-18f, 18f), Random.Range(0f, 360f), Random.Range(-18f, 18f));

            // Cada roca es un child del root (hereda la posición del cluster)
            GameObject rock = new GameObject($"RockCluster_stone{i}");
            rock.transform.SetParent(root.transform, false);
            rock.transform.localPosition = off;
            rock.transform.localRotation = tilt;

            MakePart(rock.transform, PrimitiveType.Sphere,
                Vector3.up * size * 0.28f,
                new Vector3(size, size * Random.Range(0.42f, 0.68f), size * Random.Range(0.80f, 1.30f)),
                Quaternion.identity, col);
        }
    }

    // — Zona árida con tierra seca y grietas ——————————————————————————————————

    void SpawnAridZone(Vector3 pos)
    {
        GameObject root = new GameObject("AridZone");
        root.transform.SetParent(_floraRoot);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float radius = Random.Range(1.4f, 3.2f);

        // Parche de tierra seca (cilindro muy achatado)
        Color dirtCol = new Color(
            Random.Range(0.38f, 0.52f),
            Random.Range(0.26f, 0.38f),
            Random.Range(0.14f, 0.24f));
        MakePart(root.transform, PrimitiveType.Cylinder,
            Vector3.up * 0.015f,
            new Vector3(radius * 2f, 0.015f, radius * 2f),
            Quaternion.identity, dirtCol);

        // Grietas: cilindros tumbados sobre el parche, color más oscuro
        Color crackCol = dirtCol * 0.52f; crackCol.a = 1f;
        int crackCount = Random.Range(3, 7);
        for (int i = 0; i < crackCount; i++)
        {
            float crackLen    = radius * Random.Range(0.45f, 1.15f);
            float crackAngle  = Random.Range(0f, Mathf.PI * 2f);
            Vector3 crackDir  = new Vector3(Mathf.Cos(crackAngle), 0f, Mathf.Sin(crackAngle));
            // Centro de la grieta: desplazado desde el centro del parche
            Vector3 crackPos  = crackDir * radius * Random.Range(0f, 0.55f) + Vector3.up * 0.03f;
            // Rotar el cilindro para que su eje local Y apunte en la dirección de la grieta
            Quaternion crackRot = Quaternion.FromToRotation(Vector3.up, crackDir);

            MakePart(root.transform, PrimitiveType.Cylinder,
                crackPos,
                new Vector3(0.045f, crackLen * 0.5f, 0.045f),
                crackRot, crackCol);
        }
    }

    // ── Helpers (mismo patrón que CactusGenerator) ────────────────────────────

    static void MakePart(Transform parent, PrimitiveType primitive,
                          Vector3 localPos, Vector3 scale,
                          Quaternion localRot, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        go.transform.localRotation = localRot;

        Destroy(go.GetComponent<Collider>());

        Material mat = GameMaterials.MakeLit(color);
        if (mat == null) return;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        go.GetComponent<Renderer>().material = mat;
    }

    bool IsValidPosition(Vector3 pos, Vector3[] waypoints, Vector3[] slots, List<Vector3> placed)
    {
        Vector3 flatPos = Flat(pos);

        for (int i = 0; i < waypoints.Length - 1; i++)
            if (DistToSegXZ(flatPos, Flat(waypoints[i]), Flat(waypoints[i + 1])) < pathClearance)
                return false;

        if (waypoints.Length == 1 && Vector3.Distance(flatPos, Flat(waypoints[0])) < pathClearance)
            return false;

        foreach (Vector3 s in slots)
            if (Vector3.Distance(flatPos, Flat(s)) < slotClearance)
                return false;

        foreach (Vector3 p in placed)
            if (Vector3.Distance(flatPos, Flat(p)) < itemSpacing)
                return false;

        return true;
    }

    static bool SampleTerrain(Vector3 pos, int mask, float minNormalY, out float groundY)
    {
        groundY = 0f;
        Vector3 origin = new Vector3(pos.x, 200f, pos.z);

        RaycastHit hit = default;
        bool found = mask != 0 && Physics.Raycast(origin, Vector3.down, out hit, 400f, mask);
        if (!found) found = Physics.Raycast(origin, Vector3.down, out hit, 400f);

        if (!found) return false;
        if (!(hit.collider is TerrainCollider)) return false;
        if (hit.normal.y < minNormalY) return false;

        groundY = hit.point.y;
        return true;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    static float DistToSegXZ(Vector3 p, Vector3 a, Vector3 b)
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
        var res  = new Vector3[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            res[i] = pts[i] != null ? pts[i].position : Vector3.zero;
        return res;
    }

    Vector3[] GetBuildSlotPositions()
    {
        var slots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        var res   = new Vector3[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            res[i] = slots[i].transform.position;
        return res;
    }
}
