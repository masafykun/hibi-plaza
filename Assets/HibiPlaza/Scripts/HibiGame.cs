using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HibiPlaza
{
    public enum HibiState
    {
        Landing,
        Customizing,
        Plaza
    }

    public sealed class HibiGame : MonoBehaviour
    {
        public static HibiGame Instance { get; private set; }

        public HibiState State { get; private set; }
        public AvatarVisual LocalAvatar { get; private set; }
        public AvatarData CurrentAvatar { get; private set; }

        private readonly Dictionary<string, AvatarVisual> remoteAvatars = new Dictionary<string, AvatarVisual>();
        private GameObject sceneRoot;
        private Canvas activeCanvas;
        private AvatarVisual previewAvatar;
        private PlazaHud hud;
        private HibiNetwork network;
        private bool demoResidentsSpawned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (FindFirstObjectByType<HibiGame>() != null)
            {
                return;
            }
            var root = new GameObject("Hibi Plaza");
            DontDestroyOnLoad(root);
            root.AddComponent<HibiGame>();
        }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 70f;
            QualitySettings.shadowResolution = ShadowResolution.High;
            EnsureEventSystem();
            CurrentAvatar = LoadAvatar();
            ShowLanding();
        }

        private void Update()
        {
            if (State == HibiState.Landing
                && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                ShowCustomizer();
            }

            if (State == HibiState.Plaza && Input.GetKeyDown(KeyCode.Escape))
            {
                ShowLanding();
            }
        }

        public void ShowLanding()
        {
            ClearView();
            State = HibiState.Landing;
            activeCanvas = UiKit.CreateCanvas("Welcome", 30);
            var root = activeCanvas.transform as RectTransform;

            var art = UiKit.Image(root, "Plaza Art", Color.white);
            UiKit.Stretch(art.rectTransform);
            art.sprite = Resources.Load<Sprite>("TitleArt");

            var shade = UiKit.Image(root, "Welcome Shade", new Color(0.035f, 0.08f, 0.12f, 0.77f));
            UiKit.Place(shade.rectTransform, 0f, 0f, 0.53f, 1f);

            var content = new GameObject("Welcome Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root, false);
            UiKit.Place(content, 0.065f, 0.12f, 0.48f, 0.88f);

            var kicker = UiKit.Text(content, "YOUR EVERYDAY VIRTUAL PLAZA", 19, new Color(1f, 0.72f, 0.26f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(kicker.rectTransform, 0f, 0.82f, 1f, 0.92f);
            var title = UiKit.Text(content, "HIBI\nPLAZA", 80, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 0f, 0.48f, 1f, 0.84f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 46;
            title.resizeTextMaxSize = 80;
            var subtitle = UiKit.Text(content, "Create your look, meet new people,\nand make the plaza your place.", 25,
                new Color(0.91f, 0.95f, 0.96f), TextAnchor.UpperLeft);
            UiKit.Place(subtitle.rectTransform, 0f, 0.28f, 0.95f, 0.48f);
            var button = UiKit.Button(content, "CREATE MY AVATAR", new Color(0.94f, 0.35f, 0.29f));
            UiKit.Place(button.GetComponent<RectTransform>(), 0f, 0.08f, 0.66f, 0.24f);
            button.onClick.AddListener(ShowCustomizer);

            var footer = UiKit.Text(root, "Walk together  |  Chat safely  |  Express yourself", 17,
                new Color(1f, 1f, 1f, 0.74f), TextAnchor.MiddleCenter);
            UiKit.Place(footer.rectTransform, 0.18f, 0.025f, 0.82f, 0.075f);
        }

        public void ShowCustomizer()
        {
            ClearView();
            State = HibiState.Customizing;
            sceneRoot = new GameObject("Avatar Studio");
            BuildAvatarStudio(sceneRoot.transform);
            previewAvatar = AvatarVisual.Create(CurrentAvatar, sceneRoot.transform, new Vector3(-1.25f, 0f, 0f));
            previewAvatar.SetLabelsVisible(false);

            activeCanvas = UiKit.CreateCanvas("Avatar Creator", 25);
            var root = activeCanvas.transform as RectTransform;
            var header = UiKit.Image(root, "Header", new Color(0.04f, 0.10f, 0.14f, 0.92f));
            UiKit.Place(header.rectTransform, 0f, 0.91f, 1f, 1f);
            var headerText = UiKit.Text(header.rectTransform, "HIBI PLAZA  /  AVATAR STUDIO", 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            headerText.rectTransform.anchorMin = new Vector2(0.035f, 0f);
            headerText.rectTransform.anchorMax = new Vector2(0.65f, 1f);
            headerText.rectTransform.offsetMin = Vector2.zero;
            headerText.rectTransform.offsetMax = Vector2.zero;

            var back = UiKit.Button(header.rectTransform, "BACK", new Color(0.18f, 0.25f, 0.29f));
            UiKit.Place(back.GetComponent<RectTransform>(), 0.84f, 0.18f, 0.96f, 0.82f);
            back.onClick.AddListener(ShowLanding);

            var panel = UiKit.Image(root, "Customizer Panel", new Color(0.99f, 0.985f, 0.96f, 0.97f));
            UiKit.Place(panel.rectTransform, 0.61f, 0.06f, 0.965f, 0.88f);

            var title = UiKit.Text(panel.rectTransform, "MAKE YOUR AVATAR", 29, new Color(0.09f, 0.16f, 0.20f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 0.08f, 0.87f, 0.92f, 0.97f);

            var nameLabel = UiKit.Text(panel.rectTransform, "DISPLAY NAME", 15, new Color(0.36f, 0.43f, 0.46f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(nameLabel.rectTransform, 0.08f, 0.78f, 0.92f, 0.84f);
            var nameInput = UiKit.Input(panel.rectTransform, "What should we call you?", CurrentAvatar.displayName);
            UiKit.Place(nameInput.GetComponent<RectTransform>(), 0.08f, 0.69f, 0.92f, 0.78f);
            nameInput.characterLimit = 16;
            nameInput.onValueChanged.AddListener(value =>
            {
                CurrentAvatar.displayName = string.IsNullOrWhiteSpace(value) ? "Guest" : value.Trim();
                previewAvatar.Apply(CurrentAvatar);
            });

            AddSwatchRow(panel.rectTransform, "SKIN", 0.59f, AvatarPalette.Skin, () => CurrentAvatar.skin,
                value => { CurrentAvatar.skin = value; previewAvatar.Apply(CurrentAvatar); });
            AddSwatchRow(panel.rectTransform, "HAIR", 0.47f, AvatarPalette.Hair, () => CurrentAvatar.hair,
                value => { CurrentAvatar.hair = value; previewAvatar.Apply(CurrentAvatar); });
            AddSwatchRow(panel.rectTransform, "TOP", 0.35f, AvatarPalette.Tops, () => CurrentAvatar.top,
                value => { CurrentAvatar.top = value; previewAvatar.Apply(CurrentAvatar); });
            AddSwatchRow(panel.rectTransform, "BOTTOM", 0.23f, AvatarPalette.Bottoms, () => CurrentAvatar.bottom,
                value => { CurrentAvatar.bottom = value; previewAvatar.Apply(CurrentAvatar); });

            var hairStyle = UiKit.Button(panel.rectTransform, "CHANGE HAIRSTYLE", new Color(0.22f, 0.64f, 0.62f));
            UiKit.Place(hairStyle.GetComponent<RectTransform>(), 0.08f, 0.12f, 0.52f, 0.20f);
            hairStyle.onClick.AddListener(() =>
            {
                CurrentAvatar.hairStyle = (CurrentAvatar.hairStyle + 1) % 4;
                previewAvatar.Apply(CurrentAvatar);
            });

            var enter = UiKit.Button(panel.rectTransform, "ENTER THE PLAZA", new Color(0.94f, 0.35f, 0.29f));
            UiKit.Place(enter.GetComponent<RectTransform>(), 0.55f, 0.08f, 0.92f, 0.20f);
            enter.onClick.AddListener(() => EnterPlaza(true));
        }

        public void EnterPlaza(bool connectOnline)
        {
            SaveAvatar();
            ClearView();
            State = HibiState.Plaza;
            demoResidentsSpawned = false;
            remoteAvatars.Clear();

            sceneRoot = new GameObject("Hibi Plaza World");
            var camera = PlazaWorld.Build(sceneRoot.transform);
            LocalAvatar = AvatarVisual.Create(CurrentAvatar, sceneRoot.transform, PlazaWorld.SpawnPoints[0]);
            LocalAvatar.gameObject.AddComponent<AvatarMotor>();
            camera.Follow(LocalAvatar.transform);

            activeCanvas = UiKit.CreateCanvas("Social HUD", 20);
            hud = activeCanvas.gameObject.AddComponent<PlazaHud>();
            hud.Initialize(CurrentAvatar.displayName);

            if (connectOnline)
            {
                network = new GameObject("Realtime Network", typeof(HibiNetwork)).GetComponent<HibiNetwork>();
                network.transform.SetParent(sceneRoot.transform, false);
                network.Initialize(CurrentAvatar, LocalAvatar);
            }
            else
            {
                SetConnectionStatus("OFFLINE PREVIEW", 5);
                SpawnDemoResidents();
            }
        }

        public void SetConnectionStatus(string message, int onlineCount)
        {
            hud?.SetConnection(message, onlineCount);
        }

        public void NetworkUnavailable()
        {
            SetConnectionStatus("OFFLINE PREVIEW", 1);
            SpawnDemoResidents();
        }

        public void AddChat(string name, string message, string playerId)
        {
            hud?.AddMessage(name, message);
            if (playerId == HibiNetwork.Instance?.LocalId)
            {
                LocalAvatar?.ShowChat(message);
            }
            else if (!string.IsNullOrEmpty(playerId) && remoteAvatars.TryGetValue(playerId, out var remote))
            {
                remote.ShowChat(message);
            }
        }

        public void SubmitChat(string message)
        {
            message = SanitizeLocalMessage(message);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (network != null && network.Connected)
            {
                network.SendChat(message);
            }
            else
            {
                LocalAvatar.ShowChat(message);
                hud?.AddMessage(CurrentAvatar.displayName, message);
            }
        }

        public void TriggerEmote(string emote)
        {
            LocalAvatar?.PlayEmote(emote);
            if (network != null && network.Connected)
            {
                network.SendEmote(emote);
            }
        }

        public void UpsertRemote(NetPlayer player)
        {
            if (player == null || player.id == HibiNetwork.Instance?.LocalId)
            {
                return;
            }

            if (!remoteAvatars.TryGetValue(player.id, out var avatar))
            {
                var avatarData = JsonUtility.FromJson<AvatarData>(player.avatar) ?? new AvatarData { displayName = player.name };
                avatarData.displayName = player.name;
                avatar = AvatarVisual.Create(avatarData, sceneRoot.transform, new Vector3(player.x, 0f, player.z));
                avatar.gameObject.AddComponent<RemoteAvatar>();
                remoteAvatars[player.id] = avatar;
            }
            avatar.GetComponent<RemoteAvatar>().SetState(new Vector3(player.x, 0f, player.z), player.r);
            hud?.SetConnection(network != null && network.Connected ? "LIVE" : "OFFLINE PREVIEW", remoteAvatars.Count + 1);
        }

        public void RemoveRemote(string id)
        {
            if (remoteAvatars.TryGetValue(id, out var avatar))
            {
                Destroy(avatar.gameObject);
                remoteAvatars.Remove(id);
                hud?.SetConnection("LIVE", remoteAvatars.Count + 1);
            }
        }

        public void RemoteEmote(string id, string emote)
        {
            if (remoteAvatars.TryGetValue(id, out var avatar))
            {
                avatar.PlayEmote(emote);
            }
        }

        private void SpawnDemoResidents()
        {
            if (demoResidentsSpawned || sceneRoot == null)
            {
                return;
            }
            demoResidentsSpawned = true;
            var names = new[] { "Mina", "Ren", "Sora", "Aoi" };
            for (var i = 0; i < names.Length; i++)
            {
                var avatarData = new AvatarData
                {
                    displayName = names[i],
                    skin = i % AvatarPalette.Skin.Length,
                    hair = (i + 2) % AvatarPalette.Hair.Length,
                    top = (i + 1) % AvatarPalette.Tops.Length,
                    bottom = (i + 2) % AvatarPalette.Bottoms.Length,
                    hairStyle = i
                };
                var avatar = AvatarVisual.Create(avatarData, sceneRoot.transform, PlazaWorld.SpawnPoints[i + 1]);
                var resident = avatar.gameObject.AddComponent<DemoResident>();
                resident.Initialize(i);
            }
        }

        private static string SanitizeLocalMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            value = value.Trim().Replace("\n", " ").Replace("\r", " ");
            return value.Length > 120 ? value.Substring(0, 120) : value;
        }

        private void AddSwatchRow(RectTransform panel, string label, float y, Color[] colors, Func<int> current, Action<int> apply)
        {
            var title = UiKit.Text(panel, label, 14, new Color(0.36f, 0.43f, 0.46f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 0.08f, y + 0.065f, 0.30f, y + 0.12f);
            var count = colors.Length;
            var start = 0.28f;
            var available = 0.64f;
            var width = Mathf.Min(0.075f, available / count - 0.008f);
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var swatch = UiKit.Button(panel, string.Empty, colors[i]);
                var x = start + i * (available / count);
                UiKit.Place(swatch.GetComponent<RectTransform>(), x, y + 0.045f, x + width, y + 0.115f);
                swatch.onClick.AddListener(() => apply(index));
            }
        }

        private void BuildAvatarStudio(Transform root)
        {
            Part(PrimitiveType.Cube, "Studio Floor", root, new Vector3(0f, -0.28f, 0f), new Vector3(16f, 0.5f, 14f), new Color(0.79f, 0.88f, 0.87f));
            Part(PrimitiveType.Cylinder, "Preview Platform", root, new Vector3(0f, 0.06f, 0f), new Vector3(4.5f, 0.16f, 4.5f), new Color(0.98f, 0.75f, 0.32f));

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.position = new Vector3(-1.25f, 2.75f, -8.8f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(-1.25f, 1.72f, 0f) - cameraObject.transform.position);
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 34f;
            camera.backgroundColor = new Color(0.68f, 0.86f, 0.89f);

            var key = new GameObject("Studio Key", typeof(Light));
            key.transform.SetParent(root, false);
            key.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var keyLight = key.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.0f;
            keyLight.color = new Color(1f, 0.82f, 0.68f);
            keyLight.shadows = LightShadows.Soft;

            var fill = new GameObject("Studio Fill", typeof(Light));
            fill.transform.SetParent(root, false);
            fill.transform.position = new Vector3(-3f, 4f, -3f);
            var fillLight = fill.GetComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 12f;
            fillLight.intensity = 1.35f;
            fillLight.color = new Color(0.50f, 0.76f, 1f);
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = HibiMaterials.Get(color);
            return part;
        }

        private AvatarData LoadAvatar()
        {
            var json = PlayerPrefs.GetString("hibi.avatar", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<AvatarData>(json) ?? new AvatarData();
                }
                catch
                {
                    // Ignore malformed local data and start fresh.
                }
            }
            return new AvatarData
            {
                displayName = "Guest" + UnityEngine.Random.Range(100, 999),
                skin = 0,
                hair = 0,
                top = 0,
                bottom = 0,
                hairStyle = 0
            };
        }

        private void SaveAvatar()
        {
            PlayerPrefs.SetString("hibi.avatar", JsonUtility.ToJson(CurrentAvatar));
            PlayerPrefs.Save();
        }

        private void ClearView()
        {
            if (network != null)
            {
                network.Disconnect();
                network = null;
            }
            if (activeCanvas != null)
            {
                Destroy(activeCanvas.gameObject);
            }
            if (sceneRoot != null)
            {
                Destroy(sceneRoot);
            }
            activeCanvas = null;
            sceneRoot = null;
            previewAvatar = null;
            LocalAvatar = null;
            hud = null;
            remoteAvatars.Clear();
            var camera = Camera.main;
            if (camera != null)
            {
                Destroy(camera.gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }
        }
    }

    public sealed class PlazaHud : MonoBehaviour
    {
        private readonly Queue<string> lines = new Queue<string>();
        private Text chatLog;
        private Text connection;
        private Text online;
        private InputField input;

        public void Initialize(string displayName)
        {
            var root = transform as RectTransform;
            var top = UiKit.Image(root, "Top Bar", new Color(0.035f, 0.09f, 0.13f, 0.92f));
            UiKit.Place(top.rectTransform, 0f, 0.92f, 1f, 1f);

            var brand = UiKit.Text(top.rectTransform, "HIBI PLAZA", 23, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(brand.rectTransform, 0.025f, 0f, 0.30f, 1f);
            connection = UiKit.Text(top.rectTransform, "CONNECTING", 16, new Color(1f, 0.72f, 0.26f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(connection.rectTransform, 0.43f, 0f, 0.60f, 1f);
            online = UiKit.Text(top.rectTransform, "1 ONLINE", 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.Place(online.rectTransform, 0.76f, 0f, 0.97f, 1f);

            var chatPanel = UiKit.Image(root, "Chat Panel", new Color(0.035f, 0.09f, 0.13f, 0.84f));
            UiKit.Place(chatPanel.rectTransform, 0.02f, 0.025f, 0.39f, 0.30f);
            var chatTitle = UiKit.Text(chatPanel.rectTransform, "PLAZA CHAT", 15, new Color(1f, 0.72f, 0.26f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(chatTitle.rectTransform, 0.05f, 0.81f, 0.95f, 0.96f);
            chatLog = UiKit.Text(chatPanel.rectTransform, "Welcome to Hibi Plaza.", 16, Color.white, TextAnchor.LowerLeft);
            UiKit.Place(chatLog.rectTransform, 0.05f, 0.26f, 0.95f, 0.82f);
            input = UiKit.Input(chatPanel.rectTransform, "Say something...", string.Empty);
            UiKit.Place(input.GetComponent<RectTransform>(), 0.04f, 0.05f, 0.96f, 0.24f);
            input.characterLimit = 120;
            input.onEndEdit.AddListener(SubmitInput);

            var emotes = UiKit.Image(root, "Emote Bar", new Color(0.035f, 0.09f, 0.13f, 0.84f));
            UiKit.Place(emotes.rectTransform, 0.68f, 0.035f, 0.975f, 0.14f);
            AddEmote(emotes.rectTransform, "WAVE", "wave", 0.03f, 0.32f, new Color(0.22f, 0.64f, 0.62f));
            AddEmote(emotes.rectTransform, "CHEER", "cheer", 0.35f, 0.65f, new Color(0.96f, 0.60f, 0.18f));
            AddEmote(emotes.rectTransform, "DANCE", "dance", 0.68f, 0.97f, new Color(0.75f, 0.42f, 0.82f));

            var hint = UiKit.Text(root, "Click to walk  |  WASD to move  |  Enter to chat  |  ESC to leave", 15,
                new Color(0.10f, 0.16f, 0.18f, 0.78f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(hint.rectTransform, 0.39f, 0.015f, 0.68f, 0.065f);
        }

        private void Update()
        {
            if (input != null && !input.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                input.ActivateInputField();
            }
        }

        private void SubmitInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            input.text = string.Empty;
            HibiGame.Instance.SubmitChat(message);
            input.ActivateInputField();
        }

        public void AddMessage(string name, string message)
        {
            lines.Enqueue($"<b>{name}</b>  {message}");
            while (lines.Count > 6)
            {
                lines.Dequeue();
            }
            chatLog.supportRichText = true;
            chatLog.text = string.Join("\n", lines);
        }

        public void SetConnection(string state, int count)
        {
            connection.text = state;
            connection.color = state == "LIVE" ? new Color(0.34f, 0.86f, 0.56f) : new Color(1f, 0.72f, 0.26f);
            online.text = count + (count == 1 ? " ONLINE" : " ONLINE");
        }

        private void AddEmote(RectTransform parent, string label, string emote, float minX, float maxX, Color color)
        {
            var button = UiKit.Button(parent, label, color);
            UiKit.Place(button.GetComponent<RectTransform>(), minX, 0.14f, maxX, 0.86f);
            button.onClick.AddListener(() => HibiGame.Instance.TriggerEmote(emote));
        }
    }

    public sealed class DemoResident : MonoBehaviour
    {
        private AvatarVisual visual;
        private Vector3 target;
        private float waitUntil;
        private int seed;

        public void Initialize(int value)
        {
            seed = value;
            visual = GetComponent<AvatarVisual>();
            PickTarget();
        }

        private void Update()
        {
            if (Time.time < waitUntil)
            {
                visual.SetMoving(false);
                return;
            }
            var direction = target - transform.position;
            direction.y = 0f;
            if (direction.magnitude < 0.25f)
            {
                waitUntil = Time.time + 1.5f + seed * 0.45f;
                visual.PlayEmote(seed % 2 == 0 ? "wave" : "dance");
                if (seed == 0)
                {
                    visual.ShowChat("Nice to meet you!");
                }
                PickTarget();
                return;
            }
            direction.Normalize();
            transform.position += direction * 1.9f * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
            visual.SetMoving(true);
        }

        private void PickTarget()
        {
            target = new Vector3(UnityEngine.Random.Range(-14f, 14f), 0f, UnityEngine.Random.Range(-11f, 14f));
        }
    }

    public static class UiKit
    {
        private static Font font;

        public static Canvas CreateCanvas(string name, int order)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static Image Image(RectTransform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(RectTransform parent, string value, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button Button(RectTransform parent, string label, Color color)
        {
            var image = Image(parent, label + " Button", color);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
            button.colors = colors;
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text(image.rectTransform, label, 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(text.rectTransform);
            }
            return button;
        }

        public static InputField Input(RectTransform parent, string placeholder, string value)
        {
            var background = Image(parent, "Input", new Color(0.94f, 0.95f, 0.93f, 1f));
            var field = background.gameObject.AddComponent<InputField>();
            var text = Text(background.rectTransform, value, 18, new Color(0.10f, 0.15f, 0.18f), TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = new Vector2(0.045f, 0.08f);
            text.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            var placeholderText = Text(background.rectTransform, placeholder, 18, new Color(0.35f, 0.42f, 0.45f, 0.65f), TextAnchor.MiddleLeft);
            placeholderText.rectTransform.anchorMin = new Vector2(0.045f, 0.08f);
            placeholderText.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            placeholderText.rectTransform.offsetMin = Vector2.zero;
            placeholderText.rectTransform.offsetMax = Vector2.zero;
            field.textComponent = text;
            field.placeholder = placeholderText;
            field.text = value;
            return field;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
