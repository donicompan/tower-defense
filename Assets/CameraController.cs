using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CameraController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed        = 18f;
    public float edgeScrollSpeed  = 14f;
    public float edgeThreshold    = 20f;   // píxeles desde el borde para activar scroll
    public float smoothTime       = 0.12f; // inercia del movimiento

    [Header("Rotación")]
    public float rotateSpeed      = 140f;

    [Header("Zoom")]
    public float zoomSpeed        = 6f;
    public float zoomSmooth       = 8f;
    public float minY             = 5f;
    public float maxY             = 55f;

    [Header("Touch")]
    [Tooltip("Sensibilidad del arrastre táctil para mover la cámara")]
    public float touchPanSensitivity  = 0.025f;
    [Tooltip("Sensibilidad del pellizco (pinch) para hacer zoom")]
    public float touchZoomSensitivity = 0.04f;

    [Header("Límites del mapa")]
    public float minX             = -52f;
    public float maxX             =  52f;
    public float minZ             = -52f;
    public float maxZ             =  52f;
    [Tooltip("Por cada unidad de altura de cámara, los límites XZ se reducen en este factor.")]
    [Range(0f, 0.8f)]
    public float heightClampFactor = 0.38f;

    // ── Estado interno ────────────────────────────────────────────────────────

    public static CameraController Instance { get; private set; }

    private Vector3 _velocity     = Vector3.zero;
    private float   _targetY;
    private Vector2 _lastMousePos;
    private bool    _rotating     = false;

    // Touch state
    private Vector2 _lastTouchPos;
    private float   _lastPinchDist;
    private bool    _wasPinching;

    // Screen shake
    private float _shakeMagnitude = 0f;
    private float _shakeDuration  = 0f;
    private float _shakeTimeLeft  = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _targetY = transform.position.y;
    }

    public void Shake(float magnitude, float duration)
    {
        _shakeMagnitude = magnitude;
        _shakeDuration  = duration;
        _shakeTimeLeft  = duration;
    }

    void Update()
    {
        HandleMove();
        HandleRotation();
        HandleZoom();
        ClampPosition();
        HandleShake();
    }

    // ── Movimiento WASD + borde de pantalla + touch drag ─────────────────────

    void HandleMove()
    {
        Vector3 dir = Vector3.zero;

        // WASD / flechas (PC)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                dir += GetForwardFlat();
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                dir -= GetForwardFlat();
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                dir -= GetRightFlat();
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                dir += GetRightFlat();
        }

        // Borde de pantalla (solo PC con mouse)
        if (!_rotating && Mouse.current != null)
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            float sw = Screen.width;
            float sh = Screen.height;

            if (mouse.x >= 0 && mouse.x <= sw && mouse.y >= 0 && mouse.y <= sh)
            {
                float speed = edgeScrollSpeed;
                if (mouse.x < edgeThreshold)          dir -= GetRightFlat()   * speed / moveSpeed;
                if (mouse.x > sw - edgeThreshold)     dir += GetRightFlat()   * speed / moveSpeed;
                if (mouse.y < edgeThreshold)           dir -= GetForwardFlat() * speed / moveSpeed;
                if (mouse.y > sh - edgeThreshold)     dir += GetForwardFlat() * speed / moveSpeed;
            }
        }

        // Touch: arrastre con UN dedo para mover
        if (Touchscreen.current != null)
        {
            int activeTouches = CountActiveTouches();
            if (activeTouches == 1)
            {
                var touch = GetFirstActiveTouch();
                if (touch != null && !IsOverUI(touch.position.ReadValue()))
                {
                    var phase = touch.phase.ReadValue();
                    Vector2 delta = touch.delta.ReadValue();

                    if (phase == UnityEngine.InputSystem.TouchPhase.Moved && delta.sqrMagnitude > 0.5f)
                    {
                        dir += GetRightFlat()   * (-delta.x * touchPanSensitivity);
                        dir += GetForwardFlat() * (-delta.y * touchPanSensitivity);
                    }
                }
            }
        }

        if (dir == Vector3.zero)
        {
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.deltaTime / smoothTime);
        }
        else
        {
            Vector3 target = dir.normalized * moveSpeed;
            _velocity = Vector3.Lerp(_velocity, target, Time.deltaTime / smoothTime);
        }

        Vector3 newPos = transform.position + _velocity * Time.deltaTime;
        newPos.y = transform.position.y;
        transform.position = newPos;
    }

    // ── Rotación: botón derecho (PC) ─────────────────────────────────────────

    void HandleRotation()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _rotating     = true;
            _lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
            _rotating = false;

        if (_rotating)
        {
            Vector2 mouseDelta = (Vector2)Mouse.current.position.ReadValue() - _lastMousePos;
            _lastMousePos = Mouse.current.position.ReadValue();

            float yaw = mouseDelta.x * rotateSpeed * Time.deltaTime * 0.3f;
            transform.Rotate(Vector3.up, yaw, Space.World);
        }
    }

    // ── Zoom: scroll (PC) + pinch (touch) ───────────────────────────────────

    void HandleZoom()
    {
        // Mouse scroll (PC)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _targetY -= scroll * zoomSpeed * 0.01f;
        }

        // Pinch zoom (Touch)
        if (Touchscreen.current != null)
        {
            int activeTouches = CountActiveTouches();
            if (activeTouches >= 2)
            {
                TouchControl t0 = Touchscreen.current.touches[0];
                TouchControl t1 = Touchscreen.current.touches[1];

                Vector2 pos0 = t0.position.ReadValue();
                Vector2 pos1 = t1.position.ReadValue();
                float currDist = Vector2.Distance(pos0, pos1);

                if (_wasPinching)
                {
                    float delta = currDist - _lastPinchDist;
                    _targetY -= delta * touchZoomSensitivity;
                }

                _lastPinchDist = currDist;
                _wasPinching   = true;
            }
            else
            {
                _wasPinching = false;
            }
        }

        _targetY = Mathf.Clamp(_targetY, minY, maxY);

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * zoomSmooth);
        transform.position = pos;
    }

    // ── Límites del mapa ─────────────────────────────────────────────────────

    void ClampPosition()
    {
        Vector3 p = transform.position;

        float margin = Mathf.Clamp(p.y * heightClampFactor, 0f, (maxX - minX) * 0.42f);
        float xMin = minX + margin,  xMax = maxX - margin;
        float zMin = minZ + margin,  zMax = maxZ - margin;

        if (p.x <= xMin || p.x >= xMax) _velocity.x = 0f;
        if (p.z <= zMin || p.z >= zMax) _velocity.z = 0f;

        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.z = Mathf.Clamp(p.z, zMin, zMax);
        transform.position = p;
    }

    // ── Screen shake ─────────────────────────────────────────────────────────

    void HandleShake()
    {
        if (_shakeTimeLeft <= 0f) return;
        _shakeTimeLeft -= Time.deltaTime;
        float t   = Mathf.Clamp01(_shakeTimeLeft / _shakeDuration);
        float mag = _shakeMagnitude * t;
        Vector3 p = transform.position;
        p.x += Random.Range(-mag, mag);
        p.z += Random.Range(-mag, mag);
        transform.position = p;
    }

    // ── UI overlap check ─────────────────────────────────────────────────────

    static bool IsOverUI(Vector2 screenPos)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPos };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(ped, results);
        return results.Count > 0;
    }

    // ── Helpers de touch ─────────────────────────────────────────────────────

    int CountActiveTouches()
    {
        if (Touchscreen.current == null) return 0;
        int count = 0;
        foreach (var t in Touchscreen.current.touches)
        {
            var phase = t.phase.ReadValue();
            if (phase != UnityEngine.InputSystem.TouchPhase.None &&
                phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                count++;
        }
        return count;
    }

    TouchControl GetFirstActiveTouch()
    {
        if (Touchscreen.current == null) return null;
        foreach (var t in Touchscreen.current.touches)
        {
            var phase = t.phase.ReadValue();
            if (phase != UnityEngine.InputSystem.TouchPhase.None &&
                phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                return t;
        }
        return null;
    }

    // ── Helpers de dirección ─────────────────────────────────────────────────

    Vector3 GetForwardFlat()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    Vector3 GetRightFlat()
    {
        Vector3 r = transform.right;
        r.y = 0f;
        return r.normalized;
    }
}
