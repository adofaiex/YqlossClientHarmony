using System;
using HarmonyLib;
using UnityModManagerNet;

namespace YqlossClientHarmony.Features.Replay.Interop;

public class CompatibilityOverlayer
{
    static CompatibilityOverlayer()
    {
        try
        {
            Instance = new CompatibilityOverlayer();
        }
        catch (Exception exception)
        {
            Instance = null;
            Main.Mod.Logger.Warning($"unable to load Overlayer compatibility {exception}");
        }
    }

    private CompatibilityOverlayer()
    {
        Main.Mod.Logger.Log("loading Overlayer compatibility");

        foreach (var modEntry in UnityModManager.modEntries)
        {
            if (modEntry.Info.Id != "Overlayer") continue;

            var assembly = modEntry.Assembly;
            if (assembly is null) continue;

            var typeJudgementTagPatch = assembly.GetType("Overlayer.Tags.Patches.HitPatch+JudgementTagPatch")!;

            Main.Harmony.Patch(
                AccessTools.DeclaredMethod(typeJudgementTagPatch, "IncreaseCCount"),
                new HarmonyMethod(typeof(CompatibilityOverlayer).GetMethod(nameof(Inject_JudgementTagPatch_IncreaseCCount_Prefix)))
            );

            var typeHit = assembly.GetType("Overlayer.Tags.Hit")!;

            CurrentRef = AccessTools.StaticFieldRefAccess<HitMargin>(
                AccessTools.DeclaredField(typeHit, "Current")
            );

            Main.Mod.Logger.Log("loaded Overlayer compatibility");
            return;
        }

        throw new Exception("Overlayer is not installed");
    }

    private AccessTools.FieldRef<HitMargin> CurrentRef { get; }

    public static CompatibilityOverlayer? Instance { get; }

    public static void Inject_JudgementTagPatch_IncreaseCCount_Prefix(
        ref HitMargin hit
    )
    {
        var instance = Instance;
        if (instance is null) return;
        if (!ReplayPlayer.PlayingReplay) return;

        ReplayPlayer.OnGetHitMargin(ref hit);
        instance.CurrentRef() = hit;
    }
}