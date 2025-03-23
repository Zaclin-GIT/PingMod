using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using REPO_MOD;
using UnityEngine;

namespace MyRepoMod;

[BepInPlugin("Zaclin.PingMod", "PingMod", "0.01")]
public class MyRepoMod : BaseUnityPlugin
{
    internal static MyRepoMod Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    public GameObject pingPrefab; // Assign in Unity inspector or load dynamically
    private float pingLifetime = 5f;

    private void Awake()
    {
        Instance = this;
        
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) // Change key as needed
        {
            CreatePing();
        }
    }

    void CreatePing()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject ping = Instantiate(new GameObject(), hit.point, Quaternion.identity);
            Debug.Log("Hit Position: " + hit.point);
            ping.AddComponent<PingObject>();
            Destroy(ping, pingLifetime);
        }
    }
}