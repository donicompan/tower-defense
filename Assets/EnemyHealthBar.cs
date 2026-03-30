using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    private Canvas    canvas;
    private Image     fill;
    private int       maxHealth;
    private Transform cam;

    private const float CANVAS_W = 200f;
    private const float CANVAS_H = 26f;   // ligeramente más alta para mejor visibilidad
    private const float WORLD_W  = 2.0f;

    // Paleta de salud
    static readonly Color C_HpFull  = new Color(0.18f, 0.92f, 0.35f);  // verde brillante
    static readonly Color C_HpMid   = new Color(1.00f, 0.85f, 0.10f);  // amarillo dorado
    static readonly Color C_HpLow   = new Color(1.00f, 0.22f, 0.18f);  // rojo vibrante
    static readonly Color C_BgNorm  = new Color(0.04f, 0.04f, 0.06f, 0.92f);
    static readonly Color C_BgBoss  = new Color(0.18f, 0.00f, 0.28f, 1f);
    static readonly Color C_Border  = new Color(0.10f, 0.12f, 0.20f, 1f);
    static readonly Color C_BossBorder = new Color(1f, 0.82f, 0f, 1f);

    public void Init(int maxHp, bool isBoss = false, float enemyScale = 1f)
    {
        maxHealth = maxHp;
        cam       = Camera.main?.transform;

        canvas             = gameObject.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        float worldW = isBoss ? WORLD_W * 1.5f : WORLD_W;
        float height  = isBoss ? CANVAS_H * 1.6f : CANVAS_H;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CANVAS_W, height);

        float sc = enemyScale > 0f ? enemyScale : 1f;
        float s  = worldW / CANVAS_W / sc;
        transform.localScale = new Vector3(s, s, s);

        // Borde exterior (visible en todos los enemigos, dorado en boss)
        Color borderColor = isBoss ? C_BossBorder : C_Border;
        GameObject borderGO = MakeImage("Border", borderColor);
        RectTransform brt   = borderGO.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-3f, -3f);
        brt.offsetMax = new Vector2( 3f,  3f);
        borderGO.transform.SetAsFirstSibling();

        // Fondo
        Color bgColor = isBoss ? C_BgBoss : C_BgNorm;
        MakeImage("BG", bgColor);

        // Relleno con tipo Filled
        GameObject fillGO = MakeImage("Fill", C_HpFull);
        fill            = fillGO.GetComponent<Image>();
        fill.type       = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        // Brillo en el borde superior del relleno (hace que se vea volumétrico)
        GameObject shimmer = MakeImage("FillShimmer", new Color(1f, 1f, 1f, 0.18f));
        RectTransform shRT = shimmer.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0f, 0.55f);
        shRT.anchorMax = Vector2.one;
        shRT.offsetMin = Vector2.zero;
        shRT.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
    }

    public void Refresh(int currentHp)
    {
        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);

        float ratio = Mathf.Clamp01((float)currentHp / maxHealth);
        fill.fillAmount = ratio;

        // Gradiente tricolor: rojo → amarillo → verde
        if (ratio > 0.5f)
            fill.color = Color.Lerp(C_HpMid, C_HpFull, (ratio - 0.5f) * 2f);
        else
            fill.color = Color.Lerp(C_HpLow, C_HpMid, ratio * 2f);
    }

    void LateUpdate()
    {
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(cam.position - transform.position);
    }

    GameObject MakeImage(string goName, Color color)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UIHelper.Img(go, color);
        return go;
    }
}
