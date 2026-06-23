using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HibiPlaza.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/HibiPlaza/Scenes/Main.unity";
        private const string TitleArtPath = "Assets/HibiPlaza/Resources/TitleArt.png";
        private const string SmokeKey = "HibiPlaza.SmokeTest";
        private static int smokeFrames;

        static ProjectSetup()
        {
            if (EditorPrefs.GetBool(SmokeKey, false))
            {
                EditorApplication.update -= SmokeUpdate;
                EditorApplication.update += SmokeUpdate;
            }
        }

        [MenuItem("Hibi Plaza/Configure Project")]
        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/HibiPlaza/Scenes");
            ConfigureTitleArt();
            ConfigureModels();
            ConfigurePlayer();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Hibi Plaza project configured.");
        }

        [MenuItem("Hibi Plaza/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureProject();
            var output = Path.GetFullPath("Build/WebGL");
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}, {report.summary.totalErrors} errors");
            }
            Debug.Log($"Hibi Plaza WebGL build completed: {output} ({report.summary.totalSize} bytes)");
        }

        public static void RunSmokeTest()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            smokeFrames = 0;
            EditorPrefs.SetBool(SmokeKey, true);
            EditorApplication.update += SmokeUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void SmokeUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }
            smokeFrames++;
            if (smokeFrames == 8)
            {
                if (HibiGame.Instance == null)
                {
                    FailSmoke("HibiGame was not created.");
                    return;
                }
                HibiGame.Instance.EnterPlaza(false);
            }
            if (smokeFrames < 28)
            {
                return;
            }

            var game = HibiGame.Instance;
            var avatars = UnityEngine.Object.FindObjectsByType<AvatarVisual>(FindObjectsSortMode.None);
            var camera = Camera.main;
            var hud = UnityEngine.Object.FindFirstObjectByType<PlazaHud>();
            var fountain = GameObject.Find("Central Fountain");
            var cafeShop = GameObject.Find("CAFE Shop");
            var avatarParts = game?.LocalAvatar == null ? 0 : game.LocalAvatar.GetComponentsInChildren<Renderer>(true).Length;
            var fountainParts = fountain == null ? 0 : fountain.GetComponentsInChildren<Renderer>(true).Length;
            if (game == null || game.LocalAvatar == null || avatars.Length < 5 || camera == null || hud == null || fountain == null)
            {
                FailSmoke($"Missing runtime objects: game={game != null}, local={game?.LocalAvatar != null}, avatars={avatars.Length}, camera={camera != null}, hud={hud != null}, fountain={fountain != null}");
                return;
            }
            if (avatarParts < 20 || fountainParts < 10 || cafeShop == null)
            {
                FailSmoke($"Detailed assets were not loaded: avatarParts={avatarParts}, fountainParts={fountainParts}, cafeShop={cafeShop != null}");
                return;
            }

            Debug.Log($"HIBI_PLAZA_SMOKE_OK avatars={avatars.Length} avatarParts={avatarParts} fountainParts={fountainParts} local={game.LocalAvatar.DisplayName} camera={camera.name}");
            EditorPrefs.DeleteKey(SmokeKey);
            EditorApplication.update -= SmokeUpdate;
            EditorApplication.Exit(0);
        }

        private static void FailSmoke(string message)
        {
            Debug.LogError("HIBI_PLAZA_SMOKE_FAILED " + message);
            EditorPrefs.DeleteKey(SmokeKey);
            EditorApplication.update -= SmokeUpdate;
            EditorApplication.Exit(1);
        }

        private static void ConfigureTitleArt()
        {
            AssetDatabase.ImportAsset(TitleArtPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(TitleArtPath) is not TextureImporter importer)
            {
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ConfigureModels()
        {
            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/HibiPlaza/Resources/Models" });
            foreach (var guid in modelGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }
                if (importer.bakeAxisConversion && !importer.importAnimation && !importer.importCameras && !importer.importLights)
                {
                    continue;
                }
                importer.bakeAxisConversion = true;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Masafykun Games";
            PlayerSettings.productName = "Hibi Plaza";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:HibiPlaza";
        }

        private static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
