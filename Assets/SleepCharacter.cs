using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class SleepCharacter : MonoBehaviour
{
    public int price = 150;
    public string characterName = "Sheriff";
    public GameObject zzzObject;
    public TextMeshPro priceText;
    public int goldPerWave = 8;
    public GameObject shadowPrefab;

    private bool isAsleep = true;
    private float zzzTimer = 0f;
    private Vector3 zzzStartPos;

    // Feedback visual al comprar
    private GameObject _coinIconGO;
    private TextMeshPro _incomeLabel;
    private float _pulseTimer = 0f;

    void Start()
    {
        if (zzzObject != null)
            zzzStartPos = zzzObject.transform.localPosition;

        if (priceText != null)
            priceText.text = "💰 " + price;

        if (shadowPrefab != null)
        {
            GameObject shadow = Instantiate(shadowPrefab, transform.position, shadowPrefab.transform.rotation);
            shadow.transform.SetParent(transform);
            int terrainMask = LayerMask.GetMask("Terrain");
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, terrainMask))
                shadow.transform.position = new Vector3(transform.position.x, hit.point.y + 0.02f, transform.position.z);
        }
    }

    void Update()
    {
        // Animar zzz flotando
        if (isAsleep && zzzObject != null)
        {
            zzzTimer += Time.deltaTime;
            float yOffset = Mathf.Sin(zzzTimer * 2f) * 0.3f;
            zzzObject.transform.localPosition = zzzStartPos + Vector3.up * yOffset;
        }

        // Billboard — el texto siempre mira a la cámara
        if (priceText != null && Camera.main != null)
            priceText.transform.LookAt(Camera.main.transform);

        // Animación del ícono de moneda y label de ingreso
        if (!isAsleep && Camera.main != null)
        {
            _pulseTimer += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(_pulseTimer * 3.5f) * 0.10f;

            if (_coinIconGO != null)
            {
                _coinIconGO.transform.localScale = Vector3.one * pulse;
                _coinIconGO.transform.LookAt(Camera.main.transform);
            }

            if (_incomeLabel != null)
                _incomeLabel.transform.LookAt(Camera.main.transform);
        }

        // Click / tap
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        Vector2 clickPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        if (!clicked && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    clicked  = true;
                    clickPos = touch.position.ReadValue();
                    break;
                }
            }
        }
        if (clicked && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(clickPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
                if (hit.transform.IsChildOf(this.transform) ||
                    hit.transform == this.transform)
                    TryBuy();
        }
    }

    void TryBuy()
    {
        if (!isAsleep) return;
        if (GameManager.Instance.SpendGold(price))
            WakeUp();
    }

    void WakeUp()
    {
        isAsleep = false;
        if (zzzObject != null) zzzObject.SetActive(false);
        if (priceText != null) priceText.gameObject.SetActive(false);
        transform.rotation = Quaternion.identity;
        InvokeRepeating("GiveGold", 10f, 10f);
        ShowPurchaseFeedback();
    }

    // ── Feedback visual al comprar ─────────────────────────────────────────────

    void ShowPurchaseFeedback()
    {
        // 1. Texto flotante "+X oro cada 10s" que sube y se desvanece
        StartCoroutine(FloatPurchaseText());

        // 2. Ícono de moneda persistente que pulsa
        _coinIconGO = new GameObject("CoinIcon");
        _coinIconGO.transform.SetParent(transform);
        _coinIconGO.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        TextMeshPro coinTmp  = _coinIconGO.AddComponent<TextMeshPro>();
        coinTmp.text         = "$";
        coinTmp.fontSize     = 9f;
        coinTmp.fontStyle    = FontStyles.Bold;
        coinTmp.alignment    = TextAlignmentOptions.Center;
        coinTmp.color        = new Color(1f, 0.85f, 0.08f);

        // 3. Label de ingreso persistente
        GameObject labelGO   = new GameObject("IncomeLabel");
        labelGO.transform.SetParent(transform);
        labelGO.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        _incomeLabel         = labelGO.AddComponent<TextMeshPro>();
        _incomeLabel.text    = "+" + goldPerWave + " oro cada 10s";
        _incomeLabel.fontSize = 3.2f;
        _incomeLabel.fontStyle = FontStyles.Bold;
        _incomeLabel.alignment = TextAlignmentOptions.Center;
        _incomeLabel.color   = new Color(0.25f, 1f, 0.35f);
    }

    IEnumerator FloatPurchaseText()
    {
        GameObject go    = new GameObject("FloatingPurchaseText");
        go.transform.position = transform.position + Vector3.up * 2.5f;
        TextMeshPro tmp  = go.AddComponent<TextMeshPro>();
        tmp.text         = "+" + goldPerWave + " oro cada 10s";
        tmp.fontSize     = 5.5f;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.alignment    = TextAlignmentOptions.Center;
        Color baseColor  = new Color(0.25f, 1f, 0.35f);
        tmp.color        = baseColor;

        Vector3 startPos = go.transform.position;
        float duration   = 2.8f;
        float elapsed    = 0f;
        Transform cam    = Camera.main?.transform;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            go.transform.position = startPos + Vector3.up * (elapsed * 0.55f);
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                                  Mathf.Lerp(1f, 0f, Mathf.Pow(t, 1.5f)));

            if (cam != null)
                go.transform.rotation = Quaternion.LookRotation(cam.position - go.transform.position);

            yield return null;
        }

        Destroy(go);
    }

    void GiveGold()
    {
        // Da oro solo cuando está DESPIERTO
        if (isAsleep) return;
        GameManager.Instance.AddGold(goldPerWave);
    }
}
