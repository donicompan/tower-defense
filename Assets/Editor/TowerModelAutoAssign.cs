using UnityEngine;
using UnityEditor;

/// <summary>
/// Asigna automáticamente los modelos 3D de Kenney a los TowerData ScriptableObjects.
/// Menú: Tower Defense → Asignar modelos Kenney a torres
/// </summary>
public static class TowerModelAutoAssign
{
    private const string FBX_BASE =
        "Assets/Models/kenney_tower-defense-kit/Models/FBX format/";

    // ── Mapa: nombre del asset TowerData → archivo FBX + escala ──────────────
    private static readonly (string asset, string fbx, float scale, TowerAnim anim)[] Assignments =
    {
        ("Assets/TorreBasica.asset",         "tower-square-bottom-b.fbx",  3f, TowerAnim.FlagWave),
        ("Assets/TorreCañon.asset",          "weapon-cannon.fbx",          3f, TowerAnim.CannonBob),
        ("Assets/TorreFrancotirador.asset",  "tower-square-build-f.fbx",   3f, TowerAnim.SniperScan),
    };

    [MenuItem("Tower Defense/Asignar modelos Kenney a torres")]
    static void AssignModels()
    {
        int ok = 0, fail = 0;

        foreach (var (assetPath, fbxFile, scale, anim) in Assignments)
        {
            TowerData td = AssetDatabase.LoadAssetAtPath<TowerData>(assetPath);
            if (td == null)
            {
                Debug.LogWarning($"[TowerModelAutoAssign] No se encontró TowerData: {assetPath}");
                fail++;
                continue;
            }

            string fbxPath = FBX_BASE + fbxFile;
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null)
            {
                Debug.LogWarning($"[TowerModelAutoAssign] No se encontró modelo: {fbxPath}");
                fail++;
                continue;
            }

            td.modelPrefab = model;
            td.modelScale  = scale;
            td.idleAnim    = anim;
            EditorUtility.SetDirty(td);
            Debug.Log($"[TowerModelAutoAssign] ✓ {td.towerName} → {fbxFile} ({anim})");
            ok++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Asignación completada",
            $"Modelos asignados: {ok}\nErrores: {fail}\n\n" +
            "Recordá asignar manualmente los modelos de las 4 torres desierto " +
            "(Cactus, Arena, Sol, Tormenta) en el componente DesertTowerButtonsUI del Inspector.\n\n" +
            "Modelos sugeridos:\n" +
            "  Cactus   → detail-tree-large.fbx\n" +
            "  Arena    → tower-round-bottom-a.fbx\n" +
            "  Sol      → tower-round-crystals.fbx\n" +
            "  Tormenta → weapon-ballista.fbx",
            "OK");
    }

    // ── Validación: lista todos los TowerData sin modelo asignado ─────────────
    [MenuItem("Tower Defense/Verificar torres sin modelo")]
    static void VerifyModels()
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerData");
        int missing = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerData td = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (td != null && td.modelPrefab == null)
            {
                Debug.LogWarning($"[TowerModelAutoAssign] Sin modelo: {td.towerName} ({path})");
                missing++;
            }
        }
        if (missing == 0)
            Debug.Log("[TowerModelAutoAssign] Todas las torres tienen modelo asignado.");
        else
            Debug.LogWarning($"[TowerModelAutoAssign] {missing} torre(s) sin modelo (ver warnings arriba).");
    }
}
