using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YqlossClientHarmony.Features.Replay.Interop;

namespace YqlossClientHarmony.Features.Replay;

public static class ReplayPlayer
{
    public static bool PlayingReplay { get; set; }

    public static Replay? Replay { get; set; }

    private static MyQueue<(HitMargin, int)>? HitMargins { get; set; }

    private static MyQueue<(double, int)>? ErrorMeters { get; set; }

    private static MyQueue<Replay.KeyEventType>? KeyEvents { get; set; }

    private static MyQueue<Replay.KeyEventType>? KeyEventsForReceivers { get; set; }

    private static Dictionary<int, bool> KeyStates { get; set; } = [];

    private static Dictionary<int, bool> KeyStatesForReceivers { get; set; } = [];

    private static HashSet<int> IsKeyDown { get; } = [];

    private static HashSet<int> IsKeyUp { get; } = [];

    private static bool AnyKeyDown { get; set; }

    public static bool AllowGameToUpdateInput { get; set; }

    public static bool NextCheckFailMiss { get; set; }

    public static bool AllowAuto { get; set; }

    private static double? TrailEndTime { get; set; }

    public static HashSet<int> IgnoredKeys { get; } = [];

    private static double SongSeconds => Injections.DspToSong(Adofai.Conductor.dspTime, SettingsReplay.Instance.PlayingOffset / 1000.0);

    private static void SortKeyEvents(List<(Replay.KeyEventType, int)> keyEvents)
    {
        if (!SettingsReplay.Instance.DecoderSortKeyEvents) return;

        keyEvents.Sort((x, y) => x.Item1.SongSeconds.CompareTo(y.Item1.SongSeconds));
    }

    private static void HandleMultiReleases(List<(Replay.KeyEventType, int)> keyEvents)
    {
        if (!SettingsReplay.Instance.OnlyStoreLastInMultiReleases) return;

        List<(Replay.KeyEventType, int)> filtered = [];
        Dictionary<int, bool> isKeyDown = [];

        foreach (var (keyEvent, floorId) in Enumerable.Reverse(keyEvents))
            if (keyEvent.IsKeyUp)
            {
                if (!isKeyDown.GetValueOrDefault(keyEvent.KeyCode, true)) continue;
                isKeyDown[keyEvent.KeyCode] = false;
                filtered.Add((keyEvent, floorId));
            }
            else
            {
                isKeyDown[keyEvent.KeyCode] = true;
                filtered.Add((keyEvent, floorId));
            }

        keyEvents.Clear();
        keyEvents.AddRange(Enumerable.Reverse(filtered));
    }

    private static void HandleIgnoredKeys(List<(Replay.KeyEventType, int)> keyEvents)
    {
        keyEvents.RemoveAll(pair => IgnoredKeys.Contains(pair.Item1.KeyCode));
    }

    public static void StartPlaying(int floorId)
    {
        _ = KeyEventReceiverManager.Instance;
        _ = CompatibilityOverlayer.Instance;

        var replay = Replay;
        if (replay is null) return;

        PlayingReplay = true;

        KeyStates = [];
        KeyStatesForReceivers = [];
        AllowGameToUpdateInput = false;
        NextCheckFailMiss = false;
        AllowAuto = false;
        TrailEndTime = null;

        ReplayKeyboardInputType.Instance.MarkUpdate();

        {
            MyQueue<Replay.KeyEventType> keyEvents = new();
            MyQueue<Replay.KeyEventType> keyEventsForReceivers = new();
            KeyEvents = keyEvents;
            KeyEventsForReceivers = keyEventsForReceivers;
            var accumulatedFloorId = replay.Metadata.StartingFloorId;

            List<(Replay.KeyEventType, int)> replayKeyEvents = [];
            List<(Replay.KeyEventType, int)> replayKeyEventsForReceivers = [];
            foreach (var keyEvent in replay.KeyEvents)
            {
                accumulatedFloorId += keyEvent.FloorIdIncrement;
                replayKeyEvents.Add((keyEvent, accumulatedFloorId));
                replayKeyEventsForReceivers.Add((keyEvent, accumulatedFloorId));
            }

            HandleIgnoredKeys(replayKeyEvents);
            HandleIgnoredKeys(replayKeyEventsForReceivers);
            HandleMultiReleases(replayKeyEventsForReceivers);
            SortKeyEvents(replayKeyEvents);
            SortKeyEvents(replayKeyEventsForReceivers);

            foreach (var keyEventWithFloorId in replayKeyEvents)
            {
                var (keyEvent, keyEventFloorId) = keyEventWithFloorId;
                if (keyEventFloorId < floorId) continue;
                keyEvents.Enqueue(keyEvent);
            }

            foreach (var keyEventWithFloorId in replayKeyEventsForReceivers)
            {
                var (keyEvent, keyEventFloorId) = keyEventWithFloorId;
                if (keyEventFloorId < floorId) continue;
                keyEventsForReceivers.Enqueue(keyEvent);
            }
        }

        {
            var accumulatedFloorId = replay.Metadata.StartingFloorId;
            MyQueue<(HitMargin, int)> hitMargins = new();
            MyQueue<(double, int)> errorMeters = new();
            HitMargins = hitMargins;
            ErrorMeters = errorMeters;
            foreach (var judgement in replay.Judgements)
            {
                accumulatedFloorId += judgement.FloorIdIncrement;
                if (accumulatedFloorId < floorId) continue;
                hitMargins.Enqueue((judgement.HitMargin, accumulatedFloorId));
                errorMeters.Enqueue((judgement.ErrorMeter, accumulatedFloorId));
            }
        }

        KeyEventReceiverManager.Instance.Begin();

        ReplayStorageProtocol.OnStartReplaying(floorId);

        Main.Mod.Logger.Log("starting to play replay");
    }

    public static void EndPlaying()
    {
        if (PlayingReplay)
        {
            Main.Mod.Logger.Log("stopped playing replay");

            var trailLengthMs = SettingsReplay.Instance.TrailLength;

            if (trailLengthMs <= 0)
            {
                TrailEndTime = null;
                KeyEventReceiverManager.Instance.End();
            }
            else
            {
                TrailEndTime = SongSeconds + trailLengthMs / 1000.0;

                if (SettingsReplay.Instance.Verbose)
                    Main.Mod.Logger.Log($"key release scheduled at {TrailEndTime}");
            }

            ReplayStorageProtocol.OnStopReplaying();
        }

        PlayingReplay = false;
        HitMargins = null;
        ErrorMeters = null;
        KeyEvents = null;
        KeyStates.Clear();
        KeyEventsForReceivers = null;
        NextCheckFailMiss = false;
        AllowAuto = false;
    }

    public static void ResetTrailingAnimation()
    {
        Main.Mod.Logger.Log("reset key animation");
        TrailEndTime = null;
        KeyStatesForReceivers.Clear();
        KeyEventReceiverManager.Instance.End();
    }

    public static void OnHitMargin(ref HitMargin result)
    {
        if (!PlayingReplay) return;

        var floorId = Adofai.CurrentFloorId;
        var hitMargins = HitMargins;

        if (hitMargins?.Count == 0)
        {
            Main.Mod.Logger.Warning($"[Floor {floorId}] hit margins are drained");
            hitMargins = HitMargins = null;
        }

        if (hitMargins is null) return;

        var (hitMargin, eventFloorId) = hitMargins.Dequeue();

        if (floorId != eventFloorId)
            Main.Mod.Logger.Warning($"[Floor {floorId}] hit margin floor id mismatch {eventFloorId}. judgements may be incorrect");

        if (SettingsReplay.Instance.Verbose)
            Main.Mod.Logger.Log($"[Floor {floorId}] acc: {eventFloorId} margin: {hitMargin}");

        hitMargin = hitMargin switch
        {
            ReplayConstants.HoldPreMiss => HitMargin.FailMiss,
            ReplayConstants.HoldExtraPress => HitMargin.TooEarly,
            _ => hitMargin
        };

        if (!Adofai.Controller.playerOne.midspinInfiniteMargin)
        {
            result = hitMargin;
            return;
        }

        var errorMeters = ErrorMeters;

        if (errorMeters is null || errorMeters.Count == 0)
        {
            result = hitMargin;
            return;
        }

        var (nextMeter, nextMeterFloor) = errorMeters.Dequeue();

        if (SettingsReplay.Instance.Verbose)
            Main.Mod.Logger.Log($"[Floor {floorId}] acc: {eventFloorId} midspin meter");

        if (floorId != nextMeterFloor)
            Main.Mod.Logger.Warning($"[Floor {floorId}] midspin error meter floor id mismatch {eventFloorId}. judgements may be incorrect");

        if (!double.IsNaN(nextMeter))
            Main.Mod.Logger.Warning($"[Floor {floorId}] midspin error meter has value {nextMeter}. judgements may be incorrect");

        result = hitMargin;
    }

    public static void OnErrorMeter(ref double result)
    {
        if (!PlayingReplay) return;

        var floorId = Adofai.CurrentFloorId;
        var errorMeters = ErrorMeters;

        if (errorMeters?.Count == 0)
        {
            Main.Mod.Logger.Warning($"[Floor {floorId}] error meters are drained");
            errorMeters = ErrorMeters = null;
        }

        if (errorMeters is null) return;

        var (errorMeter, eventFloorId) = errorMeters.Dequeue();

        if (!double.IsFinite(errorMeter))
        {
            Main.Mod.Logger.Warning($"[Floor {floorId}] error meter is not finite {errorMeter}. judgements may be incorrect");
            return;
        }

        if (floorId != eventFloorId)
            Main.Mod.Logger.Warning($"[Floor {floorId}] error meter floor id mismatch {eventFloorId}. judgements may be incorrect");

        if (SettingsReplay.Instance.Verbose)
            Main.Mod.Logger.Log($"[Floor {floorId}] acc: {eventFloorId} meter: {errorMeter}");

        result = errorMeter;
    }

    public static void OnGetHitMargin(ref HitMargin result)
    {
        if (!PlayingReplay) return;

        var floorId = Adofai.CurrentFloorId;
        var hitMargins = HitMargins;

        if (hitMargins?.Count == 0)
        {
            Main.Mod.Logger.Warning($"[Floor {floorId}] hit margins are drained");
            hitMargins = HitMargins = null;
        }

        if (hitMargins is null) return;

        var (hitMargin, eventFloorId) = hitMargins.Peek();

        if (floorId != eventFloorId)
            Main.Mod.Logger.Warning($"[Floor {floorId}] get hit margin floor id mismatch {eventFloorId}. judgements may be incorrect");

        if (SettingsReplay.Instance.Verbose)
            Main.Mod.Logger.Log($"[Floor {floorId}] get acc: {eventFloorId} margin: {hitMargin}");

        if (hitMargin == ReplayConstants.HoldExtraPress)
        {
            hitMargins.Dequeue();
            if (SettingsReplay.Instance.Verbose)
                Main.Mod.Logger.Log($"[Floor {floorId}] hold extra press");
        }

        result = hitMargin switch
        {
            HitMargin.FailMiss => HitMargin.TooLate,
            HitMargin.FailOverload => HitMargin.TooEarly,
            HitMargin.Auto => HitMargin.Perfect,
            ReplayConstants.HoldPreMiss => HitMargin.TooEarly,
            ReplayConstants.HoldExtraPress => HitMargin.TooEarly,
            _ => hitMargin
        };
    }

    private static bool ProcessAutoFloorAndFailMiss()
    {
        {
            for (
                var nextFloor = Adofai.Controller.currFloor.nextfloor;
                nextFloor != null && nextFloor.auto;
                nextFloor = Adofai.Controller.currFloor.nextfloor
            )
            {
                var floorId = Adofai.CurrentFloorId;
                NextCheckFailMiss = false;
                AllowGameToUpdateInput = true;
                AllowAuto = true;
                ReplayKeyboardInputType.Instance.MarkUpdate();
                Adofai.Controller.playerOne.Simulated_PlayerControl_Update();
                AllowGameToUpdateInput = false;
                if (Adofai.CurrentFloorId == floorId) break;
                if (SettingsReplay.Instance.Verbose) Main.Mod.Logger.Log("auto floor");
            }
        }

        {
            for (;;)
            {
                var hitMargins = HitMargins;

                if (hitMargins is null || hitMargins.Count == 0) break;

                var hitMarginCount = hitMargins.Count;
                var (hitMargin, _) = hitMargins.Peek();

                if (hitMargin is HitMargin.FailMiss)
                {
                    NextCheckFailMiss = true;
                    AllowGameToUpdateInput = true;
                    AllowAuto = false;
                    ReplayKeyboardInputType.Instance.MarkUpdate();
                    Adofai.Controller.playerOne.Simulated_PlayerControl_Update();
                    NextCheckFailMiss = false;
                    AllowGameToUpdateInput = false;
                }
                else
                {
                    break;
                }

                if (HitMargins?.Count == hitMarginCount) return true;
                if (SettingsReplay.Instance.Verbose) Main.Mod.Logger.Log("fail miss");
            }
        }

        return false;
    }

    public static void HandleTrail()
    {
        var trail = TrailEndTime;

        if (!PlayingReplay && trail is null) return;

        try
        {
            var songSeconds = SongSeconds;
            if (!PlayingReplay && songSeconds > trail) ResetTrailingAnimation();
        }
        catch
        {
            // ignored
        }
    }

    private static void HandleKeyEventReceivers()
    {
        if (!PlayingReplay) return;

        var isKeyDown = IsKeyDown;
        var isKeyUp = IsKeyUp;
        var lastKeyStatesForReceivers = KeyStatesForReceivers;

        try
        {
            var songSeconds = SongSeconds;

            isKeyDown.Clear();
            isKeyUp.Clear();
            AnyKeyDown = false;

            var keyEventsForReceivers = KeyEventsForReceivers;

            if (keyEventsForReceivers is null) return;

            while (keyEventsForReceivers.Count != 0)
            {
                var key = keyEventsForReceivers.Peek();
                var syncKeyCode = KeyCodeMapping.GetSyncKeyCode(key.KeyCode);

                if (key.SongSeconds > songSeconds) break;

                keyEventsForReceivers.Dequeue();

                lastKeyStatesForReceivers = KeyStatesForReceivers = new Dictionary<int, bool>(lastKeyStatesForReceivers)
                {
                    [syncKeyCode] = !key.IsKeyUp
                };

                KeyEventReceiverManager.Instance.OnKey((KeyCode)syncKeyCode, !key.IsKeyUp);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            isKeyDown.Clear();
            isKeyUp.Clear();
            AnyKeyDown = false;
            KeyStatesForReceivers = lastKeyStatesForReceivers;
        }
    }

    public static void UpdateReplayKeyStates()
    {
        if (!PlayingReplay) return;

        HandleKeyEventReceivers();

        var songSeconds = SongSeconds;

        var isKeyDown = IsKeyDown;
        var isKeyUp = IsKeyUp;

        var lastKeyStates = KeyStates;
        var lastKeyStatesForReceivers = KeyStatesForReceivers;

        isKeyDown.Clear();
        isKeyUp.Clear();
        AnyKeyDown = false;

        try
        {
            if (ProcessAutoFloorAndFailMiss()) return;

            var keyEvents = KeyEvents;

            if (keyEvents?.Count == 0)
            {
                Main.Mod.Logger.Warning($"[Floor {Adofai.CurrentFloorId}] key events are drained");
                keyEvents = KeyEvents = null;
            }

            if (keyEvents is null)
            {
                NextCheckFailMiss = true;
                AllowGameToUpdateInput = true;
                AllowAuto = true;
                ReplayKeyboardInputType.Instance.MarkUpdate();
                Adofai.Controller.playerOne.Simulated_PlayerControl_Update();
                AllowGameToUpdateInput = false;
                return;
            }

            while (keyEvents.Count != 0)
            {
                var floorId = Adofai.CurrentFloorId;

                var key = keyEvents.Peek();
                var syncKeyCode = KeyCodeMapping.GetSyncKeyCode(key.KeyCode);

                var nextFloor = Adofai.Controller.currFloor.nextfloor;
                var autoFloor = nextFloor != null && nextFloor.auto;

                if (!key.Version1)
                {
                    if (!key.IsAutoFloor && autoFloor) break;
                    if (!key.IsInputLocked && !Adofai.Controller.playerOne.responsive) break;
                }

                if (key.SongSeconds > songSeconds) break;

                if (SettingsReplay.Instance.Verbose)
                    Main.Mod.Logger.Log($"[Floor {floorId}] simulate key: {syncKeyCode}({key.KeyCode}) up: {key.IsKeyUp} dseq: {key.FloorIdIncrement} pos: {key.SongSeconds} auto: {key.IsAutoFloor} locked: {key.IsInputLocked}");

                var keyStates = KeyStates = new Dictionary<int, bool>(lastKeyStates)
                {
                    [syncKeyCode] = !key.IsKeyUp
                };

                keyEvents.Dequeue();

                isKeyDown.Clear();
                isKeyUp.Clear();
                AnyKeyDown = false;

                foreach (var (keyCode, state) in keyStates)
                {
                    var last = lastKeyStates.GetValueOrDefault(keyCode, false);

                    if (state && !last)
                    {
                        isKeyDown.Add(keyCode);
                        AnyKeyDown = true;
                    }

                    if (!state && last) isKeyUp.Add(keyCode);
                }

                NextCheckFailMiss = false;
                AllowAuto = false;

                if (SettingsReplay.Instance.Verbose)
                    Main.Mod.Logger.Log("begin simulation");

                if (key is { IsAutoFloor: false, IsInputLocked: false })
                {
                    AllowGameToUpdateInput = true;
                    ReplayKeyboardInputType.Instance.MarkUpdate();

                    Adofai.Controller.playerOne.Simulated_PlayerControl_Update();

                    if (SettingsReplay.Instance.Verbose)
                        Main.Mod.Logger.Log("end simulation");
                }
                else
                {
                    if (SettingsReplay.Instance.Verbose)
                        Main.Mod.Logger.Log("skip simulation");
                }

                isKeyDown.Clear();
                isKeyUp.Clear();
                AnyKeyDown = false;
                AllowGameToUpdateInput = false;
                lastKeyStates = keyStates;

                if (ProcessAutoFloorAndFailMiss()) return;
            }
        }
        finally
        {
            isKeyDown.Clear();
            isKeyUp.Clear();
            AnyKeyDown = false;
            KeyStates = lastKeyStates;
            KeyStatesForReceivers = lastKeyStatesForReceivers;
        }
    }

    public static bool OnGetKeyDown(KeyCode keyCode, ref bool result)
    {
        if (!PlayingReplay) return true;
        if (keyCode == KeyCode.Escape) return true;
        result = IsKeyDown.Contains((int)keyCode);
        return false;
    }

    public static bool OnGetKeyUp(KeyCode keyCode, ref bool result)
    {
        if (!PlayingReplay) return true;
        if (keyCode == KeyCode.Escape) return true;
        result = IsKeyUp.Contains((int)keyCode);
        return false;
    }

    public static bool OnGetKey(KeyCode keyCode, ref bool result)
    {
        if (!PlayingReplay && TrailEndTime is null) return true;
        if (keyCode == KeyCode.Escape) return true;
        result = KeyStatesForReceivers.GetValueOrDefault((int)keyCode, false);
        return false;
    }

    public static bool OnGetAnyKeyDown()
    {
        return AnyKeyDown;
    }

    public static bool GetKeyDownUnchecked(KeyCode keyCode)
    {
        return keyCode != KeyCode.Escape && IsKeyDown.Contains((int)keyCode);
    }

    public static bool GetKeyUpUnchecked(KeyCode keyCode)
    {
        return keyCode != KeyCode.Escape && IsKeyUp.Contains((int)keyCode);
    }

    public static bool GetKeyUnchecked(KeyCode keyCode)
    {
        return keyCode != KeyCode.Escape && KeyStates.GetValueOrDefault((int)keyCode, false);
    }

    public static bool GetAnyKeyDownUnchecked(KeyCode[] keyCodes)
    {
        return keyCodes.Any(GetKeyDownUnchecked);
    }

    public static bool GetAnyKeyUpUnchecked(KeyCode[] keyCodes)
    {
        return keyCodes.Any(GetKeyUpUnchecked);
    }

    public static bool GetAnyKeyUnchecked(KeyCode[] keyCodes)
    {
        return keyCodes.Any(GetKeyUnchecked);
    }

    public static bool GetAnyKeyReleasedUnchecked(KeyCode[] keyCodes)
    {
        return keyCodes.Any(it => !GetKeyUnchecked(it));
    }

    public static IEnumerable<KeyCode> GetKeysDownUnchecked(KeyCode[]? keyCodes = null)
    {
        return keyCodes is null
            ? IsKeyDown.Where(key => key != (int)KeyCode.Escape).Select(key => (KeyCode)key)
            : keyCodes.Where(GetKeyDownUnchecked);
    }

    public static IEnumerable<KeyCode> GetKeysUpUnchecked(KeyCode[]? keyCodes = null)
    {
        return keyCodes is null
            ? IsKeyUp.Where(key => key != (int)KeyCode.Escape).Select(key => (KeyCode)key)
            : keyCodes.Where(GetKeyUpUnchecked);
    }

    public static IEnumerable<KeyCode> GetKeysUnchecked(KeyCode[]? keyCodes = null)
    {
        return keyCodes is null
            ? KeyStates.Where(pair => pair.Key != (int)KeyCode.Escape && pair.Value).Select(pair => (KeyCode)pair.Key)
            : keyCodes.Where(GetKeyUnchecked);
    }

    public static IEnumerable<KeyCode> GetKeysReleasedUnchecked(KeyCode[] keyCodes)
    {
        return keyCodes.Where(it => !GetKeyUnchecked(it));
    }

    public static bool LoadReplay(string fileName)
    {
        var replay = ReplayDecoder.ReadCompressedReplay(fileName);

        if (replay is null) return false;

        ReplayRecorder.SaveAndResetReplay();

        Replay = replay;
        IgnoredKeys.Clear();

        if (SettingsReplay.Instance.EnableDecoderLimitKeyCount)
            ReplayUtils.GetSortedKeyPressCounts(replay)
                .Skip(SettingsReplay.Instance.DecoderLimitKeyCount)
                .Select(it => it.Item1)
                .ForEach(it => IgnoredKeys.Add(it));

        ReplayStorageProtocol.OnLoadReplay(replay.CustomPayloads);

        return true;
    }

    public static void UnloadReplay()
    {
        Replay = null;
        IgnoredKeys.Clear();
        if (PlayingReplay) EndPlaying();

        ReplayStorageProtocol.OnUnloadReplay();
    }
}