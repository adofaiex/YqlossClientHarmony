using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using YqlossClientHarmony.Features.Replay;
using YqlossClientHarmony.Gui;

namespace YqlossClientHarmony;

public static class Main
{
    private static UnityModManager.ModEntry? _mod;

    private static Settings? _settings;

    private static bool _enabled;

    public static UnityModManager.ModEntry Mod
    {
        get => _mod ?? throw new NullReferenceException("use Main.Mod before the mod is initialized");
        private set
        {
            if (ReferenceEquals(_mod, value)) return;
            _mod = value;

            _mod.OnToggle += (_, enabled) =>
            {
                Enabled = enabled;
                return true;
            };

            _mod.OnGUI += _ => GuiManager.Draw();

            _mod.OnSaveGUI += mod => Settings.Save(mod);

            _mod.OnUpdate += OnUpdate;
        }
    }

    public static Settings Settings
    {
        get => _settings ?? throw new NullReferenceException("use Main.Settings before the mod is initialized");
        private set
        {
            if (ReferenceEquals(_settings, value)) return;
            _settings = value;

            _settings.OnSettingChange += OnSettingChange;
        }
    }

    public static bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            OnToggle(value);
        }
    }

    public static Harmony Harmony { get; } = new("YCH");

    private static void InitializeMod(UnityModManager.ModEntry mod)
    {
        if (_mod is not null) return;
        Mod = mod;
        Settings = UnityModManager.ModSettings.Load<Settings>(Mod);
        Enabled = mod.Enabled;
        Settings.OnLoad(mod);
    }

    public static void Load(UnityModManager.ModEntry mod)
    {
        InitializeMod(mod);

        var dialogRenderer = new GameObject("YCH Dialog Renderer");
        UnityEngine.Object.DontDestroyOnLoad(dialogRenderer);
        dialogRenderer.AddComponent<DialogRenderer>();
    }

    private static void OnToggle(bool enabled)
    {
        if (enabled)
        {
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Mod.Logger.Log("patching");
        }
        else
        {
            Harmony.UnpatchAll(Harmony.Id);
            Mod.Logger.Log("unpatching");
        }
    }

    private static void OnSettingChange()
    {
    }

    private static void OnUpdate(UnityModManager.ModEntry mod, float _)
    {
        try
        {
            ReplayUnityModManagerEventHandlers.OnUpdate();
        }
        catch
        {
            // ignored
        }
    }
}