using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera el relieve del mapa proceduralmente como un desfiladero desierto:
///   - Cañón angosto (8-12 u) tallado a lo largo de los waypoints
///   - Mesetas a ambos lados del cañón donde se construyen torres
///   - Colinas suaves en los bordes del mapa
///   - Ruido Perlin multi-octava (fBm) para superficie irregular y natural
///   - Reposiciona waypoints y BuildSlots al nuevo terreno
///
/// USO EN EDITOR (sin entrar en Play):
///   1. Adjuntá este script a cualquier GameObject.
///   2. En el Inspector, click derecho sobre el componente → "Generar Terreno".
///
/// USO EN RUNTIME:
///   Activá "Generate On Play" en el Inspector.
/// </summary>
public class TerrainShaper : MonoBehaviour
{
    [Header("Referencias")]
    public Terrain terrain;

    [Header("Alturas normalizadas (0 = fondo, 1 = tope del TerrainData)")]
    [Range(0f, 0.15f)] public float canyonFloor    = 0.04f;  // fondo del cañón
    [Range(0.1f, 0.4f)] public float plateauHeight = 0.20f;  // meseta a los lados del cañón
    [Range(0.3f, 0.7f)] public float borderPeak    = 0.50f;  // cima de montañas en bordes

    [Header("Cañón (unidades de mundo)")]
    public float canyonHalfWidth  = 2f;   // semiancho plano del cañón (total 4 u)
    public float wallWidth        = 4f;   // ancho de la pared/transición hasta la meseta
    public float wallSteepness    = 5f;   // exponente de la pared (>1 = más vertical)

    [Header("Bordes del mapa")]
    [Range(0f, 0.5f)] public float borderFraction = 0.19f;  // fracción del mapa donde suben los bordes (~152 u de 800)
    public float borderSteepness = 2f;   // qué tan pronunciada es la curva de borde

    [Header("Montañas del borde (anillo exterior)")]
    [Tooltip("Amplitud del ruido extra en el anillo de montañas (se escala con borderT → cero en el centro).")]
    [Range(0f, 0.4f)] public float borderNoiseAmp  = 0.14f;
    [Tooltip("Frecuencia del ruido de montañas (mayor = cimas más cortas y frecuentes).")]
    public float borderNoiseFreq = 0.018f;

    [Header("Tamaño del Terreno")]
    [Tooltip("Ancho y largo objetivo. El centro del mundo permanece fijo al redimensionar.")]
    public float targetTerrainWidth  = 800f;
    public float targetTerrainLength = 800f;
    [Tooltip("Cuando está activo, redimensiona el TerrainData antes de generar el heightmap.")]
    public bool resizeBeforeGenerate = false;

    [Header("Ruido fBm (multi-octava)")]
    public float baseFrequency  = 0.012f; // frecuencia base
    public float baseAmplitude  = 0.07f;  // amplitud base
    public int   octaves        = 4;      // cantidad de octavas
    [Range(0f, 1f)] public float persistence = 0.5f;  // amplitud se multiplica por esto cada octava
    public float lacunarity     = 2.1f;   // frecuencia se multiplica por esto cada octava

    [Header("Suavizado")]
    [Range(0, 6)] public int globalSmoothPasses = 2;

    [Header("Texturas (opcional)")]
    [Tooltip("Layer 0 = arena/tierra, Layer 1 = roca (paredes del cañón), Layer 2 = arena clara (meseta). Asigná las capas en el componente Terrain.")]
    public bool paintTextures = true;

    [Header("Runtime")]
    public bool generateOnPlay = true;

    // ── Punto de entrada ─────────────────────────────────────────────────────

    void Start()
    {
        if (generateOnPlay) Generate();
    }

    // ── Redimensionar TerrainData ─────────────────────────────────────────────

    /// <summary>
    /// Cambia td.size a targetTerrainWidth × targetTerrainLength manteniendo el centro
    /// del terreno en la misma posición del mundo.
    /// Los waypoints y BuildSlots NO se mueven en XZ (su posición absoluta es independiente
    /// del tamaño del TerrainData).  RepositionWaypoints los baja/sube al nuevo heightmap
    /// después de generar la forma.
    /// </summary>
    [ContextMenu("1) Redimensionar Terreno a 800×800 (sin regenerar)")]
    public void ResizeTerrain()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("[TerrainShaper] No hay Terrain en la escena.");
            return;
        }

        TerrainData td      = terrain.terrainData;
        Vector3     oldSize = td.size;
        Vector3     oldPos  = terrain.transform.position;

        // Centro actual del terreno en el mundo (XZ)
        Vector3 oldCenter = new Vector3(
            oldPos.x + oldSize.x * 0.5f,
            oldPos.y,
            oldPos.z + oldSize.z * 0.5f);

        // Cambiar solo X y Z del size (Y = maxHeight, se conserva)
        td.size = new Vector3(targetTerrainWidth, oldSize.y, targetTerrainLength);

        // Reposicionar para que el centro del mundo quede fijo
        terrain.transform.position = new Vector3(
            oldCenter.x - targetTerrainWidth  * 0.5f,
            oldPos.y,
            oldCenter.z - targetTerrainLength * 0.5f);

        Debug.Log($"[TerrainShaper] TerrainData redimensionado: {oldSize.x}×{oldSize.z} → " +
                  $"{targetTerrainWidth}×{targetTerrainLength}. " +
                  $"Posición terreno: {terrain.transform.position}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("Aplanar Terreno (altura uniforme 0.1)")]
    public void Flatten()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("[TerrainShaper] No hay Terrain en la escena.");
            return;
        }

        TerrainData td  = terrain.terrainData;
        int         res = td.heightmapResolution;
        float[,] heights = new float[res, res];

        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                heights[z, x] = 0.1f;

        td.SetHeights(0, 0, heights);

        // Limpiar alphamap: todo Layer 0
        int alphaRes = td.alphamapResolution;
        int layerCnt = td.terrainLayers?.Length ?? 0;
        if (layerCnt > 0)
        {
            float[,,] alpha = new float[alphaRes, alphaRes, layerCnt];
            for (int z = 0; z < alphaRes; z++)
                for (int x = 0; x < alphaRes; x++)
                    alpha[z, x, 0] = 1f;
            td.SetAlphamaps(0, 0, alpha);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("2) Generar Terreno (usar tras redimensionar)")]
    public void Generate()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("[TerrainShaper] No hay Terrain en la escena.");
            return;
        }

        // Redimensionar primero si se pidió (p.ej. al pasar de 500 a 800)
        if (resizeBeforeGenerate) ResizeTerrain();

        Vector3[] waypoints = GetWaypointPositions();
        if (waypoints.Length < 2) { /* genera sin cañón */ }

        ShapeHeights(waypoints);

        if (globalSmoothPasses > 0)
            SmoothHeights(globalSmoothPasses);

        if (paintTextures)
            PaintTextures(waypoints);

        RepositionWaypoints(waypoints);
        RepositionBuildSlots();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    // ── Forma del heightmap ───────────────────────────────────────────────────

    void ShapeHeights(Vector3[] waypoints)
    {
        TerrainData td  = terrain.terrainData;
        int         res = td.heightmapResolution;
        float[,] heights = new float[res, res];

        Vector3 tPos  = terrain.transform.position;
        Vector3 tSize = td.size;

        bool hasCanyon = waypoints.Length >= 2;

        for (int zi = 0; zi < res; zi++)
        {
            for (int xi = 0; xi < res; xi++)
            {
                float nx = (float)xi / (res - 1);   // 0..1
                float nz = (float)zi / (res - 1);   // 0..1
                float wx = tPos.x + nx * tSize.x;
                float wz = tPos.z + nz * tSize.z;

                // ── 1. Altura de borde (montañas en el anillo exterior) ───────
                float edgeX    = Mathf.Min(nx, 1f - nx);
                float edgeZ    = Mathf.Min(nz, 1f - nz);
                float edgeFrac = Mathf.Min(edgeX, edgeZ); // 0 = en borde, 0.5 = centro

                // borderT: 0 = dentro del mapa, 1 = en el borde
                float borderT = Mathf.Clamp01(1f - edgeFrac / borderFraction);
                borderT = Mathf.Pow(borderT, borderSteepness);

                // Ruido de montaña: frecuencia distinta a la del centro + offset para
                // que el patrón sea completamente diferente al ruido central.
                // Se escala con borderT → cero en la zona jugable, máximo en el borde.
                float mtnNoise = FractionalBrownianMotion(
                    (wx + 371.5f) * (borderNoiseFreq / Mathf.Max(baseFrequency, 0.0001f)),
                    (wz + 452.3f) * (borderNoiseFreq / Mathf.Max(baseFrequency, 0.0001f)))
                    * borderNoiseAmp * borderT;

                float borderH = Mathf.Lerp(plateauHeight, borderPeak, borderT) + mtnNoise;

                // ── 2. Ruido fBm central para irregularidad natural ────────────
                float noise = FractionalBrownianMotion(wx, wz);

                // ── 3. Altura base = meseta + ruido (antes del cañón) ─────────
                float baseH = plateauHeight + noise;

                // Los bordes dominan sobre la meseta+ruido cuando son más altos
                baseH = Mathf.Max(baseH, borderH);

                // ── 4. Tallar el cañón ────────────────────────────────────────
                if (hasCanyon)
                {
                    float distPath = DistanceToPathXZ(new Vector3(wx, 0, wz), waypoints);
                    float totalWall = canyonHalfWidth + wallWidth;

                    if (distPath < totalWall)
                    {
                        float canyonH;

                        if (distPath <= canyonHalfWidth)
                        {
                            // Fondo plano del cañón
                            canyonH = canyonFloor;
                        }
                        else
                        {
                            // Pared: transición del fondo a la meseta
                            // t = 0 en el borde del fondo, 1 en el inicio de la meseta
                            float t = (distPath - canyonHalfWidth) / wallWidth;
                            // Curva empinada: usa potencia para pared más vertical
                            t = Mathf.Pow(t, wallSteepness);
                            t = Mathf.Clamp01(t);
                            canyonH = Mathf.Lerp(canyonFloor, plateauHeight, t);
                        }

                        // Solo bajamos: el cañón nunca levanta terreno
                        baseH = Mathf.Min(baseH, canyonH);
                    }
                }

                heights[zi, xi] = Mathf.Clamp01(baseH);
            }
        }

        td.SetHeights(0, 0, heights);
    }

    // ── Ruido Perlin multi-octava (fBm) ───────────────────────────────────────

    float FractionalBrownianMotion(float x, float z)
    {
        float value     = 0f;
        float amplitude = baseAmplitude;
        float frequency = baseFrequency;

        for (int o = 0; o < octaves; o++)
        {
            // Offsets distintos por octava para evitar correlación
            float sample = Mathf.PerlinNoise(
                x * frequency + 37.3f + o * 17.1f,
                z * frequency + 19.7f + o * 31.9f);

            value     += (sample - 0.5f) * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value;
    }

    // ── Suavizado del heightmap ───────────────────────────────────────────────

    void SmoothHeights(int passes)
    {
        TerrainData td  = terrain.terrainData;
        int         res = td.heightmapResolution;
        float[,] h = td.GetHeights(0, 0, res, res);

        for (int p = 0; p < passes; p++)
        {
            float[,] s = (float[,])h.Clone();
            for (int z = 1; z < res - 1; z++)
            {
                for (int x = 1; x < res - 1; x++)
                {
                    // Kernel 3×3 ponderado (centro×4, adyacentes×1, diagonales×0.5)
                    s[z, x] = (h[z,   x]   * 4f +
                               h[z-1, x]   + h[z+1, x] +
                               h[z,   x-1] + h[z,   x+1] +
                              (h[z-1, x-1] + h[z-1, x+1] +
                               h[z+1, x-1] + h[z+1, x+1]) * 0.5f)
                              / 10f;
                }
            }
            h = s;
        }

        td.SetHeights(0, 0, h);
    }

    // ── Pintar texturas según zona ────────────────────────────────────────────
    // Layer 0 = arena/tierra (todo el mapa por defecto)
    // Layer 1 = roca (paredes del cañón y bordes)
    // Layer 2 = arena clara (meseta plana, si existe)

    void PaintTextures(Vector3[] waypoints)
    {
        TerrainData td = terrain.terrainData;

        if (td.terrainLayers == null || td.terrainLayers.Length < 2) return;

        int alphaRes = td.alphamapResolution;
        int layerCnt = td.terrainLayers.Length;
        float[,,] alpha = new float[alphaRes, alphaRes, layerCnt];

        bool hasCanyon = waypoints.Length >= 2;
        Vector3 tPos  = terrain.transform.position;
        Vector3 tSize = td.size;

        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float normX = (float)x / (alphaRes - 1);
                float normZ = (float)z / (alphaRes - 1);
                float height = td.GetInterpolatedHeight(normX, normZ) / td.size.y;

                float wx = tPos.x + normX * tSize.x;
                float wz = tPos.z + normZ * tSize.z;

                // Fracción de roca según la pendiente y la proximidad al cañón
                float rockBlend = 0f;
                if (hasCanyon)
                {
                    float distPath = DistanceToPathXZ(new Vector3(wx, 0, wz), waypoints);
                    float wallStart = canyonHalfWidth;
                    float wallEnd   = canyonHalfWidth + wallWidth;
                    // Zona de pared → roca pura
                    if (distPath > wallStart && distPath < wallEnd)
                    {
                        float wallT = (distPath - wallStart) / wallWidth;
                        // Pico de roca en la mitad de la pared
                        rockBlend = Mathf.Sin(wallT * Mathf.PI);
                    }
                }

                // Altura alta (bordes) → también algo de roca
                float edgeRock = Mathf.Clamp01((height - plateauHeight * 1.2f) /
                                               (borderPeak - plateauHeight * 1.2f + 0.001f));
                rockBlend = Mathf.Max(rockBlend, edgeRock * 0.6f);
                rockBlend = Mathf.Clamp01(rockBlend);

                for (int l = 0; l < layerCnt; l++) alpha[z, x, l] = 0f;

                if (layerCnt >= 3)
                {
                    // Layer 2 = arena clara en la meseta (poco roca, poco arena oscura)
                    float plateauBlend = Mathf.Clamp01(1f - rockBlend - 0.1f);
                    alpha[z, x, 0] = (1f - rockBlend) * 0.4f;   // arena/tierra
                    alpha[z, x, 1] = rockBlend;                   // roca
                    alpha[z, x, 2] = plateauBlend * 0.6f;         // arena clara
                }
                else
                {
                    alpha[z, x, 0] = 1f - rockBlend;  // arena
                    alpha[z, x, 1] = rockBlend;         // roca
                }
            }
        }

        td.SetAlphamaps(0, 0, alpha);
    }

    // ── Reposicionar waypoints al nuevo terreno ───────────────────────────────

    void RepositionWaypoints(Vector3[] oldPositions)
    {
        if (WaypointPath.Instance == null) return;
        Transform[] pts = WaypointPath.Instance.points;
        if (pts == null) return;

        foreach (Transform wp in pts)
        {
            if (wp == null) continue;
            float y = SampleTerrainY(wp.position.x, wp.position.z);
            wp.position = new Vector3(wp.position.x, y, wp.position.z);
        }
    }

    // ── Reposicionar BuildSlots al nuevo terreno ──────────────────────────────

    void RepositionBuildSlots()
    {
        BuildSlot[] slots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        foreach (BuildSlot slot in slots)
        {
            float y = SampleTerrainY(slot.transform.position.x, slot.transform.position.z);
            slot.transform.position = new Vector3(
                slot.transform.position.x,
                y + 0.05f,
                slot.transform.position.z);
        }
    }

    // ── Helpers de geometría ─────────────────────────────────────────────────

    float SampleTerrainY(float wx, float wz)
    {
        if (terrain == null) return 0f;
        return terrain.SampleHeight(new Vector3(wx, 0, wz))
             + terrain.transform.position.y;
    }

    static float DistanceToPathXZ(Vector3 point, Vector3[] waypoints)
    {
        float min = float.MaxValue;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            float d = DistToSegmentXZ(point,
                new Vector3(waypoints[i].x,   0, waypoints[i].z),
                new Vector3(waypoints[i+1].x, 0, waypoints[i+1].z));
            if (d < min) min = d;
        }
        return min;
    }

    static float DistToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        if (ab.sqrMagnitude < 0.001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector3 closest = a + t * ab;
        return new Vector2(p.x - closest.x, p.z - closest.z).magnitude;
    }

    static Vector3[] GetWaypointPositions()
    {
        if (WaypointPath.Instance == null || WaypointPath.Instance.points == null)
            return new Vector3[0];

        var pts = WaypointPath.Instance.points;
        var result = new Vector3[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            result[i] = pts[i] != null ? pts[i].position : Vector3.zero;
        return result;
    }
}
