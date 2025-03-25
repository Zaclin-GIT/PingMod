using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using REPOLib.Modules;

namespace PingMod;

[BepInPlugin("Zaclin.PingMod", "PingMod", "0.01")]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class PingMod : BaseUnityPlugin
{
    internal static PingMod Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private float pingLifetime = 10f;
    public Vector2 imageSize = new Vector2(0.25f, 0.25f);
    public int PingResolution = 1024;
    public Sprite? cachedPingSprite;
    public static NetworkedEvent? PingEvent;
    private int pingOverlayLayer = 31;
    private GameObject? overlayCameraObj;
    private readonly float pingCoolDown = 1f;
    private bool onCoolDown = false;
    public Color pingColor = Color.white;


    private readonly Queue<PingData> _pingQueue = new Queue<PingData>();

    private void Awake()
    {
        Instance = this;
        PlayerPatcher.plugin = Instance;

        if (!PhotonPeer.RegisterType(typeof(PingData), 0xF1, SerializePingData, DeserializePingData))
        {
            Logger.LogError("Failed to register PingData type!");
        }


        PingEvent = new NetworkedEvent("Global Ping Event", this.EmitPing);
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        this.cachedPingSprite = CreateLocationSprite();

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }


    public static byte[] SerializePingData(object customobject)
    {
        PingData ping = (PingData)customobject;
        // 3 floats for position (3*4 = 12 bytes), 4 floats for color (4*4 = 16 bytes): total 28 bytes
        byte[] bytes = new byte[28];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(ping.position.x), 0, bytes, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(ping.position.y), 0, bytes, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(ping.position.z), 0, bytes, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(ping.color.r), 0, bytes, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(ping.color.g), 0, bytes, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(ping.color.b), 0, bytes, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(ping.color.a), 0, bytes, offset, 4); offset += 4;

        return bytes;
    }

    public static object DeserializePingData(byte[] data)
    {
        PingData ping = new PingData();
        int offset = 0;

        ping.position.x = BitConverter.ToSingle(data, offset); offset += 4;
        ping.position.y = BitConverter.ToSingle(data, offset); offset += 4;
        ping.position.z = BitConverter.ToSingle(data, offset); offset += 4;

        ping.color.r = BitConverter.ToSingle(data, offset); offset += 4;
        ping.color.g = BitConverter.ToSingle(data, offset); offset += 4;
        ping.color.b = BitConverter.ToSingle(data, offset); offset += 4;
        ping.color.a = BitConverter.ToSingle(data, offset); offset += 4;

        return ping;
    }


    private void Start()
    {
    }

    internal void Patch()
    {
        this.Harmony ??= new Harmony(Info.Metadata.GUID);
        this.Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        this.Harmony?.UnpatchSelf();
    }

    void Update()
    {
        // Logger.LogInfo($"HotKey Clicked: {Input.GetKeyDown(KeyCode.Mouse2)} | Player Health Greater than 0: {PlayerPatcher.localPlayer.playerHealth.health > 0} | CoolDown over: {(DateTime.UtcNow.Second - this.CoolDownStart) > this.pingCoolDown}");
        if (Input.GetKeyDown(KeyCode.Mouse2) && PlayerPatcher.localPlayer.playerHealth.health > 0 && !this.onCoolDown)
        {
            if (this.AttemptPing())
            {
                this.onCoolDown = true;
                Task.Delay(TimeSpan.FromSeconds(this.pingCoolDown)).ContinueWith(task => this.onCoolDown = false);
            }
        }
    }


    void LateUpdate()
    {
        if (this.overlayCameraObj == null)
        {
            // this.overlayCameraObj = new GameObject("OverlayCameraObject");
            this.overlayCameraObj = new GameObject("OverlayCameraObject");
            this.CreateOverlayCamera();
        }
        if (_pingQueue.Count > 0)
        {
            lock (_pingQueue)
            {
                while (_pingQueue.Count > 0)
                {
                    PingData data = _pingQueue.Dequeue();

                    GameObject canvasObj = new GameObject("PingObject");
                    canvasObj.layer = this.pingOverlayLayer;
                    canvasObj.transform.position = data.position;
                    this.CreatePing(canvasObj, data.color);
                    Destroy(canvasObj, pingLifetime);
                }
            }
        }
    }

    private void CreateOverlayCamera()
    {
        Camera overlayCamera = this.overlayCameraObj.AddComponent<Camera>();

        overlayCamera.CopyFrom(Camera.main);

        this.overlayCameraObj.transform.position = Camera.main.transform.position;
        this.overlayCameraObj.transform.rotation = Camera.main.transform.rotation;
        overlayCamera.transform.SetParent(Camera.main.transform);

        overlayCamera.clearFlags = CameraClearFlags.Depth; // Prevents background clearing
        overlayCamera.cullingMask = (1 << this.pingOverlayLayer); // Render ONLY the ping layer
        overlayCamera.depth = 10; // Renders above the main camera
        overlayCamera.allowHDR = false;
        overlayCamera.allowMSAA = false;

        // Ensure main camera does NOT render the Ping Layer
        Camera.main.cullingMask &= ~(1 << this.pingOverlayLayer);
    }

    private void CreatePing(GameObject canvasObj, Color color)
    {
        // Add Canvas
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create an Image object
        GameObject imageObj = new GameObject("PingImage");
        imageObj.transform.SetParent(canvasObj.transform);

        // Add Image component and assign generated sprite
        UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
        image.sprite = this.LocationSpriteCopy(color);//CreateLocationSprite(); // Generate a location icon
        image.transform.position = canvasObj.transform.position;
        image.rectTransform.sizeDelta = imageSize;

        // Billboard effect
        canvasObj.AddComponent<Billboard>();
    }


    public Sprite LocationSpriteCopy(Color newColor)
    {
        int width = this.cachedPingSprite.texture.width;
        int height = this.cachedPingSprite.texture.height;
        Texture2D texture = new Texture2D(width, height); //cachedPingSprite.texture; // Get the texture from the sprite
        Color[] pixels = this.cachedPingSprite.texture.GetPixels(); // Get all pixels

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0 && pixels[i] != Color.white) // Only change non-transparent pixels
            {
                pixels[i] = newColor;
            }
        }

        texture.SetPixels(pixels); // Apply new colors
        texture.Apply(); // Update texture on GPU
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    public Sprite CreateLocationSprite()
    {
        int width = this.PingResolution;
        int height = this.PingResolution;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color fillColor = this.pingColor;
        Color outlineColor = Color.white;

        // Clear texture
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = transparent;
        texture.SetPixels(clearPixels);

        // Define Circle Parameters
        int centerX = width / 2;
        int centerY = (int)(height * 0.65f);
        int outerRadius = (int)(width * 0.25f);
        int innerRadius = (int)(outerRadius * 0.4f);
        int outlineThickness = 10;

        // Draw Circle with Outline
        for (int x = centerX - outerRadius - outlineThickness; x <= centerX + outerRadius + outlineThickness; x++)
        {
            for (int y = centerY - outerRadius - outlineThickness; y <= centerY + outerRadius + outlineThickness; y++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distSq = dx * dx + dy * dy;

                if (distSq <= (outerRadius + outlineThickness) * (outerRadius + outlineThickness) &&
                    distSq > (outerRadius - outlineThickness) * (outerRadius - outlineThickness))
                {
                    texture.SetPixel(x, y, outlineColor);
                }
                else if (distSq <= outerRadius * outerRadius && distSq > innerRadius * innerRadius)
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }
        }

        // Define Tail Parameters
        int tailWidth = (int)(width * 0.2f);
        int tailHeight = (int)(height * 0.3f);
        int tailBottomY = (int)(height * 0.15f);
        // int tailTopY = centerY - outerRadius + 1; // The point where the tail meets the circle
        int tailTopY = (int)(height * 0.55f);

        // Tail Drawing with Outline (Stopping at the Circle)
        for (int y = tailBottomY - outlineThickness; y < tailTopY; y++)
        {
            float slope = (float)(y - tailBottomY) / (tailTopY - tailBottomY);
            int minXAtY = (int)(centerX - (slope * (tailWidth / 2)));
            int maxXAtY = (int)(centerX + (slope * (tailWidth / 2)));

            int outlineMinX = minXAtY - outlineThickness;
            int outlineMaxX = maxXAtY + outlineThickness;

            for (int x = outlineMinX; x <= outlineMaxX; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distSq = dx * dx + dy * dy;

                bool insideCircle = distSq <= outerRadius * outerRadius;
                bool isOutline = (x < minXAtY || x > maxXAtY) && y < tailTopY - outlineThickness;

                if (isOutline && !insideCircle)
                {
                    texture.SetPixel(x, y, outlineColor); // Outline slanted edges + bottom (stopping at the circle)
                }
                else if (x >= minXAtY && x <= maxXAtY)
                {
                    texture.SetPixel(x, y, fillColor); // Fill the tail
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }


    public class Billboard : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 0, 10);
        private Camera? mainCamera;
        private void Awake()
        {
            this.mainCamera = Camera.main;
        }
        private void LateUpdate()
        {
            if (this.mainCamera != null)
            {
                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                directionToCamera.y = 0; // Keep upright if needed (optional)
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }

    private void EmitPing(EventData eventData)
    {
        if (eventData.CustomData == null)
        {
            Logger.LogError("Received Ping event with null data.");
            return;
        }

        PingData data = (PingData)eventData.CustomData;

        lock (_pingQueue)
        {
            _pingQueue.Enqueue(data);
        }
    }


    private bool AttemptPing()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;


        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        int layerMask = LayerMask.GetMask("Default", "PhysGrabObjectCart", "PhysGrabObjectHinge", "PhysGrabObject");

        float maxDistance = 25f;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            Vector3 spawnPosition = hit.point + hit.normal * 0.14f;
            if (PingEvent == null)
                Logger.LogError("Ping Event Network Event is null");
            else
                PingEvent.RaiseEvent(new PingData(spawnPosition, this.pingColor), REPOLib.Modules.NetworkingEvents.RaiseAll, SendOptions.SendReliable);
            return true;
        }
        else
        {
            Logger.LogWarning("Ping raycast did not hit anything.");
        }
        return false;
    }


    [HarmonyPatch]
    public static class PlayerPatcher
    {
        internal static PlayerAvatar localPlayer;
        internal static PingMod plugin;



        [HarmonyPatch(typeof(PlayerAvatar), "Awake")]
        [HarmonyPostfix]
        public static void InitPlayer(ref bool ___isLocal, PlayerAvatar __instance)
        {
            if (___isLocal)
            {
                Logger.LogInfo("FOUND LOCAL PLAYER");
                localPlayer = __instance;
            }
        }

        [HarmonyPatch(typeof(RoundDirector), "StartRoundLogic")]
        [HarmonyPrefix]
        public static void BeforeStartOfRound(int value, RoundDirector __instance)
        {

        }

        [HarmonyPatch(typeof(PlayerAvatar), "SetColorRPC")]
        [HarmonyPostfix]
        public static void SetColor(int colorIndex, ref bool ___isLocal, PlayerAvatar __instance)
        {
            if (___isLocal)
            {
                plugin.pingColor = __instance.playerAvatarVisuals.color;
                // plugin.cachedPingSprite = plugin.CreateLocationSprite();
            }
        }
    }


    [System.Serializable]
    public class PingData
    {
        public Vector3 position;
        public Color color;

        public PingData(Vector3 pos, Color color)
        {
            this.position = pos;
            this.color = color;
        }

        // Parameterless constructor needed for deserialization
        public PingData() { }
    }

}







