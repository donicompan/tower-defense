using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Pinta las texturas del terreno con capas del paquete "Realistic Terrain Textures Lite".
///
/// Menú: Tools → Pintar Texturas del Terreno (Desierto Cañón)
///
/// Asignación de layers:
///   [0] Ground001 → Arena clara     (meseta plana, zonas abiertas)
///   [1] Ground004 → Roca            (paredes del cañón, pendientes pronunciadas)
///   [2] Ground003 → Tierra oscura   (transición, taludes suaves, bordes)
///   [3] Ground005 → Tierra agrietada (fondo plano del cañón — GroundPatchCracked01)
///
/// Lógica de pintura (por pixel del alphamap):
///   - Distancia al camino (waypoints) → controla arena / roca / grietas
///   - Normal del terreno (pendiente) → roca en pendientes > slopeThreshold
///   - Combinación normalizada de los cuatro pesos
/// </summary>
public static class TerrainTexturePainter
{
    const string LAYERS_ROOT =
        "Assets/ALP_Assets/Realistic Terrain Textures Lite/Terrain Layers/";

    // ── Parámetros de pintura — editables aquí o extendibles al Inspector ────
    const float SLOPE_ROCK_START  = 0.22f; // 0=plano 1=vertical; por encima → roca
    const float SLOPE_ROCK_FULL   = 0.52f; // pendiente en la que la roca domina 100 %
    const float SLOPE_DIRT_START  = 0.10f; // pendiente desde donde aparece tierra oscura
    const float WALL_PEAK_BLEND   = 0.85f; // peso máximo de roca en el pico de la pared

    // ── Menú principal ────────────────────────────────────────────────────────

    [MenuItem("Tools/Pintar Texturas del Terreno (Desierto Cañón)")]
    public static void Paint()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[TerrainTexturePainter] No hay Terrain activo en la escena.");
            return;
        }

        TerrainShaper shaper = Object.FindFirstObjectByType<TerrainShaper>();

        // 1. Cargar y asignar los TerrainLayers al TerrainData
        TerrainLayer[] layers = LoadLayers();
        if (layers == null) return;

        Undo.RecordObject(terrain.terrainData, "Pintar texturas terreno");
        terrain.terrainData.terrainLayers = layers;

        // 2. Pintar el alphamap
        PaintAlphamap(terrain, shaper);

        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[TerrainTexturePainter] ¡Texturas aplicadas correctamente!\n" +
                  "  [0] Arena (Ground001)  [1] Roca (Ground004)\n" +
                  "  [2] Tierra oscura (Ground003)  [3] Agrietada (Ground005/GroundPatchCracked01)");
    }

    // ── Carga de layers ───────────────────────────────────────────────────────

    static TerrainLayer[] LoadLayers()
    {
        // Orden deliberado: [0]=arena [1]=roca [2]=tierra oscura [3]=agrietada
        string[] names = { "Ground001", "Ground004", "Ground003", "Ground005" };
        var layers = new TerrainLayer[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            string path = LAYERS_ROOT + names[i] + ".terrainlayer";
            layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layers[i] == null)
            {
                Debug.LogError($"[TerrainTexturePainter] No se encontró el terrain layer:\n  {path}\n" +
                               "Verificá que el paquete esté importado correctamente.");
                return null;
            }
        }
        return layers;
    }

    // ── Pintura del alphamap ──────────────────────────────────────────────────

    static void PaintAlphamap(Terrain terrain, TerrainShaper shaper)
    {
        TerrainData td  = terrain.terrainData;
        int         res = td.alphamapResolution;
        int         nL  = td.terrainLayers.Length; // 4

        float[,,] alpha = new float[res, res, nL];

        // Parámetros del cañón (tomados del TerrainShaper si existe)
        float canyonHalf = shaper != null ? shaper.canyonHalfWidth : 3f;
        float wallW      = shaper != null ? shaper.wallWidth        : 4f;

        Vector3[] waypoints = GetWaypointPositions();
        bool      hasPath   = waypoints.Length >= 2;

        Vector3 tPos  = terrain.transform.position;
        Vector3 tSize = td.size;

        for (int az = 0; az < res; az++)
        {
            for (int ax = 0; ax < res; ax++)
            {
                float normX = (float)ax / (res - 1);
                float normZ = (float)az / (res - 1);

                // Pendiente a partir de la normal interpolada del terreno
                Vector3 normal = td.GetInterpolatedNormal(normX, normZ);
                float   slope  = 1f - normal.y; // 0 = plano, 1 = vertical

                // Posición world de este texel
                float wx = tPos.x + normX * tSize.x;
                float wz = tPos.z + normZ * tSize.z;

                float dist = hasPath
                    ? DistToPath(new Vector3(wx, 0f, wz), waypoints)
                    : float.MaxValue;

                // ── [3] Tierra agrietada — fondo del cañón ────────────────────
                // Máximo en el centro exacto del cañón, cae a 0 en canyonHalf
                float cracked = 0f;
                if (dist < canyonHalf + 1f)
                    cracked = Mathf.Clamp01(1f - dist / (canyonHalf + 0.5f));

                // ── [1] Roca — paredes del cañón + pendientes pronunciadas ────
                // Componente de pendiente
                float rockSlope = Mathf.Clamp01((slope - SLOPE_ROCK_START) /
                                                (SLOPE_ROCK_FULL - SLOPE_ROCK_START));
                // Componente de pared del cañón (pico seno en la mitad de la pared)
                float rockWall = 0f;
                if (hasPath && dist > canyonHalf && dist < canyonHalf + wallW)
                {
                    float wallT = (dist - canyonHalf) / wallW;
                    rockWall = Mathf.Sin(wallT * Mathf.PI) * WALL_PEAK_BLEND;
                }
                float rock = Mathf.Max(rockSlope, rockWall);
                // La roca no pisa el fondo ya cubierto por cracked
                rock = Mathf.Clamp01(rock * (1f - cracked));

                // ── [2] Tierra oscura — transición suave entre arena y roca ──
                float dirt = 0f;
                if (slope > SLOPE_DIRT_START)
                    dirt = Mathf.Clamp01((slope - SLOPE_DIRT_START) /
                                         (SLOPE_ROCK_START - SLOPE_DIRT_START + 0.01f));
                dirt = dirt * (1f - rock) * (1f - cracked) * 0.65f;

                // ── [0] Arena — relleno base ──────────────────────────────────
                float sand = Mathf.Max(0f, 1f - rock - dirt - cracked);

                // ── Normalizar para que la suma sea exactamente 1 ─────────────
                float sum = sand + rock + dirt + cracked;
                if (sum < 0.0001f) { sand = 1f; sum = 1f; }

                alpha[az, ax, 0] = sand    / sum;
                alpha[az, ax, 1] = rock    / sum;
                alpha[az, ax, 2] = dirt    / sum;
                alpha[az, ax, 3] = cracked / sum;
            }
        }

        td.SetAlphamaps(0, 0, alpha);
    }

    // ── Helpers de geometría ──────────────────────────────────────────────────

    static Vector3[] GetWaypointPositions()
    {
        // FindFirstObjectByType funciona en Edit Mode (no depende de Instance/Awake)
        WaypointPath wp = Object.FindFirstObjectByType<WaypointPath>();
        if (wp == null || wp.points == null) return new Vector3[0];

        var result = new Vector3[wp.points.Length];
        for (int i = 0; i < wp.points.Length; i++)
            result[i] = wp.points[i] != null ? wp.points[i].position : Vector3.zero;
        return result;
    }

    static float DistToPath(Vector3 point, Vector3[] waypoints)
    {
        float min = float.MaxValue;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            float d = DistToSegXZ(point,
                new Vector3(waypoints[i].x,   0f, waypoints[i].z),
                new Vector3(waypoints[i+1].x, 0f, waypoints[i+1].z));
            if (d < min) min = d;
        }
        return min;
    }

    static float DistToSegXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        if (ab.sqrMagnitude < 0.001f) return Vector2.Distance(new Vector2(p.x, p.z),
                                                               new Vector2(a.x, a.z));
        float   t  = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector3 cl = a + t * ab;
        return new Vector2(p.x - cl.x, p.z - cl.z).magnitude;
    }
}
