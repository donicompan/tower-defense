using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [System.Serializable]
    public struct TimeOfDaySettings
    {
        public Color lightColor;
        [Range(0f, 2f)] public float lightIntensity;
        public Color ambientColor;
    }

    public Light directionalLight;
    public float stateDuration = 120f;

    [Header("Estados")]
    public TimeOfDaySettings morning = new TimeOfDaySettings
    {
        lightColor    = new Color(1.00f, 0.78f, 0.44f),
        lightIntensity = 0.85f,
        ambientColor  = new Color(0.45f, 0.42f, 0.38f)
    };
    public TimeOfDaySettings noon = new TimeOfDaySettings
    {
        lightColor    = new Color(1.00f, 0.96f, 0.84f),
        lightIntensity = 1.30f,
        ambientColor  = new Color(0.55f, 0.55f, 0.50f)
    };
    public TimeOfDaySettings afternoon = new TimeOfDaySettings
    {
        lightColor    = new Color(1.00f, 0.48f, 0.18f),
        lightIntensity = 0.70f,
        ambientColor  = new Color(0.40f, 0.28f, 0.22f)
    };
    public TimeOfDaySettings night = new TimeOfDaySettings
    {
        lightColor    = new Color(0.14f, 0.18f, 0.45f),
        lightIntensity = 0.08f,
        ambientColor  = new Color(0.06f, 0.07f, 0.16f)
    };

    private TimeOfDaySettings[] states;
    private float cycleTime;

    void Start()
    {
        states = new[] { morning, noon, afternoon, night };

        if (directionalLight == null)
            directionalLight = FindFirstObjectByType<Light>();
    }

    void Update()
    {
        cycleTime += Time.deltaTime;
        float totalDuration = stateDuration * states.Length;
        float t = (cycleTime % totalDuration) / stateDuration;

        int from = Mathf.FloorToInt(t) % states.Length;
        int to   = (from + 1) % states.Length;
        float blend = t - Mathf.Floor(t);
        // Suavizar la transición con Smoothstep
        blend = Mathf.SmoothStep(0f, 1f, blend);

        directionalLight.color     = Color.Lerp(states[from].lightColor,    states[to].lightColor,    blend);
        directionalLight.intensity = Mathf.Lerp(states[from].lightIntensity, states[to].lightIntensity, blend);
        RenderSettings.ambientLight = Color.Lerp(states[from].ambientColor,  states[to].ambientColor,  blend);
    }
}
