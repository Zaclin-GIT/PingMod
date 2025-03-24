﻿using System;
using System.Collections.Generic;
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
    private readonly Queue<Vector3> _pingQueue = new Queue<Vector3>();

    private void Awake()
    {
        Instance = this;

        PingEvent = new NetworkedEvent("Global Ping Event", this.EmitPing);
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        this.cachedPingSprite = CreateLocationSprite();

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    private void Start()
    {
        this.overlayCameraObj = new GameObject("OverlayCameraObject");
        this.CreateOverlayCamera();
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
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            this.AttemptPing();
        }
    }


    void LateUpdate()
    {
        if (_pingQueue.Count > 0)
        {

            lock (_pingQueue)
            {
                while (_pingQueue.Count > 0)
                {
                    Vector3 position = _pingQueue.Dequeue();

                    GameObject canvasObj = new GameObject("PingObject");
                    canvasObj.layer = this.pingOverlayLayer;
                    canvasObj.transform.position = position;
                    this.CreatePing(canvasObj);
                    Destroy(canvasObj, pingLifetime);
                }
            }
        }
    }
    private bool IsURP()
    {
        return Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime") != null;
    }

    private GameObject CreateOverlayCamera()
    {
        // GameObject camObj = new GameObject("PingOverlayCamera");

        Camera overlayCamera = this.overlayCameraObj.AddComponent<Camera>();
        overlayCamera.transform.SetParent(Camera.main.transform);

        overlayCamera.clearFlags = CameraClearFlags.Depth; // Prevents background clearing
        overlayCamera.cullingMask = 1 << this.pingOverlayLayer; // Render ONLY the ping layer
        overlayCamera.depth = Camera.main.depth + 1; // Renders above the main camera
        overlayCamera.allowHDR = false;
        overlayCamera.allowMSAA = false;

        // Ensure main camera does NOT render the Ping Layer
        Camera.main.cullingMask &= ~(1 << this.pingOverlayLayer);

        Logger.LogInfo($"Overlay Camera created with depth {overlayCamera.depth}. Ping Layer: {this.pingOverlayLayer}");
        return camObj;
    }

    private void CreatePing(GameObject canvasObj)
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
        image.sprite = this.cachedPingSprite;//CreateLocationSprite(); // Generate a location icon
        image.transform.position = canvasObj.transform.position;
        image.rectTransform.sizeDelta = imageSize;

        // Billboard effect
        canvasObj.AddComponent<Billboard>();
    }







    public Sprite CreateLocationSprite()
    {
        int width = this.PingResolution;
        int height = this.PingResolution;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color fillColor = Color.white;

        // Clear texture
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = transparent;
        texture.SetPixels(clearPixels);

        // Define Circle Parameters
        int centerX = width / 2;
        int centerY = (int)(height * 0.65f);
        int outerRadius = (int)(width * 0.25f);
        int innerRadius = (int)(outerRadius * 0.4f); // The hole size

        // Circle Drawing
        int minX = centerX - outerRadius;
        int maxX = centerX + outerRadius;
        int minY = centerY - outerRadius;
        int maxY = centerY + outerRadius;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distSq = dx * dx + dy * dy;

                if (distSq <= outerRadius * outerRadius && distSq > innerRadius * innerRadius)
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }
        }

        // Define Tail Parameters
        int tailWidth = (int)(width * 0.2f);
        int tailHeight = (int)(height * 0.3f);
        int tailBottomY = (int)(height * 0.15f);
        int tailTopY = (int)(height * 0.55f);

        //Tail Drawing
        int tailMinX = centerX - tailWidth / 2;
        int tailMaxX = centerX + tailWidth / 2;

        for (int y = tailBottomY; y < tailTopY; y++)
        {
            float slope = (float)(y - tailBottomY) / (tailTopY - tailBottomY);
            int minXAtY = (int)(centerX - (slope * (tailWidth / 2)));
            int maxXAtY = (int)(centerX + (slope * (tailWidth / 2)));

            for (int x = minXAtY; x <= maxXAtY; x++)
            {
                texture.SetPixel(x, y, fillColor);
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

        Vector3 position = (Vector3)eventData.CustomData;


        lock (_pingQueue)
        {
            _pingQueue.Enqueue(position);
        }
    }


    void AttemptPing()
    {
        Camera cam = Camera.main;
        if (cam == null) return;


        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        int layerMask = LayerMask.GetMask("Default", "PhysGrabObjectCart", "PhysGrabObjectHinge", "PhysGrabObject");

        float maxDistance = 75f;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            Vector3 spawnPosition = hit.point + hit.normal * 0.14f;
            if (PingEvent == null)
                Logger.LogError("Ping Event Network Event is null");
            else
                PingEvent.RaiseEvent(spawnPosition, REPOLib.Modules.NetworkingEvents.RaiseAll, SendOptions.SendReliable);
        }
        else
        {
            Logger.LogWarning("Ping raycast did not hit anything.");
        }
    }
}
