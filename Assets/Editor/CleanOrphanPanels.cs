using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Elimina automáticamente GameObjects huérfanos de la escena al cargar.
/// Borra: GameOverPanel, PausePanel (reemplazados por GameOverUI y PauseMenu).
/// </summary>
[InitializeOnLoad]
public static class CleanOrphanPanels
{
    static readonly string[] Orphans = { "GameOverPanel", "PausePanel" };

    static CleanOrphanPanels()
    {
        EditorSceneManager.sceneOpened += (scene, _) => Clean(scene);
        // También limpiar la escena actual al recargar el dominio
        EditorApplication.delayCall += () => Clean(SceneManager.GetActiveScene());
    }

    static void Clean(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        bool dirty = false;
        foreach (string name in Orphans)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) continue;
            Object.DestroyImmediate(go);
            Debug.Log($"[CleanOrphanPanels] Eliminado '{name}' de la escena '{scene.name}'.");
            dirty = true;
        }

        if (dirty)
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
