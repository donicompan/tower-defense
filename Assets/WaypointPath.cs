using UnityEngine;

// Guarda todos los puntos del camino en orden.
// Los enemigos leen esto al nacer para saber por dónde ir.
public class WaypointPath : MonoBehaviour
{
    public static WaypointPath Instance;
    public Transform[] points;  // Arrastrar los puntos en Unity

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Dibuja líneas entre waypoints en el editor (muy útil para diseño)
    void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
}
