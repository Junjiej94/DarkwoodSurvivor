using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestSetup
{
    const string ScenePath = "Assets/ForestRoguelike/Forest.unity";

    [MenuItem("Forest Roguelike/Create or Open Game Scene")]
    public static void OpenGame()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            ForestPixelArt.ApplyToScene();
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var game = new GameObject("Forest Game").AddComponent<ForestGame>();
        ForestPixelArt.Apply(game);
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) return;
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
