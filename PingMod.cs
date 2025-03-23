using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Unity;
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

    private float pingLifetime = 1200f;
    private GameObject pingObjectPrefab;
    private GameObject pingObject;
    public static NetworkedEvent PingEvent;
    private AssetBundle pingAsset;
    public Vector2 imageSize = new Vector2(100, 100);

    private void Awake()
    {
        Instance = this;

        PingEvent = new NetworkedEvent("Global Ping Event", this.EmitPing);
        this.LoadBundle();
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }


    private void LoadBundle()
    {
        string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "pingmodassetbundle");
        this.pingAsset = AssetBundle.LoadFromFile(text);
        this.pingObjectPrefab = pingAsset.LoadAsset<GameObject>("PingImagePrefab");
        Instantiate(this.pingObjectPrefab);
        // GameObject val3 = val.LoadAsset<GameObject>("Assets/REPO/Mods/plugins/FancyWaterBottle.prefab");
        // GameObject val4 = val.LoadAsset<GameObject>("Assets/REPO/Mods/plugins/InsulatedWaterBottle.prefab");
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



    private GameObject InitializeCanvas(Vector3 pos)
    {
        // Create a new Canvas
        GameObject canvasObj = new GameObject("WorldSpaceCanvas");

        // canvasObj.transform.SetParent(ping.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject pingObj = Instantiate(this.pingObjectPrefab, pos, Quaternion.identity);
        pingObj.transform.SetParent(canvasObj.transform);

        PingMod.Billboard billboard = canvasObj.AddComponent<PingMod.Billboard>();

        return canvasObj;

        // Set the canvas position and rotation
        // canvasObj.transform.position = ping.transform.position;

        // Create an Image object as a child of the canvas

        // GameObject imageObj = Instantiate(this.pingObject.gameObject);
        // imageObj.transform.SetParent(canvasObj.transform);



        // Add the Image component and set its sprite
        // UnityEngine.UI.Image image = imageObj.GetComponent<UnityEngine.UI.Image>();
        // image.sprite = CreateWhiteSprite(1000, 1000);
        // image.rectTransform.sizeDelta = imageSize;


        // Adjust the image position and size within the canvas
        // RectTransform rectTransform = image.GetComponent<RectTransform>();
        // rectTransform.localPosition = Vector3.zero;
        // rectTransform.localRotation = Quaternion.identity;
        // rectTransform.localScale = Vector3.one * 0.01f;  // Scale down for world space
        //

    }


    public Sprite CreateWhiteSprite(int width, int height)
    {
        // Create a new blank texture
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Create a new sprite from the texture
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }


    public class Billboard : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0, 0, 10);
        private Camera mainCamera;
        private void Awake()
        {
            this.mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            bool flag = this.mainCamera != null;
            if (flag)
            {
                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                directionToCamera.y = 0; // Keep upright if needed (optional)

                transform.rotation = Quaternion.LookRotation(directionToCamera);

                // Maintain offset from parent
                // transform.position = transform.parent != null ? transform.parent.position + offset : transform.position + offset;
                base.transform.position = base.transform.parent.position;
            }
        }
    }


    private void EmitPing(EventData eventData)
    {
        Vector3 position = (Vector3)eventData.CustomData;
        /*
         * Add Logic below
         */

        GameObject canvas = this.InitializeCanvas(position);


        /*
         * End of logic
         */
        Destroy(canvas, pingLifetime);
    }


    void AttemptPing()
    {

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        // NOTE: Walls - Cart - Doors
        int layerMask = LayerMask.GetMask("Default", "PhysGrabObjectCart", "PhysGrabObjectHinge", "PhysGrabObject");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            Vector3 spawnPosition = hit.point + hit.normal * 0.1f;
            PingEvent.RaiseEvent(spawnPosition, REPOLib.Modules.NetworkingEvents.RaiseAll, SendOptions.SendReliable);
        }
    }
}
