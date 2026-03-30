using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ImportHeightmapToTerrain
{
    [MenuItem("Tools/Importar Heightmap Desert al Terrain")]
    public static void Import()
    {
        // ── Terrain activo ────────────────────────────────────────────────
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[ImportHeightmap] No hay Terrain activo en la escena.");
            return;
        }

        // ── Asegurar que la textura sea legible ───────────────────────────
        string assetPath = "Assets/Resources/heightmap_desert.png";

        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError("[ImportHeightmap] No se encontró el asset en:\n" + assetPath +
                           "\nAsegurate de que Unity haya importado el archivo " +
                           "(hacé clic en la ventana de Unity para que lo detecte).");
            return;
        }

        if (!ti.isReadable || ti.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.isReadable         = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // ── Cargar la textura ─────────────────────────────────────────────
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            Debug.LogError("[ImportHeightmap] LoadAssetAtPath falló para:\n" + assetPath);
            return;
        }

        // ── Leer píxeles y volcarlos al heightmap ─────────────────────────
        int res = terrain.terrainData.heightmapResolution;
        float[,] heights = new float[res, res];

        for (int z = 0; z < res; z++)
        {
            float v = (float)z / (res - 1);
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1);
                // GetPixelBilinear interpola si el tamaño difiere del heightmap
                heights[z, x] = tex.GetPixelBilinear(u, v).r;
            }
        }

        // ── Aplicar ───────────────────────────────────────────────────────
        Undo.RecordObject(terrain.terrainData, "Importar Heightmap Desert");
        terrain.terrainData.SetHeights(0, 0, heights);

        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[ImportHeightmap] OK — heightmap {tex.width}×{tex.height} aplicado " +
                  $"al terrain '{terrain.name}' (heightmapResolution={res}).");
    }
}
