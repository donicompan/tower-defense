using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Auto-runs in SampleScene to:
/// 1. Fix legacy scene-baked UI buttons
/// 2. Configure CanvasScaler for correct multi-resolution scaling
/// 3. Lock screen to landscape on mobile
/// Does NOT require manual scene attachment — uses RuntimeInitializeOnLoadMethod.
/// </summary>
public static class UISceneStartup
{
    static readonly Color C_DarkBg = new Color(0.05f, 0.06f, 0.10f, 0.92f);
    static readonly Color C_Gold   = new Color(1.00f, 0.85f, 0.15f, 1f);
    static readonly Color C_Border = new Color(0.20f, 0.22f, 0.35f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        string scene = SceneManager.GetActiveScene().name;

        // Lock landscape on all platforms (UI designed for landscape only)
        Screen.autorotateToPortrait            = false;
        Screen.autorotateToPortraitUpsideDown  = false;
        Screen.autorotateToLandscapeLeft       = true;
        Screen.autorotateToLandscapeRight      = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        if (scene == "SampleScene")
            FixSampleScene();
        else if (scene == "MainMenu")
            SetupCanvasScaler();
    }

    static void FixSampleScene()
    {
        SetupCanvasScaler();
        FixLegacyButtons();
        HideStrayObjects();
    }

    // ── CanvasScaler: UI scales uniformly on all screen sizes ────────────────

    static void SetupCanvasScaler()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) cs = canvas.gameObject.AddComponent<CanvasScaler>();

        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;   // blend width+height → good for both phone/tablet
    }

    // ── Style the hardcoded tower buttons ────────────────────────────────────

    static void FixLegacyButtons()
    {
        var allButtons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);

        foreach (var btn in allButtons)
        {
            string n = btn.gameObject.name;
            if (n != "Button" && n != "Button ") continue;

            Transform parent = btn.transform.parent;
            if (parent == null || parent.GetComponent<Canvas>() == null) continue;

            StyleLegacyButton(btn.gameObject);
        }
    }

    static void StyleLegacyButton(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = UIHelper.Bg;
            img.type   = Image.Type.Simple;
            img.color  = C_DarkBg;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(162f, 44f);

            GameObject border = new GameObject("_Border");
            border.transform.SetParent(go.transform, false);
            border.transform.SetAsFirstSibling();
            RectTransform brt = border.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-2f, -2f);
            brt.offsetMax = new Vector2( 2f,  2f);
            UIHelper.Img(border, C_Border);
        }

        foreach (TextMeshProUGUI tmp in go.GetComponentsInChildren<TextMeshProUGUI>())
        {
            tmp.color            = C_Gold;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            tmp.fontSize         = Mathf.Min(tmp.fontSize, 15f);
        }
    }

    // ── Remove objects that should not be visible at game start ──────────────

    static void HideStrayObjects()
    {
        foreach (string name in new[] { "QuitButton2", "RestartButton " })
        {
            GameObject go = GameObject.Find(name);
            if (go != null) go.SetActive(false);
        }
    }
}
