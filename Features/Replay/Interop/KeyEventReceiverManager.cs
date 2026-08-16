using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace YqlossClientHarmony.Features.Replay.Interop;

public class KeyEventReceiverManager : IKeyEventReceiver
{
    private KeyEventReceiverManager()
    {
        Receivers.Add(new KeyboardSimulation());

        Main.Mod.Logger.Log("looking for ReplayInput classes");

        foreach (var modEntry in UnityModManager.modEntries)
            try
            {
                var assembly = modEntry.Assembly;
                if (assembly is null) continue;

                foreach (var type in assembly.GetTypes())
                    try
                    {
                        if (type.Name != "ReplayInput") continue;

                        Receivers.Add(new ReplayInputKeyEventReceiver(type));
                        Main.Mod.Logger.Log($"found ReplayInput class: {type} in {modEntry.Info.Id}");
                    }
                    catch (Exception exception)
                    {
                        Main.Mod.Logger.Warning($"found ReplayInput class that does not follow protocol: {type} in {modEntry.Info.Id}");
                        Main.Mod.Logger.Warning($"{exception}");
                    }
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Warning($"error while looking for ReplayInput in {modEntry.Info.Id}");
                Main.Mod.Logger.Warning($"{exception}");
            }
    }

    public static KeyEventReceiverManager Instance { get; } = new();

    private List<IKeyEventReceiver> Receivers { get; } = [];

    public void Begin()
    {
        try
        {
            foreach (var receiver in Receivers) receiver.Begin();
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Warning("error while beginning key event receiver");
            Main.Mod.Logger.Warning($"{exception}");
        }
    }

    public void End()
    {
        try
        {
            foreach (var receiver in Receivers) receiver.End();
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Warning("error while ending key event receiver");
            Main.Mod.Logger.Warning($"{exception}");
        }
    }

    public void OnKey(KeyCode code, bool isKeyDown)
    {
        try
        {
            foreach (var receiver in Receivers) receiver.OnKey(code, isKeyDown);
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Warning($"error while sending key {code} {isKeyDown} to key event receiver");
            Main.Mod.Logger.Warning($"{exception}");
        }
    }
}