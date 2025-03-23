using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using REPO_MOD;
using Unity.VisualScripting;
using UnityEngine;

namespace MyRepoMod;

[BepInPlugin("Zaclin.PingMod", "PingMod", "0.01")]
public class MyRepoMod : BaseUnityPlugin
{
    internal static MyRepoMod Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private float pingLifetime = 5f;
    private GameObject pingObject;

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
        if (Input.GetKeyDown(KeyCode.P))
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
            if(pingObject != null)
            {
                GameObject ping = Instantiate(pingObject, hit.point, Quaternion.identity);
                ping.AddComponent<PingObject>();
                Destroy(ping, pingLifetime);
            }
            else
            {
                pingObject = Instantiate(new GameObject("Ping Object"));
                GameObject ping = Instantiate(pingObject, hit.point, Quaternion.identity);
                ping.AddComponent<PingObject>();
                Destroy(ping, pingLifetime);
            }
        }
    }
}