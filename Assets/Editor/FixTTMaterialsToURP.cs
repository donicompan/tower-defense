using UnityEngine;
using UnityEditor;

/// <summary>
/// Convierte materiales con shaders Built-in al shader URP Lit.
/// Los shaders Built-in (Standard, Legacy/Diffuse, etc.) renderizan magenta/rosa en builds Android URP.
/// </summary>
public static class FixTTMaterialsToURP
{
    [MenuItem("Tools/Fix TT Materials to URP")]
    static void Run()
    {
        ConvertFolderToURP(new[] { "Assets/ToonyTinyPeople" }, "Fix TT Materials to URP");
    }

    /// <summary>
    /// Convierte TODOS los materiales del proyecto que usan shaders Built-in a URP/Lit.
    /// Ejecutar esto antes de cada build Android para eliminar el problema del color rosa.
    /// Menú: Tower Defense/Android/Fix All Built-in Materials to URP
    /// </summary>
    [MenuItem("Tower Defense/Android/Fix All Built-in Materials to URP")]
    public static void FixAllBuiltinMaterials()
    {
        ConvertFolderToURP(new[] { "Assets" }, "Fix All Built-in Materials to URP");
    }

    static void ConvertFolderToURP(string[] folders, string dialogTitle)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[FixMaterials] No se encontró 'Universal Render Pipeline/Lit'. Verificá que URP esté instalado.");
            return;
        }

        string[] guids    = AssetDatabase.FindAssets("t:Material", folders);
        int converted     = 0;
        int alreadyURP    = 0;
        int skipped       = 0;

        foreach (string guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string shaderName = mat.shader.name;

            // Saltar materiales que ya usan URP o shaders que NUNCA deben convertirse
            if (shaderName.StartsWith("Universal Render Pipeline") ||
                shaderName.StartsWith("TextMeshPro") ||
                shaderName.StartsWith("Hidden/") ||
                shaderName.StartsWith("Skybox/") ||
                shaderName.StartsWith("GUI/") ||
                shaderName.StartsWith("UI/") ||
                shaderName.StartsWith("Sprites/") ||
                shaderName.StartsWith("Nature/") ||
                shaderName.StartsWith("Unlit/") ||
                shaderName == "Particles/Alpha Blended Premultiply" ||
                shaderName == "Particles/Additive" ||
                shaderName == "Particles/VertexLit Blended")
            {
                alreadyURP++;
                continue;
            }

            // Solo convertir shaders explícitamente identificados como Built-in
            // (NO usar hasBuiltinGuid — demasiado agresivo, convierte UI/Skybox/etc.)
            bool isBuiltin = shaderName == "Standard" ||
                             shaderName == "Standard (Specular setup)" ||
                             shaderName.StartsWith("Legacy Shaders/") ||
                             shaderName.StartsWith("Mobile/") ||
                             shaderName == "Diffuse" ||
                             shaderName == "Bumped Diffuse" ||
                             shaderName == "Specular" ||
                             shaderName == "Bumped Specular" ||
                             shaderName == "Particles/Standard Unlit" ||
                             shaderName == "Particles/Standard Surface";

            if (!isBuiltin)
            {
                skipped++;
                continue;
            }

            // Preservar textura principal antes de cambiar shader
            Texture mainTex  = mat.HasProperty("_MainTex")  ? mat.GetTexture("_MainTex")  : null;
            Texture bumpMap  = mat.HasProperty("_BumpMap")  ? mat.GetTexture("_BumpMap")  : null;
            Color   baseColor = mat.HasProperty("_Color")   ? mat.GetColor("_Color")       : Color.white;

            mat.shader = urpLit;

            // URP/Lit usa _BaseMap y _BaseColor en lugar de _MainTex y _Color
            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
                mat.SetTexture("_MainTex", mainTex);
            }
            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetColor("_BaseColor", baseColor);

            EditorUtility.SetDirty(mat);
            Debug.Log($"[FixMaterials] Convertido '{shaderName}' → URP/Lit: {path}");
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Convertidos: {converted}\nYa eran URP/especiales: {alreadyURP}\nOtros (no Built-in): {skipped}";
        Debug.Log($"[FixMaterials] {msg}");
        EditorUtility.DisplayDialog(dialogTitle, msg, "OK");
    }
}
