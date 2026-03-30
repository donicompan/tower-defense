using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [Header("Animación")]
    public float duration  = 1.0f;
    public float riseSpeed = 1.8f;
    public float spread    = 0.4f;

    private TextMeshPro tmp;
    private Color baseColor;
    private Vector3 moveDir;
    private Transform cam;
    private float elapsed;

    // Llamado por Enemy al instanciar — configura TMP y arranca la animación
    public void Init(int damage, Color? color = null)
    {
        // Crear TMP 3D en código: Transform normal, sin RectTransform
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text      = damage.ToString();
        tmp.fontSize  = 8f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = color ?? new Color(1f, 0f, 0f); // rojo por defecto
        tmp.alignment = TextAlignmentOptions.Center;

        baseColor = tmp.color;
        cam = Camera.main?.transform;

        float offsetX = Random.Range(-spread, spread);
        transform.position += new Vector3(offsetX, 0f, 0f);
        moveDir = new Vector3(offsetX * 0.3f, riseSpeed, 0f);
    }

    void Update()
    {
        if (tmp == null) return;

        elapsed += Time.deltaTime;

        // Subir y desacelerar
        transform.position += moveDir * Time.deltaTime;
        moveDir = Vector3.Lerp(moveDir, Vector3.zero, Time.deltaTime * 3f);

        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        // Billboard hacia la cámara (re-cachear si fue destruida y recreada)
        if (cam == null) cam = Camera.main?.transform;
        if (cam != null)
        {
            Vector3 dir = cam.position - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
