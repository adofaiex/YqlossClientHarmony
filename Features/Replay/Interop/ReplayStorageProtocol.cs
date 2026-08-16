using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityModManagerNet;

namespace YqlossClientHarmony.Features.Replay.Interop;

public static class ReplayStorageProtocol
{
    /*
     * // How to implement the protocol:
     * // replayer refers to the replay mod's id, like "YCH"
     * // it is possible that one replayer is recording while
     * // another one is replaying, so using a hash map is recommended
     * public static class ReplayStoragePluginV1
     * {
     *     // returns your unique namespace
     *     // must return the same value for the same replayer
     *     public static string GetNamespace(string replayer);
     *
     *     public static void OnStartRecording(string replayer, int tileId);
     *
     *     // returns your data to store in the replay
     *     // or null if you don't want to store anything
     *     public static byte[]? OnStopRecording(string replayer);
     *
     *     public static void OnLoadReplay(string replayer, byte[]? data);
     *
     *     public static void OnUnloadReplay(string replayer);
     *
     *     public static void OnStartReplaying(string replayer, int tileId);
     *
     *     public static void OnStopReplaying(string replayer);
     * }
     */

    public const string Replayer = "YCH";

    static ReplayStorageProtocol()
    {
        foreach (var modEntry in UnityModManager.modEntries)
            try
            {
                var assembly = modEntry.Assembly;
                if (assembly is null) continue;

                foreach (var type in assembly.GetTypes())
                    try
                    {
                        if (type.Name != "ReplayStoragePluginV1") continue;

                        Plugins.Add(new Plugin(type));
                        Main.Mod.Logger.Log($"found ReplayStoragePluginV1 class: {type} in {modEntry.Info.Id}");
                    }
                    catch (Exception exception)
                    {
                        Main.Mod.Logger.Warning($"found ReplayStoragePluginV1 class that does not follow protocol: {type} in {modEntry.Info.Id}");
                        Main.Mod.Logger.Warning($"{exception}");
                    }
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error while looking for ReplayStoragePluginV1 in {modEntry.Info.Id}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    private static List<Plugin> Plugins { get; } = [];


    public static void OnStartRecording(int tileId)
    {
        foreach (var plugin in Plugins)
            try
            {
                plugin.OnStartRecording(Replayer, tileId);
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnStartRecording of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    public static Dictionary<string, byte[]> OnStopRecording()
    {
        var customData = new Dictionary<string, byte[]>();

        foreach (var plugin in Plugins)
            try
            {
                var data = plugin.OnStopRecording(Replayer);
                if (data is not null) customData[plugin.Namespace] = data;
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnStopRecording of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }

        return customData;
    }

    public static void OnLoadReplay(Dictionary<string, byte[]> data)
    {
        foreach (var plugin in Plugins)
            try
            {
                plugin.OnLoadReplay(Replayer, data.GetValueOrDefault(plugin.Namespace));
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnLoadReplay of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    public static void OnUnloadReplay()
    {
        foreach (var plugin in Plugins)
            try
            {
                plugin.OnUnloadReplay(Replayer);
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnUnloadReplay of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    public static void OnStartReplaying(int tileId)
    {
        foreach (var plugin in Plugins)
            try
            {
                plugin.OnStartReplaying(Replayer, tileId);
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnStartReplaying of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    public static void OnStopReplaying()
    {
        foreach (var plugin in Plugins)
            try
            {
                plugin.OnStopReplaying(Replayer);
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error in OnStopReplaying of {plugin.Namespace}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    private class Plugin(Type type)
    {
        public string Namespace { get; } =
            AccessTools.MethodDelegate<Func<string, string>>(AccessTools.DeclaredMethod(type, "GetNamespace"))(Replayer);

        public Action<string, int> OnStartRecording { get; } =
            AccessTools.MethodDelegate<Action<string, int>>(AccessTools.DeclaredMethod(type, "OnStartRecording"));

        public Func<string, byte[]?> OnStopRecording { get; } =
            AccessTools.MethodDelegate<Func<string, byte[]?>>(AccessTools.DeclaredMethod(type, "OnStopRecording"));

        public Action<string, byte[]?> OnLoadReplay { get; } =
            AccessTools.MethodDelegate<Action<string, byte[]?>>(AccessTools.DeclaredMethod(type, "OnLoadReplay"));

        public Action<string> OnUnloadReplay { get; } =
            AccessTools.MethodDelegate<Action<string>>(AccessTools.DeclaredMethod(type, "OnUnloadReplay"));

        public Action<string, int> OnStartReplaying { get; } =
            AccessTools.MethodDelegate<Action<string, int>>(AccessTools.DeclaredMethod(type, "OnStartReplaying"));

        public Action<string> OnStopReplaying { get; } =
            AccessTools.MethodDelegate<Action<string>>(AccessTools.DeclaredMethod(type, "OnStopReplaying"));
    }
}