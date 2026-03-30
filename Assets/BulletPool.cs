using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pool de objetos para balas. Elimina el CreatePrimitive + Destroy por disparo.
/// Se auto-crea en escena la primera vez que alguien accede a Instance.
/// </summary>
public class BulletPool : MonoBehaviour
{
    private static BulletPool _instance;
    public static BulletPool Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("BulletPool");
                _instance = go.AddComponent<BulletPool>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private const int INITIAL_SIZE = 40;
    private const int MAX_POOL_SIZE = 120;
    private int _totalCreated = 0;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        for (int i = 0; i < INITIAL_SIZE; i++)
            _pool.Enqueue(CreateBullet());
    }

    GameObject CreateBullet()
    {
        _totalCreated++;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PooledBullet";
        Destroy(go.GetComponent<Collider>());
        go.AddComponent<Bullet>();
        go.transform.SetParent(transform);
        go.SetActive(false);
        return go;
    }

    /// <summary>Obtiene una bala del pool, lista para usar.</summary>
    public GameObject Get(float size, Material mat)
    {
        GameObject go = _pool.Count > 0 ? _pool.Dequeue() :
                        (_totalCreated < MAX_POOL_SIZE ? CreateBullet() : null);
        if (go == null) return null;
        go.transform.SetParent(null);
        go.transform.localScale = Vector3.one * size;
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;
        go.SetActive(true);
        return go;
    }

    /// <summary>Devuelve una bala al pool para ser reutilizada.</summary>
    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform);
        _pool.Enqueue(go);
    }
}
