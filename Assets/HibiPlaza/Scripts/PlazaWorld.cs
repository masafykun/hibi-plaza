using System.Collections.Generic;
using UnityEngine;

namespace HibiPlaza
{
    public static class PlazaWorld
    {
        public const int GroundLayer = 8;

        public static readonly Vector3[] SpawnPoints =
        {
            new Vector3(0f, 0f, -4.5f),
            new Vector3(-3f, 0f, 0f),
            new Vector3(3f, 0f, 0.5f),
            new Vector3(-5f, 0f, 4f),
            new Vector3(5f, 0f, 4f),
            new Vector3(0f, 0f, 8f)
        };

        public static SocialCamera Build(Transform parent)
        {
            ConfigureRendering();
            CreateLights(parent);
            CreateGround(parent);
            CreateFountain(parent);
            CreateShops(parent);
            CreateCafe(parent);
            CreateParkDetails(parent);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(SocialCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.8f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.42f, 0.76f, 0.94f);
            return cameraObject.GetComponent<SocialCamera>();
        }

        private static void ConfigureRendering()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 105f;
            RenderSettings.fogColor = new Color(0.63f, 0.82f, 0.91f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.82f, 0.93f);
            RenderSettings.ambientEquatorColor = new Color(0.54f, 0.58f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.26f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(0.38f, 0.72f, 0.96f));
                sky.SetColor("_GroundColor", new Color(0.73f, 0.78f, 0.72f));
                sky.SetFloat("_AtmosphereThickness", 0.82f);
                sky.SetFloat("_SunSize", 0.018f);
                sky.SetFloat("_Exposure", 1.10f);
                RenderSettings.skybox = sky;
            }
        }

        private static void CreateLights(Transform parent)
        {
            var sunObject = new GameObject("Warm Sun", typeof(Light));
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.84f, 0.68f);
            sun.intensity = 0.92f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.68f;
            RenderSettings.sun = sun;

            var fillObject = new GameObject("Sky Fill", typeof(Light));
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.44f, 0.68f, 0.92f);
            fill.intensity = 0.22f;
        }

        private static void CreateGround(Transform parent)
        {
            var ground = Part(PrimitiveType.Cube, "Plaza Ground", parent, new Vector3(0f, -0.25f, 0f), new Vector3(74f, 0.5f, 58f), new Color(0.34f, 0.54f, 0.36f));
            ground.layer = GroundLayer;

            Part(PrimitiveType.Cylinder, "Plaza Outer Ring", parent, new Vector3(0f, 0.01f, 5.5f), new Vector3(22.0f, 0.04f, 22.0f), new Color(0.48f, 0.36f, 0.29f), false);
            Part(PrimitiveType.Cylinder, "Plaza Center", parent, new Vector3(0f, 0.06f, 5.5f), new Vector3(20.4f, 0.04f, 20.4f), new Color(0.69f, 0.58f, 0.46f), false);
            Part(PrimitiveType.Cube, "Main Walk", parent, new Vector3(0f, 0.11f, -2f), new Vector3(13f, 0.08f, 54f), new Color(0.66f, 0.57f, 0.47f), false);
            Part(PrimitiveType.Cube, "Cross Walk", parent, new Vector3(0f, 0.12f, 3f), new Vector3(65f, 0.09f, 10f), new Color(0.68f, 0.58f, 0.47f), false);

            for (var x = -31; x <= 31; x += 4)
            {
                Part(PrimitiveType.Cube, "Paving Detail", parent, new Vector3(x, 0.18f, 3f), new Vector3(0.08f, 0.03f, 9.8f), new Color(0.43f, 0.34f, 0.27f), false);
            }
            for (var z = -26; z <= 26; z += 4)
            {
                Part(PrimitiveType.Cube, "Paving Detail", parent, new Vector3(0f, 0.17f, z), new Vector3(12.8f, 0.03f, 0.08f), new Color(0.43f, 0.34f, 0.27f), false);
            }
        }

        private static void CreateFountain(Transform parent)
        {
            var imported = Model("Fountain", "Central Fountain", parent, new Vector3(0f, 0f, 5.5f), null, Quaternion.identity, Vector3.one * 0.76f);
            if (imported != null)
            {
                CreateFountainParticles(imported.transform);
                return;
            }

            var root = new GameObject("Central Fountain").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0f, 5.5f);

            Part(PrimitiveType.Cylinder, "Fountain Base", root, new Vector3(0f, 0.28f, 0f), new Vector3(5.8f, 0.28f, 5.8f), new Color(0.76f, 0.78f, 0.76f));
            var water = Part(PrimitiveType.Cylinder, "Fountain Water", root, new Vector3(0f, 0.59f, 0f), new Vector3(5.1f, 0.08f, 5.1f), Color.white, false);
            water.GetComponent<Renderer>().sharedMaterial = HibiMaterials.Water;
            Part(PrimitiveType.Cylinder, "Fountain Stem", root, new Vector3(0f, 1.35f, 0f), new Vector3(0.75f, 1.55f, 0.75f), new Color(0.82f, 0.82f, 0.78f), false);
            Part(PrimitiveType.Cylinder, "Fountain Bowl", root, new Vector3(0f, 2.18f, 0f), new Vector3(2.9f, 0.18f, 2.9f), new Color(0.78f, 0.79f, 0.75f), false);
            Part(PrimitiveType.Cylinder, "Fountain Crown", root, new Vector3(0f, 2.90f, 0f), new Vector3(0.32f, 1.25f, 0.32f), new Color(0.84f, 0.74f, 0.50f), false);
            CreateFountainParticles(root);
        }

        private static void CreateFountainParticles(Transform parent)
        {
            var particlesObject = new GameObject("Water Jets", typeof(ParticleSystem));
            particlesObject.transform.SetParent(parent, false);
            particlesObject.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            var particles = particlesObject.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 3.1f;
            main.startSize = 0.10f;
            main.startColor = new Color(0.76f, 0.94f, 1f, 0.78f);
            main.gravityModifier = 0.78f;
            var emission = particles.emission;
            emission.rateOverTime = 34f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 19f;
            shape.radius = 0.12f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = HibiMaterials.Particle;
        }

        private static void CreateShops(Transform parent)
        {
            CreateShop(parent, new Vector3(-18.5f, 0f, 20.5f), new Color(0.94f, 0.50f, 0.52f), new Color(1f, 0.82f, 0.58f), "STYLE");
            CreateShop(parent, new Vector3(-6.3f, 0f, 21f), new Color(0.37f, 0.72f, 0.68f), new Color(0.90f, 0.96f, 0.79f), "CAFE");
            CreateShop(parent, new Vector3(6.3f, 0f, 21f), new Color(0.52f, 0.59f, 0.88f), new Color(0.91f, 0.84f, 1f), "ROOM");
            CreateShop(parent, new Vector3(18.5f, 0f, 20.5f), new Color(0.96f, 0.68f, 0.30f), new Color(1f, 0.91f, 0.66f), "GOODS");
        }

        private static void CreateShop(Transform parent, Vector3 position, Color wall, Color awning, string sign)
        {
            var imported = Model("Shop", sign + " Shop", parent, position, wall, Quaternion.identity, Vector3.one * 0.74f);
            if (imported != null)
            {
                var importedSign = WorldLabel(parent, sign, position + new Vector3(0f, 5.45f, -2.42f), new Vector2(220f, 48f), Color.white);
                importedSign.transform.localRotation = Quaternion.identity;
                return;
            }

            var root = new GameObject(sign + " Shop").transform;
            root.SetParent(parent, false);
            root.localPosition = position;

            Part(PrimitiveType.Cube, "Shop Building", root, new Vector3(0f, 4.2f, 0f), new Vector3(11f, 8.4f, 6f), wall);
            Part(PrimitiveType.Cube, "Shop Roof", root, new Vector3(0f, 8.65f, 0f), new Vector3(11.8f, 0.55f, 6.8f), new Color(0.30f, 0.22f, 0.23f), false);
            Part(PrimitiveType.Cube, "Shop Window L", root, new Vector3(-2.75f, 3.7f, -3.03f), new Vector3(3.5f, 3.5f, 0.12f), new Color(0.35f, 0.72f, 0.82f), false, true);
            Part(PrimitiveType.Cube, "Shop Window R", root, new Vector3(2.75f, 3.7f, -3.03f), new Vector3(3.5f, 3.5f, 0.12f), new Color(0.35f, 0.72f, 0.82f), false, true);
            Part(PrimitiveType.Cube, "Awning", root, new Vector3(0f, 5.9f, -3.55f), new Vector3(10.3f, 0.28f, 2.1f), awning, false);
            for (var x = -4; x <= 4; x += 2)
            {
                Part(PrimitiveType.Cube, "Awning Stripe", root, new Vector3(x, 5.92f, -3.60f), new Vector3(0.82f, 0.30f, 2.12f), Color.white, false);
            }

            var signCanvas = WorldLabel(root, sign, new Vector3(0f, 7.0f, -3.30f), new Vector2(220f, 48f), new Color(0.12f, 0.16f, 0.20f));
            signCanvas.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private static void CreateCafe(Transform parent)
        {
            for (var i = 0; i < 3; i++)
            {
                var x = 20f + i * 5f;
                var imported = Model("CafeSet", "Cafe Set " + i, parent, new Vector3(x, 0f, -9f), null, Quaternion.Euler(0f, i * 12f, 0f), Vector3.one * 0.74f);
                if (imported != null)
                {
                    continue;
                }
                var table = Part(PrimitiveType.Cylinder, "Cafe Table", parent, new Vector3(x, 0.9f, -9f), new Vector3(2.3f, 0.12f, 2.3f), new Color(0.45f, 0.23f, 0.10f), false);
                Part(PrimitiveType.Cylinder, "Table Leg", parent, new Vector3(x, 0.43f, -9f), new Vector3(0.25f, 0.85f, 0.25f), new Color(0.18f, 0.20f, 0.22f), false);
                Part(PrimitiveType.Cylinder, "Umbrella Pole", parent, new Vector3(x, 2.2f, -9f), new Vector3(0.12f, 4.4f, 0.12f), new Color(0.22f, 0.22f, 0.22f), false);
                var umbrella = Part(PrimitiveType.Cylinder, "Cafe Umbrella", parent, new Vector3(x, 4.25f, -9f), new Vector3(5.1f, 0.12f, 5.1f),
                    i % 2 == 0 ? new Color(0.96f, 0.39f, 0.29f) : new Color(0.98f, 0.72f, 0.23f), false);
                umbrella.transform.localRotation = Quaternion.Euler(0f, i * 12f, 0f);
            }
        }

        private static void CreateParkDetails(Transform parent)
        {
            var treePositions = new[]
            {
                new Vector3(-30f, 0f, -19f), new Vector3(-30f, 0f, 15f),
                new Vector3(30f, 0f, -18f), new Vector3(30f, 0f, 14f),
                new Vector3(-16f, 0f, -22f), new Vector3(16f, 0f, -22f)
            };
            foreach (var position in treePositions)
            {
                if (Model("Tree", "Tree", parent, position) == null)
                {
                    CreateTree(parent, position);
                }
            }

            for (var x = -24; x <= 24; x += 8)
            {
                var position = new Vector3(x, 0f, -17f);
                if (Model("Lamp", "Lamp", parent, position) == null)
                {
                    CreateLamp(parent, position);
                }
            }

            CreateBenchModel(parent, new Vector3(-9f, 0f, 11.5f), 25f);
            CreateBenchModel(parent, new Vector3(9f, 0f, 11.5f), -25f);
            CreateBenchModel(parent, new Vector3(-15f, 0f, -7f), 90f);
            CreateBenchModel(parent, new Vector3(15f, 0f, -7f), -90f);

            for (var i = 0; i < 8; i++)
            {
                var angle = i / 8f * Mathf.PI * 2f;
                var position = new Vector3(Mathf.Cos(angle) * 7.4f, 0f, 5.5f + Mathf.Sin(angle) * 7.4f);
                if (Model("Planter", "Flower Planter", parent, position, null, Quaternion.Euler(0f, i * 28f, 0f), Vector3.one * 0.72f) != null)
                {
                    continue;
                }
                var planter = Part(PrimitiveType.Cylinder, "Flower Planter", parent, position + Vector3.up * 0.35f, new Vector3(1.35f, 0.45f, 1.35f), new Color(0.42f, 0.24f, 0.16f), false);
                for (var flower = 0; flower < 5; flower++)
                {
                    var offset = new Vector3(Random.Range(-0.38f, 0.38f), 0.72f, Random.Range(-0.38f, 0.38f));
                    Part(PrimitiveType.Sphere, "Flower", planter.transform, offset, Vector3.one * 0.22f,
                        flower % 2 == 0 ? new Color(0.98f, 0.40f, 0.58f) : new Color(1f, 0.78f, 0.24f), false);
                }
            }
        }

        private static void CreateTree(Transform parent, Vector3 position)
        {
            Part(PrimitiveType.Cylinder, "Tree Trunk", parent, position + Vector3.up * 2.1f, new Vector3(0.72f, 4.2f, 0.72f), new Color(0.32f, 0.16f, 0.07f), false);
            Part(PrimitiveType.Sphere, "Tree Crown", parent, position + Vector3.up * 5.4f, new Vector3(4.8f, 4.2f, 4.8f), new Color(0.24f, 0.62f, 0.30f), false);
            Part(PrimitiveType.Sphere, "Tree Crown", parent, position + new Vector3(-1.7f, 5.0f, 0.5f), new Vector3(3.2f, 3.0f, 3.2f), new Color(0.34f, 0.72f, 0.32f), false);
        }

        private static void CreateLamp(Transform parent, Vector3 position)
        {
            Part(PrimitiveType.Cylinder, "Lamp Post", parent, position + Vector3.up * 2.0f, new Vector3(0.20f, 4.0f, 0.20f), new Color(0.10f, 0.13f, 0.15f), false);
            Part(PrimitiveType.Sphere, "Lamp Glow", parent, position + Vector3.up * 4.2f, Vector3.one * 0.58f, new Color(1f, 0.70f, 0.24f), false, true);
        }

        private static void CreateBench(Transform parent, Vector3 position, float rotation)
        {
            var root = new GameObject("Bench").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, rotation, 0f);
            Part(PrimitiveType.Cube, "Bench Seat", root, new Vector3(0f, 0.65f, 0f), new Vector3(3.3f, 0.25f, 0.85f), new Color(0.46f, 0.23f, 0.09f), false);
            Part(PrimitiveType.Cube, "Bench Back", root, new Vector3(0f, 1.25f, 0.38f), new Vector3(3.3f, 0.85f, 0.18f), new Color(0.46f, 0.23f, 0.09f), false);
            Part(PrimitiveType.Cube, "Bench Legs", root, new Vector3(-1.15f, 0.30f, 0f), new Vector3(0.18f, 0.65f, 0.70f), new Color(0.12f, 0.14f, 0.16f), false);
            Part(PrimitiveType.Cube, "Bench Legs", root, new Vector3(1.15f, 0.30f, 0f), new Vector3(0.18f, 0.65f, 0.70f), new Color(0.12f, 0.14f, 0.16f), false);
        }

        private static void CreateBenchModel(Transform parent, Vector3 position, float rotation)
        {
            if (Model("Bench", "Bench", parent, position, null, Quaternion.Euler(0f, rotation, 0f), Vector3.one * 0.82f) == null)
            {
                CreateBench(parent, position, rotation);
            }
        }

        private static GameObject Model(string resource, string name, Transform parent, Vector3 position, Color? tint = null,
            Quaternion rotation = default, Vector3 scale = default)
        {
            var prefab = Resources.Load<GameObject>("Models/" + resource);
            if (prefab == null)
            {
                return null;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = name;
            instance.transform.localPosition = position;
            var placementRotation = rotation == default ? Quaternion.identity : rotation;
            instance.transform.localRotation = placementRotation * Quaternion.Euler(90f, 0f, 0f);
            instance.transform.localScale = scale == default ? Vector3.one : scale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Water", System.StringComparison.Ordinal))
                {
                    renderer.sharedMaterial = HibiMaterials.Water;
                }
                else if (renderer.name.Contains("Glow", System.StringComparison.Ordinal))
                {
                    renderer.sharedMaterial = HibiMaterials.Get(new Color(1f, 0.67f, 0.18f), true);
                }
                else if (tint.HasValue && renderer.name.StartsWith("ShopTint", System.StringComparison.Ordinal))
                {
                    renderer.sharedMaterial = HibiMaterials.Get(tint.Value);
                }
                else
                {
                    var source = renderer.sharedMaterial;
                    var color = source != null && source.HasProperty("_Color") ? source.color : Color.white;
                    renderer.sharedMaterial = HibiMaterials.Get(color);
                }
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return instance;
        }

        private static RectTransform WorldLabel(Transform parent, string value, Vector3 position, Vector2 size, Color color)
        {
            var canvasObject = new GameObject("World Label", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            canvasObject.transform.localScale = Vector3.one * 0.012f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var text = UiKit.Text(rect, value, 25, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Stretch(text.rectTransform);
            return rect;
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            bool collider = true, bool emission = false)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = HibiMaterials.Get(color, emission);
            var partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                partCollider.enabled = collider;
            }
            return part;
        }
    }

    public sealed class SocialCamera : MonoBehaviour
    {
        private Transform target;
        private Vector3 velocity;

        public void Follow(Transform value)
        {
            target = value;
            Snap();
        }

        private void Snap()
        {
            if (target == null)
            {
                return;
            }
            transform.position = target.position + new Vector3(0f, 12f, -16f);
            transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            var desired = target.position + new Vector3(0f, 12f, -16f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.22f);
            transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        }
    }

    public static class HibiMaterials
    {
        private static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>();
        private static Shader stylized;
        private static Material water;
        private static Material particle;

        public static Material Water => water ??= new Material(Resources.Load<Shader>("Shaders/Water"))
        {
            name = "Fountain Water"
        };

        public static Material Particle => particle ??= new Material(Resources.Load<Shader>("Shaders/Particle"))
        {
            name = "Soft Particle"
        };

        public static Material Get(Color color, bool emission = false)
        {
            var color32 = (Color32)color;
            var key = color32.r | color32.g << 8 | color32.b << 16 | (emission ? 1 << 24 : 0);
            if (Cache.TryGetValue(key, out var material))
            {
                return material;
            }

            stylized ??= Resources.Load<Shader>("Shaders/StylizedLit");
            material = new Material(stylized)
            {
                name = "Color " + ColorUtility.ToHtmlStringRGB(color),
                color = color
            };
            material.SetFloat("_Glossiness", emission ? 0.78f : 0.38f);
            material.SetFloat("_Metallic", emission ? 0.12f : 0.02f);
            if (emission)
            {
                material.SetColor("_EmissionColor", color * 0.55f);
            }
            Cache[key] = material;
            return material;
        }
    }
}
