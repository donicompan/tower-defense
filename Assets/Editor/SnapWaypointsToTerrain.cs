using UnityEngine;
using UnityEditor;

public static class SnapWaypointsToTerrain
{
    [MenuItem("Tools/Snap Waypoints al Terreno")]
    public static void Snap()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { Debug.LogError("No hay Terrain activo."); return; }

        WaypointPath wp = Object.FindFirstObjectByType<WaypointPath>();
        if (wp == null || wp.points == null || wp.points.Length == 0)
        {
            Debug.LogError("No se encontró WaypointPath con puntos en la escena.");
            return;
        }

        Undo.RecordObjects(wp.points, "Snap Waypoints al Terreno");

        int snapped = 0;
        foreach (Transform pt in wp.points)
        {
            if (pt == null) continue;

            float y = terrain.SampleHeight(pt.position) + terrain.transform.position.y;
            pt.position = new Vector3(pt.position.x, y, pt.position.z);
            snapped++;
        }

        Debug.Log($"[SnapWaypoints] {snapped} waypoints bajados al terreno.");
    }
}
