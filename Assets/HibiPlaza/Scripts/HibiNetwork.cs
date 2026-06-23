using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

namespace HibiPlaza
{
    [Serializable]
    public sealed class NetPlayer
    {
        public string id;
        public string name;
        public string avatar;
        public float x;
        public float z;
        public float r;
    }

    [Serializable]
    public sealed class NetMessage
    {
        public string type;
        public string id;
        public string name;
        public string avatar;
        public string text;
        public string emote;
        public string message;
        public float x;
        public float z;
        public float r;
        public int count;
        public NetPlayer[] players;
    }

    public sealed class HibiNetwork : MonoBehaviour
    {
        public static HibiNetwork Instance { get; private set; }

        private WebSocket socket;
        private AvatarData avatar;
        private AvatarVisual localAvatar;
        private bool manualClose;
        private bool fallbackStarted;
        private int connectionGeneration;

        public string LocalId { get; private set; }
        public bool Connected => socket != null && socket.State == WebSocketState.Open;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(AvatarData avatarData, AvatarVisual local)
        {
            avatar = avatarData.Copy();
            localAvatar = local;
            Connect();
        }

        private async void Connect()
        {
            var url = ResolveServerUrl();
            HibiGame.Instance.SetConnectionStatus("CONNECTING", 1);
            const int maxAttempts = 2;
            for (var attempt = 1; attempt <= maxAttempts && !manualClose; attempt++)
            {
                var attemptNumber = attempt;
                var generation = ++connectionGeneration;
                var candidate = new WebSocket(url);
                var opened = false;
                var connectionResult = new TaskCompletionSource<bool>();
                socket = candidate;
                candidate.OnOpen += () =>
                {
                    if (generation != connectionGeneration || manualClose)
                    {
                        return;
                    }

                    opened = true;
                    connectionResult.TrySetResult(true);
                    HibiGame.Instance.SetConnectionStatus("LIVE", 1);
                    Send(new NetMessage
                    {
                        type = "join",
                        name = avatar.displayName,
                        avatar = JsonUtility.ToJson(avatar),
                        x = localAvatar.transform.position.x,
                        z = localAvatar.transform.position.z,
                        r = localAvatar.transform.eulerAngles.y
                    });
                };
                candidate.OnMessage += bytes =>
                {
                    if (generation == connectionGeneration)
                    {
                        HandleMessage(Encoding.UTF8.GetString(bytes));
                    }
                };
                candidate.OnError += error =>
                {
                    if (generation == connectionGeneration)
                    {
                        Debug.LogWarning("Hibi network attempt " + attemptNumber + " failed: " + error);
                        connectionResult.TrySetResult(false);
                    }
                };
                candidate.OnClose += code =>
                {
                    if (generation != connectionGeneration || manualClose)
                    {
                        return;
                    }

                    if (opened)
                    {
                        StartFallback();
                    }
                    else
                    {
                        connectionResult.TrySetResult(false);
                    }
                };

                try
                {
                    await candidate.Connect();
                    var completed = await Task.WhenAny(connectionResult.Task, Task.Delay(10000));
                    if (completed != connectionResult.Task)
                    {
                        Debug.LogWarning("Hibi network attempt " + attemptNumber + " timed out.");
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Unable to connect to Hibi server: " + exception.Message);
                }

                if (opened || (connectionResult.Task.IsCompleted && connectionResult.Task.Result))
                {
                    return;
                }

                try
                {
                    if (candidate.State == WebSocketState.Connecting || candidate.State == WebSocketState.Open)
                    {
                        await candidate.Close();
                    }
                }
                catch
                {
                    // A failed connection may already be closed by the browser.
                }

                if (attempt < maxAttempts)
                {
                    HibiGame.Instance.SetConnectionStatus("RETRYING", 1);
                    await Task.Delay(900);
                }
            }

            StartFallback();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            socket?.DispatchMessageQueue();
#endif
        }

        public void SendMove(Vector3 position, float rotation)
        {
            if (!Connected)
            {
                return;
            }
            Send(new NetMessage
            {
                type = "move",
                x = position.x,
                z = position.z,
                r = rotation
            });
        }

        public void SendChat(string text)
        {
            if (Connected)
            {
                Send(new NetMessage { type = "chat", text = text });
            }
        }

        public void SendEmote(string value)
        {
            if (Connected)
            {
                Send(new NetMessage { type = "emote", emote = value });
            }
        }

        public async void Disconnect()
        {
            manualClose = true;
            if (socket != null)
            {
                try
                {
                    await socket.Close();
                }
                catch
                {
                    // The object may be closing as the scene is replaced.
                }
            }
        }

        private async void Send(NetMessage message)
        {
            if (!Connected)
            {
                return;
            }
            try
            {
                await socket.SendText(JsonUtility.ToJson(message));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Hibi send failed: " + exception.Message);
            }
        }

        private void HandleMessage(string json)
        {
            NetMessage message;
            try
            {
                message = JsonUtility.FromJson<NetMessage>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Ignored malformed network message: " + exception.Message);
                return;
            }

            if (message == null || string.IsNullOrEmpty(message.type))
            {
                return;
            }

            switch (message.type)
            {
                case "welcome":
                    LocalId = message.id;
                    if (message.players != null)
                    {
                        foreach (var player in message.players)
                        {
                            HibiGame.Instance.UpsertRemote(player);
                        }
                    }
                    HibiGame.Instance.SetConnectionStatus("LIVE", Mathf.Max(1, message.count));
                    break;
                case "player_join":
                    HibiGame.Instance.UpsertRemote(ToPlayer(message));
                    HibiGame.Instance.AddChat("Plaza", message.name + " joined.", string.Empty);
                    break;
                case "move":
                    HibiGame.Instance.UpsertRemote(ToPlayer(message));
                    break;
                case "chat":
                    HibiGame.Instance.AddChat(message.name, message.text, message.id);
                    break;
                case "emote":
                    if (message.id != LocalId)
                    {
                        HibiGame.Instance.RemoteEmote(message.id, message.emote);
                    }
                    break;
                case "player_leave":
                    HibiGame.Instance.RemoveRemote(message.id);
                    break;
                case "count":
                    HibiGame.Instance.SetConnectionStatus("LIVE", message.count);
                    break;
                case "error":
                    HibiGame.Instance.AddChat("System", message.message, string.Empty);
                    break;
            }
        }

        private static NetPlayer ToPlayer(NetMessage message)
        {
            return new NetPlayer
            {
                id = message.id,
                name = message.name,
                avatar = message.avatar,
                x = message.x,
                z = message.z,
                r = message.r
            };
        }

        private void StartFallback()
        {
            if (fallbackStarted || manualClose)
            {
                return;
            }
            fallbackStarted = true;
            HibiGame.Instance.NetworkUnavailable();
        }

        private static string ResolveServerUrl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var absolute = Application.absoluteURL;
            var queryIndex = absolute.IndexOf('?');
            if (queryIndex >= 0)
            {
                var query = absolute.Substring(queryIndex + 1).Split('&');
                foreach (var pair in query)
                {
                    var values = pair.Split('=');
                    if (values.Length == 2 && values[0] == "server")
                    {
                        return Uri.UnescapeDataString(values[1]);
                    }
                }
            }
            return "wss://hibi.160.251.234.247.nip.io/hibi";
#else
            return "ws://127.0.0.1:8787/hibi";
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
