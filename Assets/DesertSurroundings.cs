using UnityEngine;

/// <summary>
/// 1) Extiende el terreno visualmente con un plano enorme color arena que rodea el mapa,
///    eliminando el vacío negro en los bordes.
/// 2) Configura el skybox procedural de Unity con colores de desierto (cielo azul-polvoriento,
///    horizonte cálido, suelo arena). No requiere assets externos.
/// Añadir este script a cualquier GameObject vacío en la escena (p.ej. "Ambient").
/// </summary>
public class DesertSurroundings : MonoBehaviour
{
    [Header("Plano exterior")]
    [Tooltip("Extensión total del plano de arena (world units). Debe ser mucho mayor que el mapa.")]
    public float planeSize   = 800f;
    [Tooltip("Y del plano. Negativo para que quede bajo el borde del terreno.")]
    public float planeY      = -0.5f;
    public Color sandColor   = new Color(0.74f, 0.62f, 0.40f);
    [Range(0f, 0.15f)]
    public float sandSmoothness = 0.05f;

    [Header("Skybox procedural (Skybox/Procedural)")]
    public bool  setupSkybox         = true;
    public Color skyTint             = new Color(0.52f, 0.68f, 0.92f);
    public Color groundColor         = new Color(0.58f, 0.46f, 0.28f);
    [Range(0.01f, 0.2f)]
    public float sunSize             = 0.05f;
    [Range(0f, 5f)]
    public float atmosphereThickness = 1.25f;
    [Range(0.5f, 2f)]
    public float skyExposure         = 1.1f;

    void Start()
    {
        CreateGroundPlane();
        if (setupSkybox) ApplySkybox();
    }

    // ── Plano de arena ────────────────────────────────────────────────────────

    void CreateGroundPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "DesertFloor";
        plane.transform.SetParent(transform);
        plane.transform.position   = new Vector3(0f, planeY, 0f);
        // Un Plane primitivo de Unity mide 10×10 unidades a escala 1.
        float s = planeSize / 10f;
        plane.transform.localScale = new Vector3(s, 1f, s);

        Destroy(plane.GetComponent<Collider>());

        Renderer rend = plane.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        Material mat = GameMaterials.MakeLit(sandColor);
        if (mat == null) return;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sandSmoothness);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        rend.material = mat;
    }

    // ── Skybox ────────────────────────────────────────────────────────────────

    void ApplySkybox()
    {
        // Establecer luz ambiente plana cálida ANTES del skybox para evitar
        // que la skybox azul predeterminada de Unity tiña la escena.
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.43f, 0.32f);

        // "Skybox/Procedural" es un shader integrado en Unity; no necesita assets externos.
        Shader sh = Shader.Find("Skybox/Procedural");
        if (sh == null)
        {
            Debug.LogWarning("[DesertSurroundings] Shader 'Skybox/Procedural' no encontrado.");
            return;
        }

        Material sky = new Material(sh);
        sky.SetColor("_SkyTint",             skyTint);
        sky.SetColor("_GroundColor",         groundColor);
        sky.SetFloat("_SunSize",             sunSize);
        sky.SetFloat("_AtmosphereThickness", atmosphereThickness);
        sky.SetFloat("_Exposure",            skyExposure);

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();
    }
}
