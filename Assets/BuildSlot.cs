using UnityEngine;
using UnityEngine.InputSystem;

public class BuildSlot : MonoBehaviour
{
    private bool     isOccupied    = false;
    private Color    originalColor;
    private Renderer _rend;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend != null)
            originalColor = _rend.sharedMaterial != null
                ? _rend.sharedMaterial.GetColor("_BaseColor")
                : Color.white;
    }

    void Update()
    {
        // Con FreeBuildManager activo los slots quedan inactivos
        if (FreeBuildManager.Instance != null) return;

        bool pressed  = false;
        Vector2 pressPos = Vector2.zero;

        // Mouse (PC)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed  = true;
            pressPos = Mouse.current.position.ReadValue();
        }

        // Touch (Mobile): primer dedo que comienza
        if (!pressed && Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    pressed  = true;
                    pressPos = touch.position.ReadValue();
                    break;
                }
            }
        }

        if (pressed && Camera.main != null && !IsOverUI(pressPos))
        {
            Ray ray = Camera.main.ScreenPointToRay(pressPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.transform == this.transform)
                OnClicked();
        }
    }

    void OnClicked()
    {
        if (isOccupied) return;
        BuildManager.Instance.BuildOnSlot(this);
    }

    public void SetOccupied()
    {
        isOccupied = true;
        SetColor(Color.red);
    }

    public bool IsOccupied() => isOccupied;

    public void SetEmpty()
    {
        isOccupied = false;
        SetColor(originalColor);
    }

    static bool IsOverUI(Vector2 screenPos)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPos };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(ped, results);
        return results.Count > 0;
    }

    void SetColor(Color c)
    {
        if (_rend == null) return;
        _rend.material.SetColor("_BaseColor", c);
        _rend.material.color = c;
    }
}
