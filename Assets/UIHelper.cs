using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared helper for UI creation.
/// In URP Unity 6, Image components with null sprite render as magenta.
/// WhiteSprite is generated at runtime — never depends on external assets.
/// </summary>
public static class UIHelper
{
    static Sprite _whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] colors = new Color[16];
                for (int i = 0; i < 16; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                _whiteSprite.name = "GeneratedWhiteSprite";
            }
            return _whiteSprite;
        }
    }

    /// <summary>
    /// Add an Image component with a generated white sprite so it renders
    /// as a solid-color rectangle in URP (never magenta).
    /// </summary>
    public static Image Img(GameObject go, Color color)
    {
        Image img      = go.AddComponent<Image>();
        img.sprite     = WhiteSprite;
        img.type       = Image.Type.Simple;
        img.color      = color;
        img.raycastTarget = true;
        return img;
    }
}
