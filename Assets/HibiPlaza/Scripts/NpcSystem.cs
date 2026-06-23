using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace HibiPlaza
{
    public sealed class PlazaNpc : MonoBehaviour
    {
        public const int ResidentCount = 4;

        private static readonly string[] Names = { "Momo", "Kai", "Sora", "Yui" };
        private static readonly string[] Models =
        {
            "ThirdParty/Kenney/MiniCharacters/character-female-a",
            "ThirdParty/Kenney/MiniCharacters/character-male-a",
            "ThirdParty/Kenney/MiniCharacters/character-female-b",
            "ThirdParty/Kenney/MiniCharacters/character-male-b"
        };
        private static readonly Vector3[] Starts =
        {
            new Vector3(-4.5f, 0f, 0.5f),
            new Vector3(4.5f, 0f, 2.0f),
            new Vector3(-9f, 0f, -8f),
            new Vector3(10f, 0f, -6f)
        };
        private static readonly Vector3[][] Routes =
        {
            new[] { new Vector3(-7f, 0f, -5f), new Vector3(-5f, 0f, 2f), new Vector3(-7f, 0f, 9f), new Vector3(-2f, 0f, 11f) },
            new[] { new Vector3(7f, 0f, -4f), new Vector3(9f, 0f, 3f), new Vector3(5f, 0f, 11f), new Vector3(1f, 0f, 8f) },
            new[] { new Vector3(-9f, 0f, -8f), new Vector3(-14f, 0f, -7f), new Vector3(-13f, 0f, 1f), new Vector3(-9f, 0f, 4f) },
            new[] { new Vector3(10f, 0f, -6f), new Vector3(15f, 0f, -8f), new Vector3(15f, 0f, -1f), new Vector3(10f, 0f, 4f) }
        };
        private static readonly string[][] Dialog =
        {
            new[] { "Welcome to Hibi Plaza!", "The fountain is sparkling today.", "I love seeing everyone's outfits." },
            new[] { "The cafe is my favorite spot.", "Have you tried the dance emote?", "Nice to meet you!" },
            new[] { "I'm taking a walk around the plaza.", "The flower planters look lovely.", "Hello!" },
            new[] { "The shops have new displays.", "Wave when you see me!", "What a nice day." }
        };

        private int residentIndex;
        private int waypointIndex;
        private float waitUntil;
        private float nextSpeechAt;
        private bool walking;
        private GameObject bubble;
        private Text bubbleText;
        private Animator animator;
        private PlayableGraph animationGraph;
        private AnimationClipPlayable animationPlayable;
        private AnimationClip currentClip;
        private string currentAnimation;
        private AnimationClip[] clips;

        public static void SpawnResidents(Transform parent)
        {
            for (var i = 0; i < ResidentCount; i++)
            {
                var root = new GameObject("NPC " + Names[i], typeof(PlazaNpc));
                root.transform.SetParent(parent, false);
                root.transform.localPosition = Starts[i];
                root.GetComponent<PlazaNpc>().Initialize(i);
            }
        }

        private void Initialize(int index)
        {
            residentIndex = index;
            waypointIndex = 0;
            waitUntil = Time.time + 1.2f + index * 0.45f;
            nextSpeechAt = Time.time + 3.5f + index * 1.7f;

            var prefab = Resources.Load<GameObject>(Models[index]);
            if (prefab == null)
            {
                Debug.LogError("Missing NPC model: " + Models[index]);
                return;
            }

            var model = Instantiate(prefab, transform, false);
            model.name = "Kenney Mini Character";
            model.transform.localScale = Vector3.one * 3.05f;
            model.transform.localRotation = Quaternion.identity;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            var avatars = Resources.LoadAll<Avatar>(Models[index]);
            if (avatars.Length > 0)
            {
                animator.avatar = avatars[0];
            }
            clips = Resources.LoadAll<AnimationClip>(Models[index]);
            animator.applyRootMotion = false;
            PlayAnimation("idle");
            CreateLabels();
        }

        private void Update()
        {
            LoopAnimation();
            if (bubble != null && Time.time >= nextSpeechAt)
            {
                ShowSpeech();
                nextSpeechAt = Time.time + UnityEngine.Random.Range(10f, 16f);
            }
            if (Time.time < waitUntil)
            {
                SetWalking(false);
                return;
            }

            var route = Routes[residentIndex];
            var target = route[waypointIndex];
            var direction = target - transform.position;
            direction.y = 0f;
            if (direction.magnitude < 0.18f)
            {
                waypointIndex = (waypointIndex + 1) % route.Length;
                waitUntil = Time.time + UnityEngine.Random.Range(1.8f, 4f);
                SetWalking(false);
                return;
            }

            direction.Normalize();
            transform.position += direction * (1.15f + residentIndex * 0.05f) * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            SetWalking(true);
        }

        private void SetWalking(bool value)
        {
            if (walking == value)
            {
                return;
            }
            walking = value;
            PlayAnimation(value ? "walk" : "idle");
        }

        private void PlayAnimation(string clipName)
        {
            if (animator == null || clips == null || currentAnimation == clipName)
            {
                return;
            }
            AnimationClip selected = null;
            foreach (var clip in clips)
            {
                if (string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                {
                    selected = clip;
                    break;
                }
            }
            if (selected == null)
            {
                return;
            }
            if (animationGraph.IsValid())
            {
                animationGraph.Destroy();
            }
            animationGraph = PlayableGraph.Create("NPC " + Names[residentIndex] + " " + clipName);
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(animationGraph, "NPC Animation", animator);
            animationPlayable = AnimationClipPlayable.Create(animationGraph, selected);
            output.SetSourcePlayable(animationPlayable);
            currentClip = selected;
            currentAnimation = clipName;
            animationGraph.Play();
        }

        private void LoopAnimation()
        {
            if (currentClip != null && animationPlayable.IsValid() && animationPlayable.GetTime() >= currentClip.length)
            {
                animationPlayable.SetTime(0d);
                animationPlayable.SetDone(false);
            }
        }

        private void CreateLabels()
        {
            var nameCanvas = WorldCanvas("NPC Name", new Vector3(0f, 2.75f, 0f), new Vector2(170f, 34f));
            var nameText = UiKit.Text(nameCanvas, Names[residentIndex] + "  •  LOCAL", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Stretch(nameText.rectTransform);
            var outline = nameText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.08f, 0.10f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            nameCanvas.gameObject.AddComponent<WorldBillboard>();

            var chatCanvas = WorldCanvas("NPC Bubble", new Vector3(0f, 3.42f, 0f), new Vector2(250f, 72f));
            var background = UiKit.Image(chatCanvas, "Bubble", new Color(1f, 1f, 1f, 0.97f));
            UiKit.Stretch(background.rectTransform);
            bubbleText = UiKit.Text(background.rectTransform, string.Empty, 16, new Color(0.08f, 0.13f, 0.16f), TextAnchor.MiddleCenter);
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
            canvas.sortingOrder = 6;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        private void ShowSpeech()
        {
            var lines = Dialog[residentIndex];
            bubbleText.text = lines[UnityEngine.Random.Range(0, lines.Length)];
            bubble.SetActive(true);
            CancelInvoke(nameof(HideSpeech));
            Invoke(nameof(HideSpeech), 4.8f);
        }

        private void HideSpeech()
        {
            if (bubble != null)
            {
                bubble.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (animationGraph.IsValid())
            {
                animationGraph.Destroy();
            }
        }
    }
}
