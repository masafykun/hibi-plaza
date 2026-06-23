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
            var residents = UnityEngine.Object.FindObjectsByType<PlazaNpc>(FindObjectsSortMode.None);
            var animatedResidents = 0;
            foreach (var resident in residents)
            {
                if (resident.GetComponentInChildren<Animator>(true) != null)
                {
                    animatedResidents++;
                }
            }
            var camera = Camera.main;
            var hud = UnityEngine.Object.FindFirstObjectByType<PlazaHud>();
            var fountain = GameObject.Find("Central Fountain");
            var cafeShop = GameObject.Find("CAFE Shop");
            var kenneyBench = GameObject.Find("Kenney Bench");
            var avatarParts = game?.LocalAvatar == null ? 0 : game.LocalAvatar.GetComponentsInChildren<Renderer>(true).Length;
            var fountainParts = fountain == null ? 0 : fountain.GetComponentsInChildren<Renderer>(true).Length;
            if (game == null || game.LocalAvatar == null || avatars.Length < 1 || residents.Length < PlazaNpc.ResidentCount || animatedResidents < PlazaNpc.ResidentCount || camera == null || hud == null || fountain == null)
            {
                FailSmoke($"Missing runtime objects: game={game != null}, local={game?.LocalAvatar != null}, avatars={avatars.Length}, residents={residents.Length}, animatedResidents={animatedResidents}, camera={camera != null}, hud={hud != null}, fountain={fountain != null}");
                return;
            }
            if (avatarParts < 20 || fountainParts < 10 || cafeShop == null || kenneyBench == null)
            {
                FailSmoke($"Detailed assets were not loaded: avatarParts={avatarParts}, fountainParts={fountainParts}, cafeShop={cafeShop != null}, kenneyBench={kenneyBench != null}");
                return;
            }

            Debug.Log($"HIBI_PLAZA_SMOKE_OK avatars={avatars.Length} residents={residents.Length} animatedResidents={animatedResidents} avatarParts={avatarParts} fountainParts={fountainParts} local={game.LocalAvatar.DisplayName} camera={camera.name}");
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
            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/HibiPlaza/Resources" });
            foreach (var guid in modelGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }
                var isKenneyCharacter = path.Contains("Kenney/MiniCharacters", StringComparison.Ordinal);
                if (importer.bakeAxisConversion
                    && importer.importAnimation == isKenneyCharacter
                    && !importer.importCameras
                    && !importer.importLights)
                {
                    continue;
                }
                importer.bakeAxisConversion = true;
                importer.importAnimation = isKenneyCharacter;
                if (isKenneyCharacter)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                }
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
            }
        }

        public static void InspectThirdPartyAssets()
        {
            var resources = new[]
            {
                "ThirdParty/Kenney/MiniCharacters/character-female-a",
                "ThirdParty/Kenney/Furniture/bench",
                "ThirdParty/Kenney/Furniture/chairRounded",
                "ThirdParty/Kenney/Furniture/tableRound"
            };
            foreach (var resource in resources)
            {
                var prefab = Resources.Load<GameObject>(resource);
                if (prefab == null)
                {
                    Debug.LogError("HIBI_ASSET_INSPECT missing=" + resource);
                    continue;
                }
                var instance = UnityEngine.Object.Instantiate(prefab);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                var clips = Resources.LoadAll<AnimationClip>(resource);
                Debug.Log($"HIBI_ASSET_INSPECT resource={resource} renderers={renderers.Length} size={bounds.size} clips={clips.Length}");
                if (clips.Length > 0)
                {
                    var clipNames = new string[clips.Length];
                    for (var i = 0; i < clips.Length; i++)
                    {
                        clipNames[i] = clips[i].name;
                    }
                    Debug.Log("HIBI_ASSET_CLIPS " + string.Join(",", clipNames));
                }
                UnityEngine.Object.DestroyImmediate(instance);
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
