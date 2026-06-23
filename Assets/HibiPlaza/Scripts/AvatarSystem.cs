using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HibiPlaza
{
    [Serializable]
    public sealed class AvatarData
    {
        public string displayName = "Guest";
        public int skin;
        public int hair;
        public int top;
        public int bottom;
        public int hairStyle;

        public AvatarData Copy()
        {
            return new AvatarData
            {
                displayName = displayName,
                skin = skin,
                hair = hair,
                top = top,
                bottom = bottom,
                hairStyle = hairStyle
            };
        }
    }

    public static class AvatarPalette
    {
        public static readonly Color[] Skin =
        {
            new Color(1.00f, 0.82f, 0.68f),
            new Color(0.89f, 0.65f, 0.48f),
            new Color(0.70f, 0.43f, 0.28f),
            new Color(0.43f, 0.24f, 0.16f),
            new Color(1.00f, 0.73f, 0.61f)
        };

        public static readonly Color[] Hair =
        {
            new Color(0.10f, 0.055f, 0.035f),
            new Color(0.28f, 0.12f, 0.055f),
            new Color(0.90f, 0.67f, 0.24f),
            new Color(0.95f, 0.42f, 0.58f),
            new Color(0.30f, 0.70f, 0.72f),
            new Color(0.45f, 0.33f, 0.72f),
            new Color(0.86f, 0.88f, 0.90f)
        };

        public static readonly Color[] Tops =
        {
            new Color(0.18f, 0.48f, 0.90f),
            new Color(0.95f, 0.33f, 0.28f),
            new Color(0.96f, 0.63f, 0.18f),
            new Color(0.27f, 0.70f, 0.48f),
            new Color(0.65f, 0.40f, 0.84f),
            new Color(0.96f, 0.50f, 0.68f),
            new Color(0.12f, 0.17f, 0.22f)
        };

        public static readonly Color[] Bottoms =
        {
            new Color(0.12f, 0.20f, 0.31f),
            new Color(0.27f, 0.39f, 0.60f),
            new Color(0.45f, 0.25f, 0.18f),
            new Color(0.28f, 0.58f, 0.48f),
            new Color(0.58f, 0.36f, 0.62f),
            new Color(0.82f, 0.72f, 0.55f)
        };
    }

    public sealed class AvatarVisual : MonoBehaviour
    {
        private AvatarData data;
        private Transform leftArm;
        private Transform rightArm;
        private Transform bodyRoot;
        private Transform hairRoot;
        private Text nameText;
        private GameObject bubble;
        private Text bubbleText;
        private float walkCycle;
        private float emoteUntil;
        private string emote;
        private bool moving;
        private bool usesImportedModel;

        public string DisplayName => data?.displayName ?? "Guest";

        public static AvatarVisual Create(AvatarData avatar, Transform parent, Vector3 position)
        {
            var root = new GameObject("Avatar " + avatar.displayName, typeof(AvatarVisual), typeof(CapsuleCollider));
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var collider = root.GetComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.62f, 0f);
            collider.height = 3.18f;
            collider.radius = 0.64f;
            var visual = root.GetComponent<AvatarVisual>();
            visual.Build(avatar.Copy());
            return visual;
        }

        public void Apply(AvatarData avatar)
        {
            data = avatar.Copy();
            if (bodyRoot == null)
            {
                Build(data);
                return;
            }

            var skin = AvatarPalette.Skin[Mathf.Abs(data.skin) % AvatarPalette.Skin.Length];
            var hair = AvatarPalette.Hair[Mathf.Abs(data.hair) % AvatarPalette.Hair.Length];
            var top = AvatarPalette.Tops[Mathf.Abs(data.top) % AvatarPalette.Tops.Length];
            var bottom = AvatarPalette.Bottoms[Mathf.Abs(data.bottom) % AvatarPalette.Bottoms.Length];

            SetPartColor("Skin", skin);
            SetPartColor("Hair", hair);
            SetPartColor("Top", top);
            SetPartColor("Bottom", bottom);
            if (nameText != null)
            {
                nameText.text = data.displayName;
            }
            if (usesImportedModel)
            {
                ApplyHairStyle();
            }
            else
            {
                RebuildHair(hair);
            }
        }

        public void SetMoving(bool value)
        {
            moving = value;
        }

        public void PlayEmote(string value)
        {
            emote = value;
            emoteUntil = Time.time + 2.2f;
        }

        public void ShowChat(string message)
        {
            if (bubble == null)
            {
                return;
            }
            bubbleText.text = message;
            bubble.SetActive(true);
            CancelInvoke(nameof(HideChat));
            Invoke(nameof(HideChat), Mathf.Clamp(2.8f + message.Length * 0.035f, 3f, 6f));
        }

        public void SetLabelsVisible(bool visible)
        {
            if (nameText != null)
            {
                nameText.transform.parent.gameObject.SetActive(visible);
            }
            if (bubble != null)
            {
                bubble.SetActive(false);
            }
        }

        private void HideChat()
        {
            if (bubble != null)
            {
                bubble.SetActive(false);
            }
        }

        private void Build(AvatarData avatar)
        {
            data = avatar;
            var model = Resources.Load<GameObject>("Models/HibiAvatar");
            if (model != null)
            {
                bodyRoot = new GameObject("Body Motion").transform;
                bodyRoot.SetParent(transform, false);
                var instance = Instantiate(model, bodyRoot, false);
                instance.name = "Body Model";
                instance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                leftArm = FindDeepChild(instance.transform, "ArmPivot_L");
                rightArm = FindDeepChild(instance.transform, "ArmPivot_R");
                usesImportedModel = leftArm != null && rightArm != null;
                if (usesImportedModel)
                {
                    RestyleImportedModel(instance);
                    Apply(data);
                    CreateLabels();
                    return;
                }
                Destroy(instance);
                Destroy(bodyRoot.gameObject);
            }

            bodyRoot = new GameObject("Body").transform;
            bodyRoot.SetParent(transform, false);

            var skin = AvatarPalette.Skin[Mathf.Abs(data.skin) % AvatarPalette.Skin.Length];
            var top = AvatarPalette.Tops[Mathf.Abs(data.top) % AvatarPalette.Tops.Length];
            var bottom = AvatarPalette.Bottoms[Mathf.Abs(data.bottom) % AvatarPalette.Bottoms.Length];

            Part(PrimitiveType.Sphere, "Skin Head", bodyRoot, new Vector3(0f, 2.35f, 0f), new Vector3(0.96f, 1.02f, 0.90f), skin);
            Part(PrimitiveType.Cube, "Top Torso", bodyRoot, new Vector3(0f, 1.35f, 0f), new Vector3(0.88f, 0.92f, 0.54f), top, new Vector3(0f, 0f, 0f), 0.12f);

            leftArm = Limb("Skin Left Arm", new Vector3(-0.62f, 1.43f, 0f), skin);
            rightArm = Limb("Skin Right Arm", new Vector3(0.62f, 1.43f, 0f), skin);

            Part(PrimitiveType.Cube, "Top Sleeve L", leftArm, new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.45f, 0.36f), top, Vector3.zero, 0.10f);
            Part(PrimitiveType.Cube, "Top Sleeve R", rightArm, new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.45f, 0.36f), top, Vector3.zero, 0.10f);

            Part(PrimitiveType.Cube, "Bottom Left Leg", bodyRoot, new Vector3(-0.24f, 0.57f, 0f), new Vector3(0.40f, 0.72f, 0.44f), bottom, Vector3.zero, 0.10f);
            Part(PrimitiveType.Cube, "Bottom Right Leg", bodyRoot, new Vector3(0.24f, 0.57f, 0f), new Vector3(0.40f, 0.72f, 0.44f), bottom, Vector3.zero, 0.10f);
            Part(PrimitiveType.Cube, "Shoe L", bodyRoot, new Vector3(-0.24f, 0.16f, 0.11f), new Vector3(0.43f, 0.24f, 0.64f), new Color(0.94f, 0.95f, 0.96f), Vector3.zero, 0.11f);
            Part(PrimitiveType.Cube, "Shoe R", bodyRoot, new Vector3(0.24f, 0.16f, 0.11f), new Vector3(0.43f, 0.24f, 0.64f), new Color(0.94f, 0.95f, 0.96f), Vector3.zero, 0.11f);

            CreateFace();
            hairRoot = new GameObject("Hair Root").transform;
            hairRoot.SetParent(bodyRoot, false);
            RebuildHair(AvatarPalette.Hair[Mathf.Abs(data.hair) % AvatarPalette.Hair.Length]);
            CreateLabels();
        }

        private void ApplyHairStyle()
        {
            var activeStyle = Mathf.Abs(data.hairStyle) % 4;
            for (var i = 0; i < 4; i++)
            {
                var style = FindDeepChild(bodyRoot, "HairStyle_" + i);
                if (style != null)
                {
                    style.gameObject.SetActive(i == activeStyle);
                }
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent.name == childName)
            {
                return parent;
            }
            foreach (Transform child in parent)
            {
                var found = FindDeepChild(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static void RestyleImportedModel(GameObject model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterial;
                var color = source != null && source.HasProperty("_Color") ? source.color : Color.white;
                renderer.sharedMaterial = HibiMaterials.Get(color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private Transform Limb(string name, Vector3 position, Color color)
        {
            var pivot = new GameObject(name + " Pivot").transform;
            pivot.SetParent(bodyRoot, false);
            pivot.localPosition = position;
            Part(PrimitiveType.Capsule, name, pivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.30f, 0.52f, 0.30f), color);
            return pivot;
        }

        private void CreateFace()
        {
            var eyeColor = new Color(0.06f, 0.055f, 0.07f);
            Part(PrimitiveType.Sphere, "Eye L", bodyRoot, new Vector3(-0.21f, 2.42f, 0.42f), new Vector3(0.12f, 0.17f, 0.08f), eyeColor);
            Part(PrimitiveType.Sphere, "Eye R", bodyRoot, new Vector3(0.21f, 2.42f, 0.42f), new Vector3(0.12f, 0.17f, 0.08f), eyeColor);
            Part(PrimitiveType.Sphere, "Cheek L", bodyRoot, new Vector3(-0.34f, 2.25f, 0.42f), new Vector3(0.13f, 0.07f, 0.045f), new Color(1f, 0.44f, 0.48f));
            Part(PrimitiveType.Sphere, "Cheek R", bodyRoot, new Vector3(0.34f, 2.25f, 0.42f), new Vector3(0.13f, 0.07f, 0.045f), new Color(1f, 0.44f, 0.48f));
        }

        private void RebuildHair(Color color)
        {
            if (hairRoot == null)
            {
                return;
            }
            for (var i = hairRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(hairRoot.GetChild(i).gameObject);
            }

            var style = Mathf.Abs(data.hairStyle) % 4;
            Part(PrimitiveType.Sphere, "Hair Cap", hairRoot, new Vector3(0f, 2.64f, -0.03f), new Vector3(1.01f, 0.72f, 0.92f), color);
            if (style == 0)
            {
                Part(PrimitiveType.Sphere, "Hair Bob L", hairRoot, new Vector3(-0.42f, 2.36f, -0.03f), new Vector3(0.42f, 0.68f, 0.50f), color);
                Part(PrimitiveType.Sphere, "Hair Bob R", hairRoot, new Vector3(0.42f, 2.36f, -0.03f), new Vector3(0.42f, 0.68f, 0.50f), color);
            }
            else if (style == 1)
            {
                Part(PrimitiveType.Sphere, "Hair Bun L", hairRoot, new Vector3(-0.52f, 2.82f, -0.03f), Vector3.one * 0.46f, color);
                Part(PrimitiveType.Sphere, "Hair Bun R", hairRoot, new Vector3(0.52f, 2.82f, -0.03f), Vector3.one * 0.46f, color);
            }
            else if (style == 2)
            {
                for (var i = -2; i <= 2; i++)
                {
                    Part(PrimitiveType.Capsule, "Hair Spike", hairRoot, new Vector3(i * 0.20f, 2.92f - Mathf.Abs(i) * 0.04f, -0.04f),
                        new Vector3(0.20f, 0.38f, 0.22f), color, new Vector3(0f, 0f, i * -9f));
                }
            }
            else
            {
                Part(PrimitiveType.Sphere, "Hair Short", hairRoot, new Vector3(0f, 2.62f, -0.18f), new Vector3(0.96f, 0.62f, 0.78f), color);
            }
        }

        private void CreateLabels()
        {
            var labelHeight = usesImportedModel ? 3.78f : 3.35f;
            var nameCanvas = WorldCanvas("Nameplate", new Vector3(0f, labelHeight, 0f), new Vector2(180f, 34f));
            nameText = UiKit.Text(nameCanvas, data.displayName, 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Stretch(nameText.rectTransform);
            nameCanvas.gameObject.AddComponent<WorldBillboard>();

            var chatCanvas = WorldCanvas("Chat Bubble", new Vector3(0f, labelHeight + 0.70f, 0f), new Vector2(240f, 76f));
            var background = UiKit.Image(chatCanvas, "Bubble", new Color(1f, 1f, 1f, 0.96f));
            UiKit.Stretch(background.rectTransform);
            bubbleText = UiKit.Text(background.rectTransform, string.Empty, 17, new Color(0.10f, 0.13f, 0.16f), TextAnchor.MiddleCenter);
            bubbleText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            bubbleText.rectTransform.anchorMax = new Vector2(0.94f, 0.92f);
            bubbleText.rectTransform.offsetMin = Vector2.zero;
            bubbleText.rectTransform.offsetMax = Vector2.zero;
            chatCanvas.gameObject.AddComponent<WorldBillboard>();
            bubble = chatCanvas.gameObject;
            bubble.SetActive(false);
        }

        private RectTransform WorldCanvas(string name, Vector3 localPosition, Vector2 size)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.0105f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        private void SetPartColor(string prefix, Color color)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    renderer.sharedMaterial = HibiMaterials.Get(color);
                }
            }
        }

        private void Update()
        {
            if (bodyRoot == null)
            {
                return;
            }
            walkCycle += Time.deltaTime * (moving ? 9f : 2f);
            var bob = moving ? Mathf.Abs(Mathf.Sin(walkCycle)) * 0.07f : Mathf.Sin(walkCycle * 0.45f) * 0.015f;
            bodyRoot.localPosition = Vector3.up * bob;

            var leftAngle = moving ? Mathf.Sin(walkCycle) * 24f : 0f;
            var rightAngle = -leftAngle;
            if (Time.time < emoteUntil)
            {
                if (emote == "wave")
                {
                    rightAngle = -145f + Mathf.Sin(Time.time * 12f) * 24f;
                }
                else if (emote == "cheer")
                {
                    leftAngle = 145f;
                    rightAngle = -145f;
                    bodyRoot.localPosition += Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.14f;
                }
                else if (emote == "dance")
                {
                    leftAngle = Mathf.Sin(Time.time * 9f) * 70f;
                    rightAngle = -leftAngle;
                    bodyRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 5f) * 18f, 0f);
                }
            }
            else
            {
                bodyRoot.localRotation = Quaternion.identity;
            }

            leftArm.localRotation = Quaternion.Euler(leftAngle, 0f, 0f);
            rightArm.localRotation = Quaternion.Euler(rightAngle, 0f, 0f);
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color,
            Vector3 rotation = default, float bevel = 0f)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = HibiMaterials.Get(color);
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            return part;
        }
    }

    public sealed class AvatarMotor : MonoBehaviour
    {
        private AvatarVisual avatar;
        private Vector3 target;
        private bool hasTarget;
        private float sendAt;

        private void Awake()
        {
            avatar = GetComponent<AvatarVisual>();
            target = transform.position;
        }

        private void Update()
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0) && Camera.main != null)
                {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out var hit, 200f, 1 << PlazaWorld.GroundLayer))
                    {
                        target = hit.point;
                        target.y = 0f;
                        hasTarget = true;
                    }
                }
            }

            var keyboard = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Vector3 direction;
            if (keyboard.sqrMagnitude > 0.05f)
            {
                var cameraForward = Camera.main == null ? Vector3.forward : Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                var cameraRight = Camera.main == null ? Vector3.right : Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
                direction = (cameraForward * keyboard.z + cameraRight * keyboard.x).normalized;
                target = transform.position + direction * 2f;
                hasTarget = false;
            }
            else if (hasTarget)
            {
                direction = target - transform.position;
                direction.y = 0f;
                if (direction.magnitude < 0.12f)
                {
                    hasTarget = false;
                    direction = Vector3.zero;
                }
                else
                {
                    direction.Normalize();
                }
            }
            else
            {
                direction = Vector3.zero;
            }

            var moving = direction.sqrMagnitude > 0.01f;
            avatar.SetMoving(moving);
            if (moving)
            {
                transform.position += direction * 4.5f * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-direction), Time.deltaTime * 11f);
            }

            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, -31f, 31f),
                0f,
                Mathf.Clamp(transform.position.z, -24f, 24f));

            if (Time.time >= sendAt)
            {
                sendAt = Time.time + 0.10f;
                HibiNetwork.Instance?.SendMove(transform.position, transform.eulerAngles.y);
            }
        }
    }

    public sealed class RemoteAvatar : MonoBehaviour
    {
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private AvatarVisual visual;

        private void Awake()
        {
            visual = GetComponent<AvatarVisual>();
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        public void SetState(Vector3 position, float rotation)
        {
            targetPosition = position;
            targetRotation = Quaternion.Euler(0f, rotation, 0f);
        }

        private void Update()
        {
            var before = transform.position;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 9f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            visual.SetMoving((transform.position - before).sqrMagnitude > 0.00002f);
        }
    }

    public sealed class WorldBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
            }
        }
    }
}
