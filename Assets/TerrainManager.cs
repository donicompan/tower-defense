using UnityEngine;

/// <summary>
/// Sistema de terreno dinámico.
///   Backup()     — llamado en GameManager.Awake(): copia el heightmap en RAM.
///   FlattenAt()  — llamado por FreeBuildManager al construir: aplana un área.
///   Restore()    — llamado al salir de la partida: devuelve el heightmap original.
///
/// El backup vive solo en RAM; nunca modifica el archivo .asset del proyecto.
/// Como medida extra de seguridad también se restaura automáticamente al cerrar
/// la aplicación (Application.quitting).
/// </summary>
public static class TerrainManager
{
    private static Terrain  _terrain;
    private static float[,] _backup;   // [z_index, x_index], rango [0, 1]

    // Registro automático del handler de quit (se ejecuta antes de cargar la escena)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterQuitHandler() => Application.quitting += Restore;

    // ── Backup ────────────────────────────────────────────────────────────────

    /// <summary>Guarda una copia completa del heightmap del Terrain activo.</summary>
    public static void Backup()
    {
        _terrain = Terrain.activeTerrain;
        if (_terrain == null) return;

        TerrainData td  = _terrain.terrainData;
        int         res = td.heightmapResolution;
        _backup = td.GetHeights(0, 0, res, res);   // copia profunda en RAM
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el heightmap al estado guardado en Backup().
    /// Seguro llamarlo más de una vez; las llamadas extra son no-ops.
    /// </summary>
    public static void Restore()
    {
        if (_terrain == null || _backup == null) return;

        _terrain.terrainData.SetHeights(0, 0, _backup);
        _terrain = null;
        _backup  = null;
    }

    // ── FlattenAt ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplana el terreno alrededor de <paramref name="worldPos"/>.
    /// <paramref name="flatRadius"/>  : radio (unidades mundo) completamente plano.
    /// <paramref name="blendRadius"/> : anillo de transición suave hacia el terreno circundante.
    /// La altura de aplanado es la del centro en el momento del llamado, no la del backup,
    /// para que la torre quede a ras del suelo actual.
    /// </summary>
    public static void FlattenAt(Vector3 worldPos, float flatRadius, float blendRadius)
    {
        if (_terrain == null || _backup == null) return;

        TerrainData td     = _terrain.terrainData;
        Vector3     origin = _terrain.transform.position;
        int         res    = td.heightmapResolution;

        // ── Convertir posición world a índices del heightmap ──────────────────
        float normX = Mathf.Clamp01((worldPos.x - origin.x) / td.size.x);
        float normZ = Mathf.Clamp01((worldPos.z - origin.z) / td.size.z);
        int cx = Mathf.RoundToInt(normX * (res - 1));
        int cz = Mathf.RoundToInt(normZ * (res - 1));

        // Altura normalizada del centro (nivel al que aplanamos)
        float targetH = td.GetHeights(cx, cz, 1, 1)[0, 0];

        // ── Calcular radio en píxeles ─────────────────────────────────────────
        float pixelsPerUnit = (res - 1) / td.size.x;
        int pixFlat  = Mathf.CeilToInt(flatRadius  * pixelsPerUnit);
        int pixBlend = Mathf.CeilToInt(blendRadius * pixelsPerUnit);
        int pixTotal = pixFlat + pixBlend;

        // ── Recorte al tamaño del heightmap ───────────────────────────────────
        int xMin = Mathf.Max(0, cx - pixTotal);
        int zMin = Mathf.Max(0, cz - pixTotal);
        int xMax = Mathf.Min(res - 1, cx + pixTotal);
        int zMax = Mathf.Min(res - 1, cz + pixTotal);
        int pW   = xMax - xMin + 1;
        int pH   = zMax - zMin + 1;

        // ── Leer, modificar y escribir el parche ──────────────────────────────
        float[,] patch = td.GetHeights(xMin, zMin, pW, pH);

        for (int pz = 0; pz < pH; pz++)
        {
            for (int px = 0; px < pW; px++)
            {
                float dx   = (xMin + px) - cx;
                float dz   = (zMin + pz) - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                float t;
                if      (dist <= pixFlat)
                    t = 1f;
                else if (dist <= pixFlat + pixBlend)
                    t = 1f - Mathf.SmoothStep(0f, 1f, (dist - pixFlat) / pixBlend);
                else
                    continue;   // fuera del área — no tocar

                patch[pz, px] = Mathf.Lerp(patch[pz, px], targetH, t);
            }
        }

        td.SetHeights(xMin, zMin, patch);
    }
}
