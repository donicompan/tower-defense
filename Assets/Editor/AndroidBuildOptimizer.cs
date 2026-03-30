using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Build completo para Android — maneja shaders, cache, Player Settings y build.
/// Menú: Tower Defense / Android / ...
/// </summary>
public static class AndroidBuildOptimizer
{
    static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(Application.dataPath, ".."));

    // ════════════════════════════════════════════════════════════════════════
    // BUILD COMPLETO — único punto de entrada recomendado
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tower Defense/Android/BUILD COMPLETO (todo automático)")]
    public static void FullBuild()
    {
        Debug.Log("[Build] ══ INICIO BUILD ANDROID ══");

        FixTTMaterialsToURP.FixAllBuiltinMaterials(); // convierte materiales Built-in → URP/Lit (evita pink)
        DisableShaderStripping();                     // CRÍTICO: evita que URP stripee shaders en runtime
        ShaderVariantCollector.CollectFromAllMaterials(); // regenera SVC con todos los shaders URP
        ShaderVariantCollector.RegisterSVCInPreloadedShaders();
        CleanAlwaysIncludedShaders();
        CleanBeeBuildCache();       // borra Library/Bee/Android/ si existe
        ConfigureAndroidPlayerSettings();
        OptimizeMobileURPAsset();

        string buildsDir = Path.Combine(ProjectRoot, "Builds");
        Directory.CreateDirectory(buildsDir);
        string apkPath = Path.Combine(buildsDir, "TowerDefense.apk");

        var opts = new BuildPlayerOptions
        {
            scenes           = GetEditorScenes(),
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.CompressWithLz4HC,
        };

        Debug.Log($"[Build] Output: {apkPath}");
        var report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[Build] ✓ APK generado: {apkPath}  ({report.summary.totalSize / 1024 / 1024} MB)");
        else
            Debug.LogError($"[Build] ✗ Falló. Revisá errores arriba.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 0-EXTRA. SHADER STRIPPING — deshabilitar strip agresivo de URP
    // m_StripUnusedVariants=1 es la causa #1 de color rosa en Android:
    // elimina variantes creadas en runtime que URP no "ve" en análisis estático.
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tower Defense/Android/0-Extra - Deshabilitar Shader Stripping (anti-pink)")]
    public static void DisableShaderStripping()
    {
        const string path = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset == null) { Debug.LogError($"[Strip] No encontrado: {path}"); return; }

        var so = new SerializedObject(asset);
        SetBool(so, "m_StripUnusedVariants",            false);
        SetBool(so, "m_StripUnusedPostProcessingVariants", false);

        // Buscar dentro de structs anidados también
        var stripping    = so.FindProperty("m_URPShaderStrippingSetting");
        var strippingBase = so.FindProperty("m_ShaderStrippingSetting");
        if (stripping != null)
        {
            var p = stripping.FindPropertyRelative("m_StripUnusedVariants");
            if (p != null) p.boolValue = false;
            var p2 = stripping.FindPropertyRelative("m_StripUnusedPostProcessingVariants");
            if (p2 != null) p2.boolValue = false;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        Debug.Log("[Strip] ✓ Shader stripping deshabilitado — variantes de runtime no serán eliminadas.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1. ALWAYS INCLUDED SHADERS — solo terrain shaders (no URP/Lit global)
    // Los terrain shaders DEBEN estar en Always Included porque Unity los
    // stripea aunque estén en la ShaderVariantCollection.
    // ════════════════════════════════════════════════════════════════════════

    // Shaders de terrain que SIEMPRE deben incluirse en el build Android
    static readonly string[] TerrainShaders =
    {
        "Universal Render Pipeline/Terrain/Lit",
    };

    [MenuItem("Tower Defense/Android/1 - Configurar Always Included Shaders (Terrain)")]
    public static void CleanAlwaysIncludedShaders()
    {
        var gfxAsset = AssetDatabase.LoadAssetAtPath<Object>(
            "ProjectSettings/GraphicsSettings.asset");
        if (gfxAsset == null)
        {
            Debug.LogError("[AIS] No se encontró ProjectSettings/GraphicsSettings.asset");
            return;
        }

        var gfx  = new SerializedObject(gfxAsset);
        var list = gfx.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) { Debug.LogError("[AIS] Propiedad no encontrada."); return; }

        // Limpiar entradas null o no-terrain
        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            var s = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (s == null)
            {
                list.DeleteArrayElementAtIndex(i);
                continue;
            }
            bool isTerrainShader = false;
            foreach (var t in TerrainShaders)
                if (s.name == t) { isTerrainShader = true; break; }
            if (!isTerrainShader)
                list.DeleteArrayElementAtIndex(i);
        }

        // Agregar terrain shaders que falten
        foreach (var shaderName in TerrainShaders)
        {
            Shader sh = Shader.Find(shaderName);
            if (sh == null) { Debug.LogWarning($"[AIS] No encontrado: {shaderName}"); continue; }

            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh)
                { found = true; break; }

            if (!found)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
                Debug.Log($"[AIS] Agregado: {shaderName}");
            }
        }

        gfx.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[AIS] ✓ Always Included configurado con {list.arraySize} terrain shader(s).");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. BEE CACHE — borrar Library/Bee/Android/ (paths de otro usuario)
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tower Defense/Android/2 - Limpiar caché Bee/Android")]
    public static void CleanBeeBuildCache()
    {
        string beePath = Path.Combine(ProjectRoot, "Library", "Bee", "Android");
        if (!Directory.Exists(beePath))
        {
            Debug.Log("[Bee] Library/Bee/Android/ ya estaba limpio.");
            return;
        }
        try
        {
            Directory.Delete(beePath, recursive: true);
            Debug.Log("[Bee] ✓ Library/Bee/Android/ eliminado. " +
                      "Gradle regenerará los paths correctos en el próximo build.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Bee] No se pudo borrar (Unity puede estar usándolo): {e.Message}\n" +
                             "Cerrá Unity, borrá manualmente Library/Bee/Android/ y volvé a buildear.");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. PLAYER SETTINGS ANDROID
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tower Defense/Android/3 - Configurar Player Settings Android")]
    public static void ConfigureAndroidPlayerSettings()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Master);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
        PlayerSettings.graphicsJobs = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        // Usar Activity clásica en lugar de GameActivity para eliminar la dependencia
        // games-activity AAR que requiere Prefab CLI — cuyo JAR está en un path con
        // caracteres especiales (Ú) que CMD.EXE no puede leer (codepage OEM cp850).
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

        Debug.Log("[Settings] ✓ IL2CPP · ARM64 · Master · Stripping:High · minSDK:24 · Activity");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. URP MOBILE ASSET — desactivar features no usadas
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Tower Defense/Android/4 - Optimizar URP Mobile Asset")]
    public static void OptimizeMobileURPAsset()
    {
        const string path = "Assets/Settings/Mobile_RPAsset.asset";
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset == null) { Debug.LogError($"[URP] No encontrado: {path}"); return; }

        var so = new SerializedObject(asset);
        // Decals
        SetBool(so, "m_PrefilterDBufferMRT1", false);
        SetBool(so, "m_PrefilterDBufferMRT2", false);
        SetBool(so, "m_PrefilterDBufferMRT3", false);
        // SSAO
        SetBool(so, "m_PrefilteringModeScreenSpaceOcclusion", false);
        SetBool(so, "m_PrefilterSSAODepthNormals",            false);
        SetBool(so, "m_PrefilterSSAOSourceDepthLow",          false);
        SetBool(so, "m_PrefilterSSAOSourceDepthMedium",       false);
        SetBool(so, "m_PrefilterSSAOSourceDepthHigh",         false);
        SetBool(so, "m_PrefilterSSAOInterleaved",             false);
        SetBool(so, "m_PrefilterSSAOBlueNoise",               false);
        SetBool(so, "m_PrefilterSSAOSampleCountLow",          false);
        SetBool(so, "m_PrefilterSSAOSampleCountMedium",       false);
        SetBool(so, "m_PrefilterSSAOSampleCountHigh",         false);
        // XR / HDR / Debug / Layers / Reflections / Shadows / Forward+
        SetBool(so, "m_PrefilterXRKeywords",                   false);
        SetBool(so, "m_PrefilterHDROutput",                    false);
        SetBool(so, "m_PrefilterAlphaOutput",                  false);
        SetBool(so, "m_PrefilterDebugKeywords",                false);
        SetBool(so, "m_PrefilterWriteRenderingLayers",         false);
        SetBool(so, "m_PrefilterReflectionProbeBlending",      false);
        SetBool(so, "m_PrefilterReflectionProbeBoxProjection", false);
        SetBool(so, "m_PrefilterSoftShadows",                  false);
        SetBool(so, "m_PrefilterSoftShadowsQualityLow",        false);
        SetBool(so, "m_PrefilterSoftShadowsQualityMedium",     false);
        SetBool(so, "m_PrefilterSoftShadowsQualityHigh",       false);
        SetBool(so, "m_PrefilterUseLegacyLightmaps",           false);
        SetBool(so, "m_PrefilteringModeForwardPlus",           false);
        SetBool(so, "m_PrefilteringModeDeferredRendering",     false);
        // Main light shadows: modo 1 = PCF básico (sin cascadas, sin screen-space)
        SetInt (so, "m_PrefilteringModeMainLightShadows",        1);
        // Additional lights — mobile solo usa vertex lights (mode 0), sin sombras adicionales
        SetBool(so, "m_PrefilteringModeAdditionalLight",        false);
        SetBool(so, "m_PrefilteringModeAdditionalLightShadows", false);
        // Native Render Pass y Screen Coord override — no usados en mobile estándar
        SetBool(so, "m_PrefilterNativeRenderPass",              false);
        SetBool(so, "m_PrefilterScreenCoord",                   false);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        Debug.Log("[URP] ✓ Mobile_RPAsset optimizado.");
    }

    static void SetBool(SerializedObject so, string name, bool value)
    {
        var p = so.FindProperty(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
        else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = value ? 1 : 0;
    }

    static void SetInt(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
        else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0;
    }

    static string[] GetEditorScenes()
    {
        var list = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) list.Add(s.path);
        return list.ToArray();
    }
}
