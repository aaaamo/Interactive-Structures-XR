using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using TMPro;
using System.Threading;

public class NetworkConnect : MonoBehaviour
{
    [Header("Manual Setup (Optional)")]
    public GameObject networkManagerPrefab;

    [Header("VR UI (Auto-created if null)")]
    public TextMeshPro statusText;
    public Transform uiAnchor;

    [Header("Auto Start")]
    [Tooltip("Automatically start as host when the app launches — skips the manual network setup step.")]
    public bool autoStartAsHost = true;

    [Header("Network Settings")]
    public string defaultHostIP = "192.168.1.100";
    public int discoveryPort = 47777;

    [Header("Controller Reference")]
    public OVRGraphController graphController;

    public enum ConnectionState { Disconnected, SelectingMode, EnteringIP, Connecting, Connected, Failed }
    public ConnectionState state = ConnectionState.Disconnected;

    public bool IsConnected => NetworkManager.Singleton != null &&
                               (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

    private float connectionTimeout = 5f;
    private float connectionStartTime;

    // Host discovery
    private List<string> discoveredHosts = new List<string>();
    private int selectedHostIndex = 0;
    private UdpClient broadcastClient;
    private Thread listenerThread;
    private bool isListening = false;
    private float lastBroadcastTime = 0f;
    private float broadcastInterval = 1f;

    // IP input
    private int selectedIPDigit = 0;
    private int[] ipOctets = { 192, 168, 1, 100 };
    private bool manualIPMode = false;

    private float buttonCooldown = 0.3f;
    private float lastButtonTime = 0f;

    private GameObject uiObject;

    void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            if (networkManagerPrefab != null)
            {
                Instantiate(networkManagerPrefab);
                Debug.Log("[NetworkConnect] NetworkManager instantiated from prefab");
            }
            else
            {
                Debug.LogError("[NetworkConnect] NetworkManager not found! Add NetworkManager + UnityTransport to scene.");
            }
        }
    }

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkConnect] NetworkManager still null after Awake!");
            return;
        }
        Debug.Log($"[NetworkConnect] NetworkManager ready. Local IP: {GetLocalIPAddress()}");

        ParseIPString(defaultHostIP);

        if (statusText == null)
        {
            CreateVRUI();
        }

        if (graphController == null)
        {
            graphController = FindObjectOfType<OVRGraphController>();
        }

        state = ConnectionState.SelectingMode;
        UpdateStatusText();

        // Register network callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Auto-start as host so the app is immediately usable without manual setup
        if (autoStartAsHost)
        {
            StartAsHost();
        }
    }

    void OnDestroy()
    {
        StopHostDiscovery();
        StopBroadcasting();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkConnect] Client connected: {clientId}");

        // If we're the client that just connected
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            state = ConnectionState.Connected;
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            Debug.Log("[NetworkConnect] Successfully connected to server!");
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkConnect] Client disconnected callback - clientId: {clientId}, LocalClientId: {NetworkManager.Singleton.LocalClientId}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}, CurrentState: {state}");

        // If we got disconnected
        if (NetworkManager.Singleton.LocalClientId == clientId || !NetworkManager.Singleton.IsConnectedClient)
        {
            if (state == ConnectionState.Connecting)
            {
                state = ConnectionState.Failed;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
                Debug.LogWarning($"[NetworkConnect] Failed to connect to server! DisconnectReason may be in NetworkManager logs");
            }
            else if (state == ConnectionState.Connected)
            {
                state = ConnectionState.SelectingMode;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                Debug.Log("[NetworkConnect] Disconnected from server");
            }
        }
    }

    void CreateVRUI()
    {
        uiObject = new GameObject("NetworkStatusUI");

        if (uiAnchor != null)
        {
            uiObject.transform.SetParent(uiAnchor);
            uiObject.transform.localPosition = new Vector3(0, 0.1f, 0.1f);
            uiObject.transform.localRotation = Quaternion.Euler(45, 0, 0);
        }
        else
        {
            uiObject.transform.position = new Vector3(0, 1.2f, 0.5f);
        }

        statusText = uiObject.AddComponent<TextMeshPro>();
        statusText.fontSize = 0.3f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.rectTransform.sizeDelta = new Vector2(0.8f, 0.5f);
        statusText.richText = true;

        uiObject.AddComponent<Billboard>();
    }

    void Update()
    {
        if (NetworkManager.Singleton == null) return;

        bool isNetworkMode = graphController != null &&
                            graphController.currentMode == OVRGraphController.Mode.Setup &&
                            graphController.currentSetupTool == OVRGraphController.SetupTool.Network;

        if (uiObject != null)
        {
            uiObject.SetActive(isNetworkMode);
        }
        if (statusText != null && statusText.gameObject != uiObject)
        {
            statusText.gameObject.SetActive(isNetworkMode);
        }

        if (!isNetworkMode)
        {
            StopHostDiscovery();
            return;
        }

        // Position UI relative to camera (works regardless of tracking origin)
        if (statusText != null)
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            if (uiAnchor != null)
            {
                statusText.transform.position = uiAnchor.position + uiAnchor.up * 0.1f + uiAnchor.forward * 0.1f;
            }
            else if (camTransform != null)
            {
                // Position in front of camera
                statusText.transform.position = camTransform.position + camTransform.forward * 0.5f + Vector3.up * -0.1f;
            }
        }

        // Host broadcasts its presence
        if (state == ConnectionState.Connected && NetworkManager.Singleton.IsHost)
        {
            BroadcastHostPresence();
        }

        // Check connection timeout
        if (state == ConnectionState.Connecting)
        {
            if (Time.time - connectionStartTime > connectionTimeout)
            {
                Debug.LogWarning("[NetworkConnect] Connection timed out!");
                NetworkManager.Singleton.Shutdown();
                state = ConnectionState.Failed;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
            }
        }

        HandleVRInput();
        UpdateStatusText();
    }

    void HandleVRInput()
    {
        if (Time.time - lastButtonTime < buttonCooldown) return;

        bool yButton = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
        bool xButton = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool leftStickClick = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

        switch (state)
        {
            case ConnectionState.SelectingMode:
                if (yButton)
                {
                    StartAsHost();
                    lastButtonTime = Time.time;
                }
                else if (xButton)
                {
                    state = ConnectionState.EnteringIP;
                    manualIPMode = false;
                    selectedHostIndex = 0;
                    StartHostDiscovery();
                    lastButtonTime = Time.time;
                }
                break;

            case ConnectionState.EnteringIP:
                if (manualIPMode)
                {
                    // Manual IP input mode
                    if (leftStick.x > 0.7f)
                    {
                        selectedIPDigit = (selectedIPDigit + 1) % 4;
                        lastButtonTime = Time.time;
                        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
                    }
                    else if (leftStick.x < -0.7f)
                    {
                        selectedIPDigit = (selectedIPDigit + 3) % 4;
                        lastButtonTime = Time.time;
                        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
                    }
                    else if (leftStick.y > 0.7f)
                    {
                        ipOctets[selectedIPDigit] = Mathf.Min(255, ipOctets[selectedIPDigit] + 1);
                        lastButtonTime = Time.time;
                    }
                    else if (leftStick.y < -0.7f)
                    {
                        ipOctets[selectedIPDigit] = Mathf.Max(0, ipOctets[selectedIPDigit] - 1);
                        lastButtonTime = Time.time;
                    }

                    if (leftStickClick)
                    {
                        // Switch back to discovered hosts
                        manualIPMode = false;
                        lastButtonTime = Time.time;
                    }
                }
                else
                {
                    // Discovered hosts selection mode
                    if (leftStick.y > 0.7f && discoveredHosts.Count > 0)
                    {
                        selectedHostIndex = (selectedHostIndex - 1 + discoveredHosts.Count) % discoveredHosts.Count;
                        lastButtonTime = Time.time;
                        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
                    }
                    else if (leftStick.y < -0.7f && discoveredHosts.Count > 0)
                    {
                        selectedHostIndex = (selectedHostIndex + 1) % discoveredHosts.Count;
                        lastButtonTime = Time.time;
                        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
                    }

                    if (leftStickClick)
                    {
                        // Switch to manual IP input
                        manualIPMode = true;
                        selectedIPDigit = 0;
                        lastButtonTime = Time.time;
                    }
                }

                if (xButton)
                {
                    if (manualIPMode || discoveredHosts.Count == 0)
                    {
                        StartAsClient(GetIPString());
                    }
                    else
                    {
                        StartAsClient(discoveredHosts[selectedHostIndex]);
                    }
                    lastButtonTime = Time.time;
                }
                else if (yButton)
                {
                    StopHostDiscovery();
                    state = ConnectionState.SelectingMode;
                    lastButtonTime = Time.time;
                }
                break;

            case ConnectionState.Connecting:
                // Allow cancel with Y button
                if (yButton)
                {
                    NetworkManager.Singleton.Shutdown();
                    state = ConnectionState.SelectingMode;
                    lastButtonTime = Time.time;
                    Debug.Log("[NetworkConnect] Connection cancelled");
                }
                break;

            case ConnectionState.Connected:
                if (yButton)
                {
                    Disconnect();
                    lastButtonTime = Time.time;
                }
                break;

            case ConnectionState.Failed:
                // Any button returns to selection
                if (yButton || xButton)
                {
                    state = ConnectionState.SelectingMode;
                    lastButtonTime = Time.time;
                }
                break;
        }
    }

    #region Host Discovery

    void StartHostDiscovery()
    {
        if (isListening) return;

        discoveredHosts.Clear();
        isListening = true;

        listenerThread = new Thread(ListenForHosts);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        Debug.Log("[NetworkConnect] Started host discovery");
    }

    void StopHostDiscovery()
    {
        isListening = false;

        if (broadcastClient != null)
        {
            broadcastClient.Close();
            broadcastClient = null;
        }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(100);
            listenerThread = null;
        }
    }

    void ListenForHosts()
    {
        try
        {
            broadcastClient = new UdpClient(discoveryPort);
            broadcastClient.Client.ReceiveTimeout = 500;

            while (isListening)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = broadcastClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);

                    if (message.StartsWith("HOST:"))
                    {
                        string hostIP = message.Substring(5);
                        lock (discoveredHosts)
                        {
                            if (!discoveredHosts.Contains(hostIP))
                            {
                                discoveredHosts.Add(hostIP);
                                Debug.Log($"[NetworkConnect] Discovered host: {hostIP}");
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    // Timeout, continue listening
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetworkConnect] Discovery listener error: {e.Message}");
        }
    }

    void BroadcastHostPresence()
    {
        if (Time.time - lastBroadcastTime < broadcastInterval) return;
        lastBroadcastTime = Time.time;

        try
        {
            using (UdpClient client = new UdpClient())
            {
                client.EnableBroadcast = true;
                string message = "HOST:" + GetLocalIPAddress();
                byte[] data = Encoding.UTF8.GetBytes(message);
                client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, discoveryPort));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetworkConnect] Broadcast error: {e.Message}");
        }
    }

    void StopBroadcasting()
    {
        // Broadcasting stops automatically when not in Connected state as Host
    }

    #endregion

    void StartAsHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Set server to listen on all interfaces (0.0.0.0) so other devices can connect
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            Debug.Log($"[NetworkConnect] Host transport - Port: {transport.ConnectionData.Port}, ServerListenAddress: {transport.ConnectionData.ServerListenAddress}");
        }

        // Destroy any leftover spawned NetworkObjects from a previous session.
        // If they remain in the scene, NetworkManager throws a GlobalObjectIdHash collision.
        DestroyLeftoverNetworkObjects();
        graphController?.OnStructureReset();

        RegisterPrefabs();
        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"[NetworkConnect] StartHost returned: {started}");

        if (started)
        {
            state = ConnectionState.Connected;
            Debug.Log($"[NetworkConnect] Started as HOST. IP: {GetLocalIPAddress()}, Port: {transport?.ConnectionData.Port}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
        }
        else
        {
            state = ConnectionState.Failed;
            Debug.LogError("[NetworkConnect] Failed to start host!");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
        }
    }

    void StartAsClient(string targetIP)
    {
        StopHostDiscovery();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = targetIP;
            // Make sure port matches (default is 7777)
            Debug.Log($"[NetworkConnect] Transport settings - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}");
        }
        else
        {
            Debug.LogError("[NetworkConnect] UnityTransport not found!");
        }

        // Update ipOctets for display
        ParseIPString(targetIP);

        DestroyLeftoverNetworkObjects();
        graphController?.OnStructureReset();
        RegisterPrefabs();

        Debug.Log($"[NetworkConnect] Starting client connection to: {targetIP}");
        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"[NetworkConnect] StartClient returned: {started}");

        state = ConnectionState.Connecting;
        connectionStartTime = Time.time;
    }

    void Disconnect()
    {
        StopBroadcasting();
        NetworkManager.Singleton.Shutdown();
        state = ConnectionState.SelectingMode;
        Debug.Log("[NetworkConnect] Disconnected");
        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;

        string text;

        switch (state)
        {
            case ConnectionState.SelectingMode:
                text = "<b>Network Setup</b>\n\n";
                text += $"My IP: {GetLocalIPAddress()}\n\n";
                text += "<color=#FFD700>[Y] Host</color> - Start server\n";
                text += "<color=#00FFFF>[X] Client</color> - Join server";
                break;

            case ConnectionState.EnteringIP:
                text = "<b>Join Server</b>\n\n";

                if (manualIPMode)
                {
                    text += "<color=#888888>Manual IP:</color>\n";
                    text += FormatIPWithSelection() + "\n\n";
                    text += "Stick: Select/Change\n";
                    text += "<size=80%>[Stick Click] Show discovered</size>\n";
                }
                else
                {
                    if (discoveredHosts.Count > 0)
                    {
                        text += "<color=#00FF88>Discovered Hosts:</color>\n";
                        lock (discoveredHosts)
                        {
                            for (int i = 0; i < discoveredHosts.Count; i++)
                            {
                                if (i == selectedHostIndex)
                                    text += $"<color=#FFD700>> {discoveredHosts[i]}</color>\n";
                                else
                                    text += $"  {discoveredHosts[i]}\n";
                            }
                        }
                        text += "\nStick Up/Down: Select\n";
                    }
                    else
                    {
                        text += "<color=#888888>Searching for hosts...</color>\n\n";
                    }
                    text += "<size=80%>[Stick Click] Manual IP</size>\n";
                }

                text += "\n<color=#00FFFF>[X] Connect</color>  <color=#FFD700>[Y] Back</color>";
                break;

            case ConnectionState.Connecting:
                text = "<b>Connecting...</b>\n\n";
                text += $"Target: {GetIPString()}\n";
                int dots = (int)(Time.time * 2) % 4;
                text += new string('.', dots) + "\n\n";
                text += "<color=#FFD700>[Y] Cancel</color>";
                break;

            case ConnectionState.Connected:
                if (NetworkManager.Singleton.IsHost)
                {
                    text = "<b><color=#00FF88>HOST</color></b>\n\n";
                    text += $"Server IP: {GetLocalIPAddress()}\n";
                    text += $"Clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}\n\n";
                }
                else
                {
                    text = "<b><color=#00FFFF>CLIENT</color></b>\n\n";
                    text += $"Connected to: {GetIPString()}\n\n";
                }
                text += "<color=#FFD700>[Y] Disconnect</color>\n";
                text += "<size=80%>Switch mode to start editing</size>";
                break;

            case ConnectionState.Failed:
                text = "<b><color=#FF6666>Connection Failed</color></b>\n\n";
                text += $"Could not connect to:\n{GetIPString()}\n\n";
                text += "<color=#FFD700>[Y] or [X] to go back</color>";
                break;

            default:
                text = "Initializing...";
                break;
        }

        statusText.text = text;
    }

    string FormatIPWithSelection()
    {
        string result = "";
        for (int i = 0; i < 4; i++)
        {
            if (i == selectedIPDigit)
                result += $"<color=#FFD700><b>[{ipOctets[i]:D3}]</b></color>";
            else
                result += $"{ipOctets[i]}";

            if (i < 3) result += ".";
        }
        return result;
    }

    string GetIPString()
    {
        return $"{ipOctets[0]}.{ipOctets[1]}.{ipOctets[2]}.{ipOctets[3]}";
    }

    void ParseIPString(string ip)
    {
        string[] parts = ip.Split('.');
        if (parts.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                if (int.TryParse(parts[i], out int val))
                    ipOctets[i] = Mathf.Clamp(val, 0, 255);
            }
        }
    }

    private void RegisterPrefabs()
    {
        Debug.Log("[NetworkConnect] Prefabs should already be registered via DefaultNetworkPrefabs asset");
    }

    /// <summary>
    /// Destroys all dynamically spawned NetworkObject clones (Node, Edge, Load) left over
    /// from a previous session. Without this, restarting as host throws a
    /// GlobalObjectIdHash collision in NetworkSceneManager.
    /// </summary>
    private void DestroyLeftoverNetworkObjects()
    {
        var leftover = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var netObj in leftover)
        {
            if (netObj == null) { continue; }
            // Only destroy prefab clones — scene-placed objects have "(Clone)" in their name
            // or are NodeBehaviour / EdgeBehaviour / LoadBehaviour components.
            bool isStructureObject = netObj.GetComponent<NodeBehaviour>() != null
                                  || netObj.GetComponent<EdgeBehaviour>()  != null
                                  || netObj.GetComponent<LoadBehaviour>()  != null;
            if (isStructureObject)
            {
                // DestroyImmediate so objects are gone before NetworkManager.StartHost()
                // scans the scene — prevents GlobalObjectIdHash collisions
                DestroyImmediate(netObj.gameObject);
                count++;
            }
        }
        if (count > 0)
        {
            Debug.Log($"[NetworkConnect] Cleaned up {count} leftover NetworkObject(s) before restarting.");
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}

public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
