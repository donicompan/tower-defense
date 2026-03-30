using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// RESTAURACIÓN DE EMERGENCIA DEL TERRENO
/// Menú: Tools → Restaurar Terreno Ahora
///
/// 1. Redimensiona el TerrainData a 500×500 (preservando el centro del mundo).
/// 2. Aplica los parámetros originales al TerrainShaper.
/// 3. Llama Generate() para reconstruir el heightmap completo.
/// </summary>
public static class RestoreTerrainNow
{
    [MenuItem("Tools/Restaurar Terreno Ahora (500×500)")]
    public static void Run()
    {
        // ── 1. Encontrar Terrain y TerrainShaper ─────────────────────────────
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[RestoreTerrain] No hay Terrain activo en la escena.");
            return;
        }

        TerrainShaper shaper = Object.FindFirstObjectByType<TerrainShaper>();
        if (shaper == null)
        {
            Debug.LogError("[RestoreTerrain] No se encontró TerrainShaper en la escena.");
            return;
        }

        // ── 2. Restaurar tamaño a 500×500 preservando el centro del mundo ────
        TerrainData td      = terrain.terrainData;
        Vector3     oldSize = td.size;
        Vector3     oldPos  = terrain.transform.position;
        Vector3     center  = new Vector3(
            oldPos.x + oldSize.x * 0.5f,
            oldPos.y,
            oldPos.z + oldSize.z * 0.5f);

        const float TARGET_W = 500f;
        const float TARGET_L = 500f;

        td.size = new Vector3(TARGET_W, oldSize.y, TARGET_L);
        terrain.transform.position = new Vector3(
            center.x - TARGET_W * 0.5f,
            oldPos.y,
            center.z - TARGET_L * 0.5f);

        Debug.Log($"[RestoreTerrain] Terreno redimensionado: {oldSize.x}×{oldSize.z} → 500×500. " +
                  $"Pos: {terrain.transform.position}");

        // ── 3. Aplicar parámetros originales al TerrainShaper ────────────────
        shaper.terrain         = terrain;

        shaper.canyonFloor     = 0.08f;
        shaper.plateauHeight   = 0.20f;
        shaper.borderPeak      = 0.45f;

        shaper.canyonHalfWidth = 3f;
        shaper.wallWidth       = 4f;
        shaper.wallSteepness   = 4f;

        shaper.borderFraction  = 0.20f;
        shaper.borderSteepness = 2f;

        shaper.baseFrequency   = 0.012f;
        shaper.baseAmplitude   = 0.09f;
        shaper.octaves         = 4;
        shaper.persistence     = 0.5f;
        shaper.lacunarity      = 2.1f;

        shaper.globalSmoothPasses = 2;
        shaper.paintTextures      = true;

        // Asegurar que NO redimensione de nuevo al generar
        shaper.resizeBeforeGenerate = false;

        // ── 4. Regenerar el heightmap ─────────────────────────────────────────
        shaper.Generate();

        // ── 5. Marcar la escena como modificada ───────────────────────────────
        EditorUtility.SetDirty(td);
        EditorUtility.SetDirty(terrain.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[RestoreTerrain] ¡Terreno restaurado correctamente!");
    }
}
