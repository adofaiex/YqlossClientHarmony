using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ADOFAI;
using HarmonyLib;
using MonsterLove.StateMachine;
using SkyHook;
using UnityEngine;
using EventType = SkyHook.EventType;

namespace YqlossClientHarmony.Features.Replay;

public static class Injections
{
    private static bool IsInSwitchChosen { get; set; }

    private static bool IsInUpdateHoldBehavior { get; set; }

    private static ConcurrentQueue<SkyHookEvent> KeyQueue { get; } = [];

    public static double TickToDsp(long ticks)
    {
        return (ticks + ((long)(AudioSettings.dspTime * 10000000.0) - DateTime.Now.Ticks)) / 10000000.0;
    }

    public static double DspToSong(double dsp, double offset)
    {
        var conductor = Adofai.Conductor;

        return conductor.song.pitch * (
                   dsp
                   - conductor.dspTimeSongPosZero
                   - scrConductor.calibration_i
                   + offset
               )
               - conductor.adjustedCountdownTicks * conductor.crotchetAtStart
               + conductor.addoffset;
    }

    [HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
    public static class Inject_scnGame_Play
    {
        public static void Prefix(
            int seqID
        )
        {
            if (ADOBase.isOfficialLevel) return;
            if (RDC.auto)
            {
                Main.Mod.Logger.Log("skipping recording and playing replay: auto mode");
                return;
            }

            ReplayPlayer.StartPlaying(seqID);
            if (!SettingsReplay.Instance.Enabled) return;
            ReplayRecorder.StartRecording(seqID);
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.FailAction))]
    public static class Inject_scrController_FailAction
    {
        public static void Prefix(
            scrController __instance,
            bool hitbox = false
        )
        {
            if (!__instance.gameworld || ADOBase.isOfficialLevel) return;

            if (
                !hitbox && (
                    RDC.auto ||
                    (
                        __instance.currFloor.nextfloor != null &&
                        __instance.currFloor.nextfloor.auto &&
                        !RDC.useOldAuto
                    ) ||
                    (!__instance.gameworld && !__instance.currFloor.freeroam) ||
                    __instance.noFail ||
                    (__instance.currFloor.isSafe && GCS.hitMarginLimit == HitMarginLimit.None)
                )
            ) return;

            ReplayRecorder.EndRecording();
            ReplayPlayer.EndPlaying();
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.QuitToMainMenu))]
    public static class Inject_scrController_QuitToMainMenu
    {
        public static void Prefix()
        {
            ReplayPlayer.UnloadReplay();
            ReplayRecorder.EndRecording();
            ReplayPlayer.EndPlaying();
            ReplayPlayer.ResetTrailingAnimation();
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.OnLandOnPortal))]
    public static class Inject_scrController_OnLandOnPortal
    {
        public static void Prefix()
        {
            ReplayRecorder.EndRecording();
            ReplayPlayer.EndPlaying();
        }

        public static void Postfix(
            scrController __instance
        )
        {
            var now = DateTime.Now;
            if (now.Month != 7 || now.Day != 27) return;
            __instance.txtAllStrictClear.text = "727 WYSI!";
        }
    }

    [HarmonyPatch(typeof(scnEditor), "ResetScene")]
    public static class Inject_scnEditor_ResetScene
    {
        public static void Prefix()
        {
            ReplayRecorder.EndRecording();
            ReplayPlayer.EndPlaying();
            ReplayPlayer.ResetTrailingAnimation();
        }
    }

    [HarmonyPatch(typeof(scrMarginTracker), nameof(scrMarginTracker.AddHit))]
    public static class Inject_scrMarginTracker_AddHit
    {
        public static void Prefix(
            ref HitMargin hit
        )
        {
            if (Interoperation.ReplayIgnoreJudgement) return;

            if (ReplayRecorder.Replay is not null)
                ReplayRecorder.OnHitMargin(IsInUpdateHoldBehavior && hit == HitMargin.FailMiss ? ReplayConstants.HoldPreMiss : hit);

            if (ReplayPlayer.PlayingReplay) ReplayPlayer.OnHitMargin(ref hit);
        }
    }

    [HarmonyPatch(typeof(scrPlanet), nameof(scrPlanet.SwitchChosen))]
    public static class Inject_scrPlanet_SwitchChosen
    {
        public static void Prefix(
            scrPlanet __instance
        )
        {
            IsInSwitchChosen = true;
            if (!Adofai.Controller.gameworld) return;
            if (ReplayRecorder.Replay is null || Adofai.Controller.playerOne.midspinInfiniteMargin) return;

            var nextFloorAuto = __instance.currfloor.nextfloor != null && __instance.currfloor.nextfloor.auto;

            var angleDiff = __instance.cachedAngle - __instance.targetExitAngle;

            if (!Adofai.Controller.playerOne.planetarySystem.isCW) angleDiff *= -1f;
            if (RDC.auto || (nextFloorAuto && !RDC.useOldAuto)) angleDiff = 0;

            ReplayRecorder.OnErrorMeter(angleDiff);
        }
    }

    [HarmonyPatch(typeof(scrMisc), nameof(scrMisc.GetHitMargin))]
    public static class Inject_scrMisc_GetHitMargin
    {
        public static void Postfix(
            ref HitMargin __result
        )
        {
            if (!IsInSwitchChosen) return;
            IsInSwitchChosen = false;
            if (Interoperation.ReplayIgnoreJudgement) return;
            if (!ReplayPlayer.PlayingReplay) return;
            ReplayPlayer.OnGetHitMargin(ref __result);
        }
    }

    [HarmonyPatch(typeof(scrHitErrorMeter), nameof(scrHitErrorMeter.AddHit))]
    public static class Inject_scrHitErrorMeter_AddHit
    {
        public static void Prefix(
            ref float angleDiff
        )
        {
            if (Interoperation.ReplayIgnoreJudgement) return;
            if (!ReplayPlayer.PlayingReplay) return;
            double result = angleDiff;
            ReplayPlayer.OnErrorMeter(ref result);
            angleDiff = (float)result;
        }
    }

    [HarmonyPatch(typeof(scrController), nameof(scrController.UpdateInput))]
    public static class Inject_scrController_UpdateInput
    {
        private static AccessTools.FieldRef<StateEngine, StateMapping> DestinationStateField { get; } =
            AccessTools.FieldRefAccess<StateEngine, StateMapping>("destinationState");

        public static void Prefix()
        {
            var replayToRecord = ReplayRecorder.Replay;

            if (replayToRecord == null) return;

            if (
                !AsyncInputManager.isActive ||
                !Persistence.GetChosenAsynchronousInput() ||
                !Application.isFocused ||
                (Adofai.CurrentFloorId <= replayToRecord.Metadata.StartingFloorId && (
                    Adofai.Controller.state != States.PlayerControl ||
                    (States)DestinationStateField(Adofai.Controller.stateMachine).state != States.PlayerControl ||
                    !Adofai.Controller.playerOne.responsive
                ))
            )
            {
                KeyQueue.Clear();
                return;
            }

            ReplayRecorder.OnIterationStart(Adofai.Controller.playerOne.keyTimes.Count);

            var sortedKeyQueue = new PriorityQueue<SkyHookEvent, long>();

            while (KeyQueue.TryDequeue(out var key))
                sortedKeyQueue.Enqueue(key, key.GetTimeInTicks());

            var keys = Persistence.keyLimiterKeys.asyncKeysCache;
            var limit = RDInput.useKeyLimiter && keys.Count > 0;

            while (sortedKeyQueue.TryDequeue(out var key, out var ticks))
            {
                if (limit && !keys.Contains(key.Key)) continue;
                ReplayRecorder.OnKeyEvent(
                    0x1000 + key.Key,
                    key.Type == EventType.KeyReleased,
                    DspToSong(TickToDsp(ticks), SettingsReplay.Instance.AsyncRecordingOffset / 1000.0)
                );
            }
        }
    }

    [HarmonyPatch(typeof(scrController), "PlayerControl_Update")]
    public static class Inject_scrController_PlayerControl_Update
    {
        private static AccessTools.FieldRef<StateEngine, StateMapping> DestinationStateField { get; } =
            AccessTools.FieldRefAccess<StateEngine, StateMapping>("destinationState");

        private static AccessTools.FieldRef<RDInputType_Keyboard, KeyCode[]> MainKeysField { get; } =
            AccessTools.FieldRefAccess<RDInputType_Keyboard, KeyCode[]>("mainKeys");

        public static void Prefix()
        {
            if (Adofai.Controller.paused)
            {
                KeyQueue.Clear();
                return;
            }

            if (ReplayPlayer.PlayingReplay) ReplayPlayer.UpdateReplayKeyStates();

            if (AsyncInputManager.isActive || Persistence.GetChosenAsynchronousInput()) return;

            KeyQueue.Clear();

            var replayToRecord = ReplayRecorder.Replay;

            if (
                replayToRecord == null ||
                (Adofai.CurrentFloorId <= replayToRecord.Metadata.StartingFloorId && (
                    Adofai.Controller.state != States.PlayerControl ||
                    (States)DestinationStateField(Adofai.Controller.stateMachine).state != States.PlayerControl ||
                    !Adofai.Controller.playerOne.responsive
                ))
            )
            {
                KeyQueue.Clear();
                return;
            }

            ReplayRecorder.OnIterationStart(Adofai.Controller.playerOne.keyTimes.Count);

            var mainKeys = MainKeysField(RDInput.keyboardInput)!;

            var keys = Persistence.keyLimiterKeys.unityKeysCache;
            var limit = RDInput.useKeyLimiter && keys.Count > 0;

            foreach (var mainKey in mainKeys)
            {
                var wentDown = Input.GetKeyDown(mainKey);
                var wentUp = Input.GetKeyUp(mainKey);

                if (wentDown)
                    CallEvent(false);
                else if (wentUp)
                    CallEvent(true);

                continue;

                void CallEvent(bool isKeyUp)
                {
                    if (limit && !keys.Contains(mainKey)) return;
                    ReplayRecorder.OnKeyEvent(
                        (int)mainKey,
                        isKeyUp,
                        DspToSong(Adofai.Conductor.dspTime, SettingsReplay.Instance.SyncRecordingOffset / 1000.0)
                    );
                }
            }
        }
    }

    [HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
    public static class Inject_Input_GetKey
    {
        public static bool Prefix(
            ref bool __result,
            KeyCode key
        )
        {
            return ReplayPlayer.OnGetKey(key, ref __result);
        }
    }

    [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
    public static class Inject_Input_GetKeyDown
    {
        public static bool Prefix(
            ref bool __result,
            KeyCode key
        )
        {
            return !ReplayPlayer.PlayingReplay || ReplayPlayer.OnGetKeyDown(key, ref __result);
        }
    }

    [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
    public static class Inject_Input_GetKeyUp
    {
        public static bool Prefix(
            ref bool __result,
            KeyCode key
        )
        {
            return !ReplayPlayer.PlayingReplay || ReplayPlayer.OnGetKeyUp(key, ref __result);
        }
    }

    [HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.ValidInputWasTriggered))]
    public static class Inject_scrPlayer_ValidInputWasTriggered
    {
        public static bool Prefix(ref bool __result)
        {
            if (!ReplayPlayer.PlayingReplay) return true;

            __result = !Adofai.Controller.exitingToMainMenu &&
                       (ReplayPlayer.OnGetAnyKeyDown() ||
                        RDInput.GetMain(ButtonState.IsDown) > 0) &&
                       Adofai.Controller.playerOne.CountValidKeysPressed() > 0;

            return false;
        }
    }

    [HarmonyPatch(typeof(LevelData), nameof(LevelData.LoadLevel))]
    public static class Inject_LevelData_LoadLevel
    {
        public static void Prefix()
        {
            ReplayPlayer.UnloadReplay();
        }
    }

    [HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Simulated_PlayerControl_Update))]
    public static class Inject_scrPlayer_Simulated_PlayerControl_Update
    {
        public static bool Prefix()
        {
            if (!ReplayPlayer.PlayingReplay) return true;
            return !ReplayPlayer.PlayingReplay || ReplayPlayer.AllowGameToUpdateInput;
        }

        public static void Postfix()
        {
            if (!ReplayPlayer.PlayingReplay) return;
            scrPlayer.shouldReplaceCamyToPos = false;
        }
    }

    [HarmonyPatch(typeof(scrPlayer), "CheckPostHoldFail")]
    public static class Inject_scrPlayer_CheckPostHoldFail
    {
        public static bool Prefix()
        {
            if (!ReplayPlayer.PlayingReplay) return true;
            var continueExecution = ReplayPlayer.NextCheckFailMiss;
            ReplayPlayer.NextCheckFailMiss = false;
            return continueExecution;
        }
    }

    [HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Hit))]
    public static class Inject_scrPlayer_Hit
    {
        public static void Prefix()
        {
            if (!ReplayPlayer.PlayingReplay) return;
            if (!Adofai.Controller.noFailInfiniteMargin) return;
            ReplayPlayer.NextCheckFailMiss = false;
        }

        public static void Postfix()
        {
            if (ReplayRecorder.Replay is null) return;
            ReplayRecorder.OnPostHit();
        }
    }

    [HarmonyPatch(typeof(AsyncInputManager), "Setup")]
    public static class Inject_AsyncInputManager_Setup
    {
        public static void Prefix(
            List<KeyLabel> ___MouseKeys
        )
        {
            SkyHookManager.KeyUpdated.AddListener(keyEvent =>
            {
                try
                {
                    if (___MouseKeys.Contains(keyEvent.Label)) return;
                    KeyQueue.Enqueue(keyEvent);
                }
                catch (Exception exception)
                {
                    Main.Mod.Logger.Log($"async exception: {exception}");
                }
            });
        }
    }

    [HarmonyPatch(typeof(scrPlanet), nameof(scrPlanet.AutoShouldHitNow))]
    public static class Inject_scrPlanet_AutoShouldHitNow
    {
        public static bool Prefix(
            ref bool __result
        )
        {
            if (!ReplayPlayer.PlayingReplay) return true;
            var continueExecution = ReplayPlayer.AllowAuto;
            ReplayPlayer.AllowAuto = false;
            if (!continueExecution) __result = false;
            return continueExecution;
        }
    }

    [HarmonyPatch(typeof(scrPlayer), "UpdateHoldKeys")]
    public static class Inject_scrPlayer_UpdateHoldKeys
    {
        private static readonly Func<scrPlayer, bool> NextTileIsHoldGetter =
            AccessTools.MethodDelegate<Func<scrPlayer, bool>>(AccessTools.DeclaredPropertyGetter(typeof(scrPlayer), "_nextTileIsHold"));

        private static readonly Func<scrPlayer, double> HoldMarginGetter =
            AccessTools.MethodDelegate<Func<scrPlayer, double>>(AccessTools.DeclaredPropertyGetter(typeof(scrPlayer), "_holdMargin"));

        public static void Prefix()
        {
            var controller = Adofai.Controller;

            if (ReplayRecorder.Replay is null) return;

            if (
                controller.playerOne.keyTimes.Count <= 0 ||
                GCS.d_stationary ||
                (!((controller.currFloor.holdLength > -1 && !controller.strictHolds) ||
                   NextTileIsHoldGetter(controller.playerOne)) &&
                 controller.currFloor.holdLength != -1 &&
                 controller.currFloor.holdCompletion >= HoldMarginGetter(controller.playerOne)) ||
                (controller.gameworld &&
                 controller.currFloor.seqID >= ADOBase.lm.listFloors.Count - 1)
            ) return;

            var nextFloor = Adofai.Controller.currFloor.nextfloor;
            var autoFloor = nextFloor != null && nextFloor.auto;

            ReplayRecorder.OnMarkKeyEvent(autoFloor, controller.playerOne.responsive);
        }
    }

    [HarmonyPatch(typeof(RDInput), nameof(RDInput.GetState))]
    public static class Inject_RDInput_GetState
    {
        public static void Prefix(
            out List<RDInputType> __state
        )
        {
            __state = RDInput.inputs;
            if (!ReplayPlayer.PlayingReplay) return;
            RDInput.inputs = ReplayKeyboardInputType.SingletonList;
        }

        public static Exception? Finalizer(
            Exception? __exception,
            List<RDInputType> __state
        )
        {
            RDInput.inputs = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(RDInput), nameof(RDInput.GetMain))]
    public static class Inject_RDInput_GetMain
    {
        public static void Prefix(
            out List<RDInputType> __state
        )
        {
            __state = RDInput.inputs;
            if (!ReplayPlayer.PlayingReplay) return;
            RDInput.inputs = ReplayKeyboardInputType.SingletonList;
        }

        public static Exception? Finalizer(
            Exception? __exception,
            List<RDInputType> __state
        )
        {
            RDInput.inputs = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(RDInput), nameof(RDInput.GetStateKeys))]
    public static class Inject_RDInput_GetStateKeys
    {
        public static void Prefix(
            out List<RDInputType> __state
        )
        {
            __state = RDInput.inputs;
            if (!ReplayPlayer.PlayingReplay) return;
            RDInput.inputs = ReplayKeyboardInputType.SingletonList;
        }

        public static Exception? Finalizer(
            Exception? __exception,
            List<RDInputType> __state
        )
        {
            RDInput.inputs = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(scrController), "ProcessKeyInputs")]
    public static class Inject_scrController_ProcessKeyInputs
    {
        public static bool Prefix()
        {
            return !ReplayPlayer.PlayingReplay;
        }
    }

    [HarmonyPatch(typeof(scnEditor), "ClearAllFloorOffsets")]
    public static class Inject_scnEditor_ClearAllFloorOffsets
    {
        public static void Prefix()
        {
            ReplayPlayer.UnloadReplay();
            ReplayRecorder.EndRecording();
            ReplayPlayer.EndPlaying();
        }
    }

    [HarmonyPatch(typeof(scrPlayer), "UpdateHoldBehavior")]
    public static class Inject_scrPlayer_UpdateHoldBehavior
    {
        public static void Prefix()
        {
            IsInUpdateHoldBehavior = true;
        }

        public static Exception? Finalizer(
            Exception? __exception
        )
        {
            IsInUpdateHoldBehavior = false;
            return __exception;
        }
    }
}