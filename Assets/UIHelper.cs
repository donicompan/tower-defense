using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared helper for UI creation.
/// In URP Unity 6, Image components with null sprite render as magenta.
/// Always call Img() instead of AddComponent<Image>() directly.
///
/// WhiteSquare.png is in Assets/Resources/ — imported as Sprite type.
/// Resources.Load<Sprite> works in both Editor and Android builds.
/// </summary>
public static class UIHelper
{
    static Sprite _bg;

    public static Sprite Bg
    {
        get
        {
            if (_bg == null)
                _bg = LoadSprite();
            return _bg;
        }
    }

    static Sprite LoadSprite()
    {
        // Primary: load the pre-made WhiteSquare.png from Resources
        // (Assets/Resources/WhiteSquare.png, imported as Sprite type)
        Sprite s = Resources.Load<Sprite>("WhiteSquare");
        if (s != null) return s;

        // Fallback A: Unity built-in background sprite
        s = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        if (s != null) return s;

        // Fallback B: generate a 4×4 white texture at runtime
        // Works on Android without any asset loading
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var white = new Color32(255, 255, 255, 255);
        var pixels = new Color32[16];
        for (int i = 0; i < 16; i++) pixels[i] = white;
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Add an Image component with a valid white sprite so it renders
    /// as a solid-color rectangle in URP (never magenta).
    /// </summary>
    public static Image Img(GameObject go, Color color)
    {
        Image img  = go.AddComponent<Image>();
        img.sprite = Bg;
        img.type   = Image.Type.Simple;
        img.color  = color;
        return img;
    }
}
