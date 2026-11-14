using System;
using System.Globalization;
using System.Text;
using UnityEngine;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
#endif

namespace CyberRush
{
    public class LanPvpManager : MonoBehaviour
    {
        public static LanPvpManager Instance { get; private set; }

        public bool IsRunning { get; private set; }
        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }
        public string StatusText { get; private set; } = "IDLE";
        public int RemotePort { get; private set; } = 7777;
        public string TargetIp { get; private set; } = "127.0.0.1";

#if !UNITY_WEBGL || UNITY_EDITOR
        private UdpClient _udp;
        private IPEndPoint _remoteEndPoint;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
#endif
        private float _sendTimer;
        private float _lastRecvTime;
        private int _pendingActionFlags;
        private int _pendingOutgoingDamage;
        private int _localPacketSeq;
        private int _lastProcessedPeerSeq = -1;

        public RemotePvpAvatar RemoteAvatar { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public static LanPvpManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<LanPvpManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }
            var go = new GameObject("LanPvpManager");
            Instance = go.AddComponent<LanPvpManager>();
            return Instance;
        }

        public void SetRemoteAvatar(RemotePvpAvatar avatar)
        {
            RemoteAvatar = avatar;
            if (RemoteAvatar != null)
                RemoteAvatar.gameObject.SetActive(false);
        }

        public void StartHost(int port = 7777)
        {
            StopNetwork();
#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                RemotePort = port;
                IsHost = true;
                IsConnected = false;
                _udp = new UdpClient(port);
                _udp.Client.ReceiveTimeout = 0;
                _udp.BeginReceive(OnUdpReceive, null);
                IsRunning = true;
                StatusText = $"HOSTING UDP :{port} (WAITING FOR P2)";
                if (RemoteAvatar != null) RemoteAvatar.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                StatusText = $"HOST ERROR: {ex.Message}";
                StopNetwork();
            }
#else
            StatusText = "WEBGL: UDP UNSUPPORTED";
#endif
        }

        public void StartJoin(string ip, int port = 7777)
        {
            StopNetwork();
#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                TargetIp = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
                RemotePort = port;
                IsHost = false;
                IsConnected = false;
                _udp = new UdpClient();
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(TargetIp), port);
                _udp.BeginReceive(OnUdpReceive, null);
                IsRunning = true;
                StatusText = $"CONNECTING TO {TargetIp}:{port}...";
                if (RemoteAvatar != null) RemoteAvatar.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                StatusText = $"JOIN ERROR: {ex.Message}";
                StopNetwork();
            }
#else
            StatusText = "WEBGL: UDP UNSUPPORTED";
#endif
        }

        public void StopNetwork()
        {
            IsRunning = false;
            IsConnected = false;
            _lastProcessedPeerSeq = -1;
            _pendingActionFlags = 0;
            _pendingOutgoingDamage = 0;
#if !UNITY_WEBGL || UNITY_EDITOR
            _remoteEndPoint = null;
            while (_incoming.TryDequeue(out _)) { }
            if (_udp != null)
            {
                try { _udp.Close(); } catch { }
                _udp = null;
            }
#endif
            if (RemoteAvatar != null) RemoteAvatar.gameObject.SetActive(false);
            StatusText = "DISCONNECTED";
        }

        public void NotifyLocalAction(int flag)
        {
            if (IsRunning) _pendingActionFlags |= flag;
        }

        public void QueueOutgoingDamage(int dmg)
        {
            if (IsRunning && dmg > 0) _pendingOutgoingDamage += dmg;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void OnUdpReceive(IAsyncResult ar)
        {
            if (_udp == null || !IsRunning) return;
            try
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = _udp.EndReceive(ar, ref sender);
                if (IsHost || _remoteEndPoint == null)
                    _remoteEndPoint = sender;

                string msg = Encoding.UTF8.GetString(bytes);
                _incoming.Enqueue(msg);
                _udp.BeginReceive(OnUdpReceive, null);
            }
            catch
            {
                // Socket closed or transient UDP error
            }
        }
#endif

        private void Update()
        {
            if (!IsRunning) return;

#if !UNITY_WEBGL || UNITY_EDITOR
            while (_incoming.TryDequeue(out string msg))
            {
                ParseIncoming(msg);
            }

            if (IsConnected && Time.realtimeSinceStartup - _lastRecvTime > 4.0f)
            {
                StatusText = IsHost ? $"HOSTING :{RemotePort} (WAITING...)" : $"WAITING RESPONSE ({TargetIp}:{RemotePort})";
                IsConnected = false;
            }

            _sendTimer += Time.unscaledDeltaTime;
            if (_sendTimer >= 1f / 45f)
            {
                _sendTimer = 0f;
                SendLocalState();
            }
#endif
        }

        private void SendLocalState()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (_udp == null || _remoteEndPoint == null) return;
            var sm = StageManager.Instance;
            CyberParkourController p1 = sm != null ? sm.Player1 : null;
            if (p1 == null) return;

            _localPacketSeq++;
            Vector3 pos = p1.transform.position;
            float yaw = p1.transform.eulerAngles.y;
            float pitch = p1.cameraHolder != null ? p1.cameraHolder.localEulerAngles.x : 0f;

            string line = string.Format(
                CultureInfo.InvariantCulture,
                "CR1|{0}|{1:F2}|{2:F2}|{3:F2}|{4:F1}|{5:F1}|{6}|{7}|{8}|{9}",
                _localPacketSeq, pos.x, pos.y, pos.z, yaw, pitch,
                p1.ArenaHp, p1.ArenaKills, _pendingActionFlags, _pendingOutgoingDamage
            );
            _pendingActionFlags = 0;
            _pendingOutgoingDamage = 0;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                _udp.Send(bytes, bytes.Length, _remoteEndPoint);
            }
            catch { }
#endif
        }

        private void ParseIncoming(string msg)
        {
            var parts = msg.Split('|');
            if (parts.Length < 10 || parts[0] != "CR1") return;

            if (!int.TryParse(parts[1], out int seq)) return;
            if (seq <= _lastProcessedPeerSeq && _lastProcessedPeerSeq - seq < 200) return;
            _lastProcessedPeerSeq = seq;

            var inv = CultureInfo.InvariantCulture;
            if (!float.TryParse(parts[2], NumberStyles.Float, inv, out float x)) return;
            if (!float.TryParse(parts[3], NumberStyles.Float, inv, out float y)) return;
            if (!float.TryParse(parts[4], NumberStyles.Float, inv, out float z)) return;
            if (!float.TryParse(parts[5], NumberStyles.Float, inv, out float yaw)) return;
            if (!float.TryParse(parts[6], NumberStyles.Float, inv, out float pitch)) return;
            int.TryParse(parts[7], out int hp);
            int.TryParse(parts[8], out int kills);
            int.TryParse(parts[9], out int actionFlags);
            int dmgToLocal = parts.Length > 10 && int.TryParse(parts[10], out int d) ? d : 0;

            _lastRecvTime = Time.realtimeSinceStartup;
            if (!IsConnected)
            {
                IsConnected = true;
#if !UNITY_WEBGL || UNITY_EDITOR
                StatusText = $"CONNECTED ({_remoteEndPoint?.Address})";
#else
                StatusText = "CONNECTED";
#endif
                CyberHUD.ShowCallout("LAN OPPONENT CONNECTED!", new Color(0.18f, 0.95f, 1f));
            }

            if (RemoteAvatar != null)
            {
                if (!RemoteAvatar.gameObject.activeSelf) RemoteAvatar.gameObject.SetActive(true);
                RemoteAvatar.SetPose(new Vector3(x, y, z), yaw, hp, kills);
                if (actionFlags != 0) RemoteAvatar.PlayRemoteAction(actionFlags, pitch);
            }

            if (dmgToLocal > 0 && StageManager.Instance != null && StageManager.Instance.Player1 != null)
            {
                StageManager.Instance.Player1.TakeCombatDamage(dmgToLocal, null);
            }
        }

        private void OnDestroy()
        {
            StopNetwork();
        }
    }
}
