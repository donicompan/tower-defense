using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Elimina variantes de shader que definitivamente NO se usan en este juego mobile.
/// Objetivo: reducir de ~200K variantes a menos de 5K sin causar color rosa.
///
/// Regla: strip si la variante tiene CUALQUIERA de las keywords del blacklist activa.
/// Las keywords del whitelist se respetan aunque no estén en ningún material.
/// </summary>
public class MobileShaderStripper : IPreprocessShaders
{
    // ── Keywords que NUNCA se necesitan en este tower defense mobile ──────────
    static readonly string[] _blacklist =
    {
        // DOTS / ECS — no usamos Entity Component System
        "DOTS_INSTANCING_ON",

        // Lightmaps — solo lightmap estático básico (LIGHTMAP_ON se preserva)
        "DYNAMICLIGHTMAP_ON",
        "DIRLIGHTMAP_COMBINED",
        "LIGHTMAP_SHADOW_MIXING",
        "SHADOWS_SHADOWMASK",

        // Mixed lighting — no usamos Mixed
        "_MIXED_LIGHTING_SUBTRACTIVE",

        // Sombras de luces adicionales — muy caro en mobile, solo sombras de main light
        "_ADDITIONAL_LIGHT_SHADOWS",

        // SSAO — desactivado en Mobile_RPAsset
        "_SCREEN_SPACE_OCCLUSION",
        "SSAO",

        // Reflection probes avanzados
        "_REFLECTION_PROBE_BLENDING",
        "_REFLECTION_PROBE_BOX_PROJECTION",

        // Detail maps — ningún material usa detail maps
        "_DETAIL_MULX2",
        "_DETAIL_SCALED",

        // Fog — no hay niebla en la escena del desierto
        "FOG_LINEAR",
        "FOG_EXP",
        "FOG_EXP2",

        // Decals / DBuffer
        "_DBUFFER_MRT1",
        "_DBUFFER_MRT2",
        "_DBUFFER_MRT3",
        "_DECAL_LAYERS",
        "_DECAL_NORMAL_BLEND_LOW",
        "_DECAL_NORMAL_BLEND_MEDIUM",
        "_DECAL_NORMAL_BLEND_HIGH",

        // Light layers / Rendering layers — no usados
        "_LIGHT_LAYERS",
        "_WRITE_RENDERING_LAYERS",
        "_LIGHT_COOKIES",

        // Debug — solo build de release
        "DEBUG_DISPLAY",
        "EDITOR_VISUALIZATION",

        // XR / Foveated — no es VR/AR
        "_FOVEATED_RENDERING_NON_UNIFORM_RASTER",
        "STEREO_INSTANCING_ON",
        "STEREO_MULTIVIEW_ON",
        "UNITY_SINGLE_PASS_STEREO",

        // GBuffer / Deferred — usamos Forward, no Deferred
        "_GBUFFER_NORMALS_OCT",
        "_DEFERRED_MIXED_LIGHTING",
        "USE_LEGACY_LIGHTMAPS",

        // Probe Volumes (APV) — no usados
        "PROBE_VOLUMES_L1",
        "PROBE_VOLUMES_L2",

        // Forward+ / Clustered — Mobile usa Forward estándar
        "_FORWARD_PLUS",
        "_CLUSTERED_RENDERING",

        // Soft particles — desactivado en QualitySettings mobile
        "_SOFTPARTICLES_ON",

        // Sombras de cascada > 1 — Mobile_RPAsset usa 1 cascade
        "_MAIN_LIGHT_SHADOWS_CASCADE",
        "_MAIN_LIGHT_SHADOWS_SCREEN",

        // HDR output
        "_HDR_GRADING",
        "HDR_INPUT",

        // Native Render Pass variants extra
        "_RENDER_PASS_ENABLED",

        // Parallax — ningún material lo usa en mobile
        "_PARALLAXMAP",
    };

    static HashSet<string> _blacklistSet;
    static HashSet<string> BlacklistSet
    {
        get
        {
            if (_blacklistSet == null)
            {
                _blacklistSet = new HashSet<string>(_blacklist);
            }
            return _blacklistSet;
        }
    }

    // IPreprocessShaders — se llama por cada variante durante el build
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        // Solo aplicar al build Android
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != UnityEditor.BuildTarget.Android)
            return;

        int before = data.Count;

        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (ShouldStrip(data[i].shaderKeywordSet))
                data.RemoveAt(i);
        }

        int stripped = before - data.Count;
        if (stripped > 0 && before > 10)
        {
            // Log solo shaders con alto volumen para no saturar la consola
            if (before > 500)
                Debug.Log($"[Stripper] {shader.name} | {snippet.passName}: {before} → {data.Count} variantes (-{stripped})");
        }
    }

    static bool ShouldStrip(ShaderKeywordSet keywordSet)
    {
        foreach (string kw in BlacklistSet)
        {
            if (keywordSet.IsEnabled(new ShaderKeyword(kw)))
                return true;
        }
        return false;
    }
}
