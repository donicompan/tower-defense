using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Genera una ShaderVariantCollection a partir de todos los materiales del proyecto.
/// No requiere Play Mode: escanea los assets en disco.
///
/// Menú: Tower Defense / Android / Generar ShaderVariantCollection
/// </summary>
public static class ShaderVariantCollector
{
    const string OutputPath = "Assets/ShaderVariants_Mobile.shadervariants";

    [MenuItem("Tower Defense/Android/0 - Generar ShaderVariantCollection (materiales)")]
    public static void CollectFromAllMaterials()
    {
        var guids = AssetDatabase.FindAssets("t:Material");
        var svc   = new ShaderVariantCollection();
        int added = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            var shader = mat.shader;

            // Saltar shaders Built-in (Standard, Legacy, etc.) — no renderizan en URP
            // y agregarlos al SVC incluiría variantes inútiles para Android.
            string sName = shader.name;
            if (!sName.StartsWith("Universal Render Pipeline") &&
                !sName.StartsWith("TextMeshPro") &&
                sName != "Sprites/Default" &&
                !sName.StartsWith("Hidden/") &&
                sName != "Skybox/Procedural" &&
                sName != "Skybox/6 Sided")
                continue;

            var enabledKeywords = new List<string>(mat.shaderKeywords);

            var passTypes = new[]
            {
                PassType.Normal,
                PassType.ShadowCaster,
                PassType.Meta,
                PassType.ScriptableRenderPipeline,
                PassType.ScriptableRenderPipelineDefaultUnlit,
            };

            foreach (var passType in passTypes)
            {
                try
                {
                    svc.Add(new ShaderVariantCollection.ShaderVariant(
                        shader, passType, enabledKeywords.ToArray()));
                    added++;
                }
                catch { }
            }

            // Variante sin keywords como fallback
            try
            {
                svc.Add(new ShaderVariantCollection.ShaderVariant(
                    shader, PassType.ScriptableRenderPipeline));
                added++;
            }
            catch { }
        }

        // Shaders URP explícitos (usados en GameMaterials, VFX y Terrain en runtime)
        AddURPShader(svc, "Universal Render Pipeline/Lit",             ref added);
        AddURPShader(svc, "Universal Render Pipeline/Unlit",           ref added);
        AddURPShader(svc, "Universal Render Pipeline/Particles/Unlit", ref added);
        AddURPShader(svc, "Universal Render Pipeline/Particles/Lit",   ref added);
        // Terrain — CRÍTICO: sin estas variantes el terreno es rosa en Android
        AddURPShader(svc, "Universal Render Pipeline/Terrain/Lit",     ref added);
        AddURPShader(svc, "Hidden/Universal Render Pipeline/Terrain/Lit", ref added);

        AssetDatabase.DeleteAsset(OutputPath);
        AssetDatabase.CreateAsset(svc, OutputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SVC] ✓ ShaderVariantCollection generada en {OutputPath}\n" +
                  $"  Materiales escaneados : {guids.Length}\n" +
                  $"  Variantes registradas : {svc.variantCount}\n\n" +
                  $"SIGUIENTE PASO:\n" +
                  $"  1. Ejecutá '0b - Registrar SVC en Preloaded Shaders'\n" +
                  $"  2. Ejecutá '1 - Limpiar Always Included Shaders'\n" +
                  $"  3. Ejecutá 'Build Android con LZ4HC'");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    static void AddURPShader(ShaderVariantCollection svc, string shaderName, ref int count)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return;
        foreach (var pt in new[] {
            PassType.Normal, PassType.ShadowCaster,
            PassType.ScriptableRenderPipeline,
            PassType.ScriptableRenderPipelineDefaultUnlit })
        {
            try { svc.Add(new ShaderVariantCollection.ShaderVariant(shader, pt)); count++; }
            catch { }
        }
    }

    [MenuItem("Tower Defense/Android/0b - Registrar SVC en Preloaded Shaders")]
    public static void RegisterSVCInPreloadedShaders()
    {
        var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath);
        if (svc == null)
        {
            Debug.LogError($"[SVC] No se encontró {OutputPath}. Ejecutá primero el paso 0.");
            return;
        }

        var gfx  = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset"));
        var list = gfx.FindProperty("m_PreloadedShaders");
        if (list == null) { Debug.LogError("[SVC] m_PreloadedShaders no encontrado."); return; }

        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == svc)
            { Debug.Log("[SVC] La SVC ya está registrada en Preloaded Shaders."); return; }

        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = svc;
        gfx.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[SVC] ✓ {OutputPath} registrada en Project Settings > Graphics > Preloaded Shaders.");
    }
}
