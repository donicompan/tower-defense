using UnityEditor;
using UnityEngine;

/// <summary>
/// Agrega automáticamente los shaders de URP necesarios en runtime a
/// "Always Included Shaders" para que estén disponibles en builds donde
/// no haya ningún material que los referencie explícitamente.
///
/// Se ejecuta al cargar el Editor y también está disponible como menú manual.
/// </summary>
// [InitializeOnLoad] removido: agregaba URP/Lit a Always Included en cada
// recarga del Editor, causando 1.769.472 variantes y bloqueando el build Android.
// Las variantes ahora vienen de ShaderVariants_Mobile.shadervariants (Preloaded Shaders).
public static class ShaderIncludeSetup
{
    static readonly string[] RequiredShaders =
    {
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Particles/Unlit",
    };

    // Menú mantenido por si se necesita restaurar manualmente (ej: builds PC sin SVC).
    [MenuItem("Tower Defense/Agregar shaders a Always Included (solo PC sin SVC)")]
    public static void EnsureShadersIncluded()
    {
        SerializedObject gfx = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset"));

        SerializedProperty list = gfx.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) return;

        bool changed = false;

        foreach (string shaderName in RequiredShaders)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[ShaderIncludeSetup] No se encontró: {shaderName}");
                continue;
            }

            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                { found = true; break; }
            }

            if (!found)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
                Debug.Log($"[ShaderIncludeSetup] Agregado a Always Included: {shaderName}");
            }
        }

        if (changed)
        {
            gfx.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
