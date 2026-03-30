using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// TumbleweedManager — spawns tumbleweeds periodically at map edges
// TumbleweedMover   — per-tumbleweed physics (movement, bounce, roll)
// ─────────────────────────────────────────────────────────────────────────────

public class TumbleweedManager : MonoBehaviour
{
    [Header("Spawn")]
    public float spawnIntervalMin = 6f;
    public float spawnIntervalMax = 14f;

    [Header("Viento")]
    public float windSpeedMin = 5f;
    public float windSpeedMax = 10f;

    [Header("Límites del mapa (igual que CameraController)")]
    public float mapMinX = -55f;
    public float mapMaxX =  55f;
    public float mapMinZ = -55f;
    public float mapMaxZ =  55f;

    void Start() => StartCoroutine(SpawnLoop());

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
            SpawnTumbleweed();
        }
    }

    void SpawnTumbleweed()
    {
        Vector3 startPos;
        Vector3 direction;

        // Elije un lado del mapa al azar y cruza al opuesto
        if (Random.value > 0.5f)
        {
            bool fromLeft = Random.value > 0.5f;
            float z = Random.Range(mapMinZ + 8f, mapMaxZ - 8f);
            startPos = new Vector3(fromLeft ? mapMinX - 2f : mapMaxX + 2f, 0f, z);
            float xDir = fromLeft ? 1f : -1f;
            direction = new Vector3(xDir, 0f, Random.Range(-0.25f, 0.25f)).normalized;
        }
        else
        {
            bool fromBottom = Random.value > 0.5f;
            float x = Random.Range(mapMinX + 8f, mapMaxX - 8f);
            startPos = new Vector3(x, 0f, fromBottom ? mapMinZ - 2f : mapMaxZ + 2f);
            float zDir = fromBottom ? 1f : -1f;
            direction = new Vector3(Random.Range(-0.25f, 0.25f), 0f, zDir).normalized;
        }

        // Alinear con el terreno (con fallback si "Terrain" layer no existe)
        int terrainMask = LayerMask.GetMask("Terrain");
        startPos.y = SampleTerrainY(startPos, terrainMask) + 0.5f;

        GameObject tw = BuildTumbleweedMesh();
        tw.transform.position = startPos;

        TumbleweedMover mover = tw.AddComponent<TumbleweedMover>();
        mover.Init(direction,
                   Random.Range(windSpeedMin, windSpeedMax),
                   mapMinX - 6f, mapMaxX + 6f,
                   mapMinZ - 6f, mapMaxZ + 6f);
    }

    static GameObject BuildTumbleweedMesh()
    {
        float size = Random.Range(0.45f, 1.1f);

        // Color paja seca: beige-marrón oscuro
        Color baseCol = new Color(
            Random.Range(0.52f, 0.68f),
            Random.Range(0.35f, 0.48f),
            Random.Range(0.10f, 0.22f));

        GameObject root = new GameObject("Tumbleweed");
        root.transform.localScale = Vector3.one * size;

        // Esfera central
        AddSphere(root.transform, Vector3.zero, 1f, baseCol);

        // Esferas irregulares para dar forma de matorral
        for (int i = 0; i < 5; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.55f;
            float subSize  = Random.Range(0.45f, 0.75f);
            Color subCol   = baseCol * Random.Range(0.80f, 1.15f);
            subCol.a = 1f;
            AddSphere(root.transform, offset, subSize, subCol);
        }

        return root;
    }

    // Raycast con fallback sin máscara
    static float SampleTerrainY(Vector3 pos, int mask)
    {
        Vector3 origin = new Vector3(pos.x, 200f, pos.z);
        if (mask != 0 && Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f, mask))
            return hit.point.y;
        if (Physics.Raycast(origin, Vector3.down, out hit, 400f))
            return hit.point.y;
        return 0f;
    }

    static void AddSphere(Transform parent, Vector3 localPos, float scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * scale;
        Object.Destroy(go.GetComponent<Collider>());

        Material mat = GameMaterials.MakeLit(color);
        if (mat != null) go.GetComponent<Renderer>().material = mat;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class TumbleweedMover : MonoBehaviour
{
    private Vector3 _dir;
    private float   _speed;
    private float   _minX, _maxX, _minZ, _maxZ;
    private float   _rollSpeed;
    private float   _bouncePhase;
    private float   _groundY;
    private float   _radius;
    private int     _terrainMask;

    public void Init(Vector3 dir, float speed,
                     float minX, float maxX, float minZ, float maxZ)
    {
        _dir        = dir;
        _speed      = speed;
        _minX = minX; _maxX = maxX;
        _minZ = minZ; _maxZ = maxZ;
        _rollSpeed   = speed * Random.Range(100f, 160f);
        _bouncePhase = Random.Range(0f, Mathf.PI * 2f);
        _groundY     = transform.position.y;
        _radius      = transform.localScale.x * 0.5f;
        _terrainMask = LayerMask.GetMask("Terrain");
    }

    void Update()
    {
        // Movimiento horizontal
        transform.position += _dir * _speed * Time.deltaTime;

        // Rodado alrededor del eje perpendicular a la dirección
        Vector3 rollAxis = Vector3.Cross(_dir, Vector3.up);
        transform.Rotate(rollAxis, _rollSpeed * Time.deltaTime, Space.World);

        // Altura del terreno bajo el objeto (con fallback sin máscara)
        Vector3 pos    = transform.position;
        Vector3 origin = pos + Vector3.up * 5f;
        RaycastHit hit;
        bool landed = (_terrainMask != 0 && Physics.Raycast(origin, Vector3.down, out hit, 12f, _terrainMask))
                   || Physics.Raycast(origin, Vector3.down, out hit, 12f);
        if (landed) _groundY = hit.point.y + _radius;

        // Rebote suave
        float bounce = Mathf.Abs(Mathf.Sin(Time.time * 2.8f + _bouncePhase)) * 0.35f * _radius * 2f;
        pos.y = _groundY + bounce;
        transform.position = pos;

        // Auto-destruir al salir del mapa
        if (pos.x < _minX || pos.x > _maxX || pos.z < _minZ || pos.z > _maxZ)
            Destroy(gameObject);
    }
}
