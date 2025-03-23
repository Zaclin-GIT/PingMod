﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Unity.VisualScripting;
using UnityEngine;
using Unity;

namespace PingMod;

[BepInPlugin("Zaclin.PingMod", "PingMod", "0.01")]
public class PingMod : BaseUnityPlugin
{
    internal static PingMod Instance { get; private set; } = null!;
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
        //Walls - Cart - Doors
        int layerMask = LayerMask.GetMask("Default", "PhysGrabObjectCart", "PhysGrabObjectHinge", "PhysGrabObject");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {

            Vector3 spawnPosition = hit.point + hit.normal * 0.1f;
            if (pingObject != null)
            {
                GameObject ping = Instantiate(pingObject, spawnPosition, Quaternion.identity);
                ping.AddComponent<PingObject>();
                Destroy(ping, pingLifetime);
            }
            else
            {
                pingObject = Instantiate(new GameObject("Ping Object"));
                GameObject ping = Instantiate(pingObject, spawnPosition, Quaternion.identity);
                ping.AddComponent<PingObject>();
                Destroy(ping, pingLifetime);
            }
        }
    }
}
