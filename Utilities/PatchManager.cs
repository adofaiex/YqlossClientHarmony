using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using YqlossClientHarmony.Features.BlockUnintentionalEscape;
using YqlossClientHarmony.Features.FixKillerDecorationsInNoFail;
using YqlossClientHarmony.Features.LimitRecolorTrack;
using YqlossClientHarmony.Features.ModifyLoadingLevel;
using YqlossClientHarmony.Features.PlaySoundOnGameEnd;
using YqlossClientHarmony.Features.Replay;
using BlockInjections = YqlossClientHarmony.Features.BlockUnintentionalEscape.Injections;
using FixKillerInjections = YqlossClientHarmony.Features.FixKillerDecorationsInNoFail.Injections;
using LimitInjections = YqlossClientHarmony.Features.LimitRecolorTrack.Injections;
using ModifyLoadingLevelInjections = YqlossClientHarmony.Features.ModifyLoadingLevel.Injections;
using PlaySoundInjections = YqlossClientHarmony.Features.PlaySoundOnGameEnd.Injections;
using ReplayInjections = YqlossClientHarmony.Features.Replay.Injections;

namespace YqlossClientHarmony.Utilities;

// applies patches one by one instead of Harmony.PatchAll, so that one broken patch
// cannot keep the rest from being applied, and patches each feature only while it is enabled
public static class PatchManager
{
    public class Failure
    {
        public Type Type { get; set; } = null!;

        public Exception? Exception { get; set; }
    }

    private class PatchDef
    {
        public Type Type { get; set; } = null!;

        public Func<bool> Condition { get; set; } = null!;
    }

    private static readonly List<PatchDef> Definitions = [];

    private static readonly Dictionary<Type, bool> ActivePatches = [];

    private static readonly List<Failure> FailureList = [];

    public static IReadOnlyList<Failure> Failures => FailureList;

    static PatchManager()
    {
        Func<bool> replay = () => SettingsReplay.Instance.Enabled;
        Func<bool> modifyLoadingLevel = () => SettingsModifyLoadingLevel.Instance.Enabled;
        Func<bool> playSoundOnGameEnd = () => SettingsPlaySoundOnGameEnd.Instance.Enabled;
        Func<bool> blockUnintentionalEscape = () => SettingsBlockUnintentionalEscape.Instance.Enabled;
        Func<bool> limitRecolorTrack = () => SettingsLimitRecolorTrack.Instance.Enabled;
        Func<bool> fixKillerDecorationsInNoFail = () => SettingsFixKillerDecorationsInNoFail.Instance.Enabled;

        // replay
        Add(typeof(ReplayInjections.Inject_scnGame_Play), replay);
        Add(typeof(ReplayInjections.Inject_scrController_FailAction), replay);
        Add(typeof(ReplayInjections.Inject_scrController_QuitToMainMenu), replay);
        Add(typeof(ReplayInjections.Inject_scrController_OnLandOnPortal), replay);
        Add(typeof(ReplayInjections.Inject_scnEditor_ResetScene), replay);
        Add(typeof(ReplayInjections.Inject_scrMarginTracker_AddHit), replay);
        Add(typeof(ReplayInjections.Inject_scrPlanet_SwitchChosen), replay);
        Add(typeof(ReplayInjections.Inject_scrMisc_GetHitMargin), replay);
        Add(typeof(ReplayInjections.Inject_scrHitErrorMeter_AddHit), replay);
        Add(typeof(ReplayInjections.Inject_scrController_UpdateInput), replay);
        Add(typeof(ReplayInjections.Inject_scrController_PlayerControl_Update), replay);
        Add(typeof(ReplayInjections.Inject_Input_GetKey), replay);
        Add(typeof(ReplayInjections.Inject_Input_GetKeyDown), replay);
        Add(typeof(ReplayInjections.Inject_Input_GetKeyUp), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_ValidInputWasTriggered), replay);
        Add(typeof(ReplayInjections.Inject_LevelData_LoadLevel), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_Simulated_PlayerControl_Update), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_CheckPostHoldFail), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_Hit), replay);
        Add(typeof(ReplayInjections.Inject_AsyncInputManager_Setup), replay);
        Add(typeof(ReplayInjections.Inject_scrPlanet_AutoShouldHitNow), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_UpdateHoldKeys), replay);
        Add(typeof(ReplayInjections.Inject_RDInput_GetState), replay);
        Add(typeof(ReplayInjections.Inject_RDInput_GetMain), replay);
        Add(typeof(ReplayInjections.Inject_RDInput_GetStateKeys), replay);
        Add(typeof(ReplayInjections.Inject_scrController_ProcessKeyInputs), replay);
        Add(typeof(ReplayInjections.Inject_scnEditor_ClearAllFloorOffsets), replay);
        Add(typeof(ReplayInjections.Inject_scrPlayer_UpdateHoldBehavior), replay);

        // modify loading level
        Add(typeof(ModifyLoadingLevelInjections.Inject_LevelData_LoadLevel), modifyLoadingLevel);
        Add(typeof(ModifyLoadingLevelInjections.Inject_RDFile_ReadAllText), modifyLoadingLevel);
        Add(typeof(ModifyLoadingLevelInjections.Inject_scnEditor_SaveLevel), modifyLoadingLevel);
        Add(typeof(ModifyLoadingLevelInjections.Inject_scnEditor_SaveAndQuit), modifyLoadingLevel);

        // play sound on game end
        Add(typeof(PlaySoundInjections.Inject_scrController_OnLandOnPortal), playSoundOnGameEnd);
        Add(typeof(PlaySoundInjections.Inject_scrController_FailAction), playSoundOnGameEnd);
        Add(typeof(PlaySoundInjections.Inject_StateEngine_ChangeState), playSoundOnGameEnd);

        // other features
        Add(typeof(BlockInjections.Inject_scnEditor_SwitchToEditMode), blockUnintentionalEscape);
        Add(typeof(LimitInjections.Inject_ffxRecolorFloorPlus_Decode), limitRecolorTrack);
        Add(typeof(FixKillerInjections.Inject_scrDecoration_HitboxTriggerAction), fixKillerDecorationsInNoFail);
    }

    private static void Add(Type type, Func<bool> condition)
    {
        Definitions.Add(new PatchDef { Type = type, Condition = condition });
    }

    // applies every patch whose feature is enabled, and removes the others
    public static void PatchAll()
    {
        UpdateAllPatches(true);
    }

    // called when a setting changes, so that enabling or disabling a feature applies or removes its patches
    public static void UpdateAllPatches(bool log = false)
    {
        var changed = 0;

        foreach (var definition in Definitions)
            if (UpdatePatch(definition))
                ++changed;

        if (!log && changed == 0) return;

        var applied = Definitions.Count(
            definition => ActivePatches.GetValueOrDefault(definition.Type)
                          && FailureList.All(failure => failure.Type != definition.Type)
        );

        var skipped = Definitions.Count - applied - FailureList.Count;

        Main.Mod.Logger.Log($"patching: {applied} applied, {skipped} skipped, {FailureList.Count} failed (of {Definitions.Count} patch classes)");

        foreach (var definition in Definitions)
        {
            var name = definition.Type.FullName;

            if (FailureList.Any(failure => failure.Type == definition.Type))
                Main.Mod.Logger.Warning($"failed: {name}");
            else if (!ActivePatches.GetValueOrDefault(definition.Type))
                Main.Mod.Logger.Log($"skipped (feature disabled): {name}");
            else
                Main.Mod.Logger.Log($"applied: {name}");
        }
    }

    public static void UnpatchAll()
    {
        Main.Harmony.UnpatchAll(Main.Harmony.Id);
        ActivePatches.Clear();
        FailureList.Clear();
        Main.Mod.Logger.Log("unpatching");
    }

    // the text copied by the gui, so that it can be pasted into a bug report
    public static string GetFailureReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"YCH {Main.Mod.Info.Version} / ADOFAI {Application.version}");
        builder.AppendLine("patch failures:");

        foreach (var failure in FailureList)
        {
            builder.Append("- ").Append(failure.Type.FullName).Append(": ");

            if (failure.Exception is null) builder.AppendLine("no patch method was applied");
            else builder.AppendLine(failure.Exception.ToString());
        }

        return builder.ToString();
    }

    private static bool UpdatePatch(PatchDef definition)
    {
        var shouldBeActive = definition.Condition();
        var isActive = ActivePatches.GetValueOrDefault(definition.Type);
        if (isActive == shouldBeActive) return false;

        if (shouldBeActive) ApplyPatch(definition);
        else RemovePatch(definition.Type);

        // a failed patch is not retried until its condition changes again, otherwise every setting change would spam the log
        ActivePatches[definition.Type] = shouldBeActive;
        return true;
    }

    private static void ApplyPatch(PatchDef definition)
    {
        try
        {
            // Harmony throws when a target method cannot be found, and returns no patch method when nothing was applied
            var patchMethods = Main.Harmony.CreateClassProcessor(definition.Type).Patch();

            if (patchMethods is not { Count: > 0 })
            {
                RecordFailure(definition.Type, null);
                return;
            }

            ClearFailure(definition.Type);
        }
        catch (Exception exception)
        {
            RecordFailure(definition.Type, exception);
        }
    }

    private static void RemovePatch(Type type)
    {
        try
        {
            // a patch that failed to apply has nothing to remove, and resolving its target again may throw
            if (FailureList.All(failure => failure.Type != type))
                Main.Harmony.CreateClassProcessor(type).Unpatch();
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Error($"failed to remove patch {type.FullName}: {exception}");
        }

        ClearFailure(type);
    }

    private static void RecordFailure(Type type, Exception? exception)
    {
        ClearFailure(type);
        FailureList.Add(new Failure { Type = type, Exception = exception });
        Main.Mod.Logger.Error($"failed to apply patch {type.FullName}: {(exception is null ? "no patch method was applied" : exception.ToString())}");
    }

    private static void ClearFailure(Type type)
    {
        FailureList.RemoveAll(failure => failure.Type == type);
    }
}
