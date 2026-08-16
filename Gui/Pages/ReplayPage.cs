using System;
using System.Collections.Generic;
using System.Linq;
using UnityFileDialog;
using YqlossClientHarmony.Features.Replay;
using YqlossClientHarmony.Utilities;
using static YqlossClientHarmony.Gui.YCHLayout;
using static YqlossClientHarmony.Gui.YCHLayoutPreset;
using static YqlossClientHarmony.Utilities.SettingUtil;

namespace YqlossClientHarmony.Gui.Pages;

public static class ReplayPage
{
    private static IReadOnlyList<string>? LastError { get; set; }

    private static string[] ErrorLoadReplay { get; } = ["Gui.Replay.Error.LoadReplay"];

    private static string[] ErrorRequireInLevel { get; } = ["Gui.Replay.Error.RequireInLevel"];

    private static string[] ErrorOfficialLevel { get; } = ["Gui.Replay.Error.OfficialLevel"];

    private static string[] ErrorLoadInGame { get; } = ["Gui.Replay.Error.LoadInGame"];

    private static string[] ErrorUnloadInGame { get; } = ["Gui.Replay.Error.UnloadInGame"];

    private static string[] ImportantTexts { get; } =
    [
        "Gui.Replay.Important.RecordInAsync",
        "Gui.Replay.Important.OnlySupportKeyboard",
        "Gui.Replay.Important.DLCCompatibility",
        "Gui.Replay.Important.KeyboardChatterBlockerCompatibility",
        "Gui.Replay.Important.OverlayerCompatibility",
        "Gui.Replay.Important.KeyLimiterCompatibility"
    ];

    private static string LoadedReplayFileName { get; set; } = "";

    private static Trigger<ReplayInformationCacheKey, (string[], List<(int, int)>)> CachedReplayInformation { get; } = new();

    private static SizesGroup.Holder Group { get; } = new();

    private static void LoadReplay()
    {
        if (!Adofai.Controller.gameworld)
        {
            LastError = ErrorRequireInLevel;
            return;
        }

        if (ADOBase.isOfficialLevel)
        {
            LastError = ErrorOfficialLevel;
            return;
        }

        if (!Adofai.Controller.paused)
        {
            LastError = ErrorLoadInGame;
            return;
        }

        var replayFileName = FileBrowser.PickFile(
            SettingsReplay.Instance.ReplayStorageLocation,
            null,
            ["ychreplaygz", "ychreplay.gz"],
            I18N.Translate("Dialog.Replay.SelectReplay.Title")
        );

        if (replayFileName is null) return;

        LoadedReplayFileName = replayFileName;

        LastError = ReplayPlayer.LoadReplay(replayFileName) ? null : ErrorLoadReplay;
    }

    private static void SelectReplayStorageLocation()
    {
        var storageLocation = FileBrowser.PickFolder(
            SettingsReplay.Instance.ReplayStorageLocation,
            null,
            null,
            I18N.Translate("Dialog.Replay.SelectReplayStorageLocation.Title")
        );

        if (storageLocation is null) return;

        SettingsReplay.Instance.ReplayStorageLocation = storageLocation;
        Save |= true;
    }


    private static void UnloadReplay()
    {
        if (Adofai.Controller.gameworld && !ADOBase.isOfficialLevel && !Adofai.Controller.paused)
        {
            LastError = ErrorUnloadInGame;
            return;
        }

        ReplayPlayer.UnloadReplay();

        LastError = null;
    }

    private static void TryJumpToFloor(int floorId)
    {
        try
        {
            Adofai.Editor.SelectFloor(Adofai.Editor.floors[floorId]);
        }
        catch
        {
            // ignored
        }
    }

    private static string[] ReplayInformationKeys(bool advanced)
    {
        IEnumerable<string> keys =
        [
            I18N.Translate("Gui.Replay.ReplayInformation.ReplayFile.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.LevelPath.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Artist.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Song.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Author.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Pitch.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.XAccuracy.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Progress.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Judgements.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Difficulty.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.NoFail.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.HoldTileBehavior.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.LimitJudgements.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.KeyCount.Name"),
            I18N.Translate("Gui.Replay.ReplayInformation.Plugins.Name")
        ];

        if (advanced)
            keys = keys.Concat([
                I18N.Translate("Gui.Replay.ReplayInformation.AsyncInput.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.KeyPressCounts.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.YchVersion.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.StartTime.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.EndTime.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.RecordingOffset.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.InputOffset.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.AudioBufferSize.Name"),
                I18N.Translate("Gui.Replay.ReplayInformation.ModList.Name")
            ]);

        return keys.ToArray();
    }

    private static (int, List<int>) GetKeyCountInfo(Replay replay)
    {
        var keyCount = ReplayUtils.CalculateKeyPressCounts(replay);
        var values = keyCount.Values.ToList();
        values.Sort((x, y) => y.CompareTo(x));
        return (keyCount.Count, values);
    }

    private static string[] ReplayInformationValues(string replayFileName, Replay replay)
    {
        var filteredArtist = ReplayUtils.FilterInvalidCharacters(replay.Metadata.Artist).Trim();
        var filteredSong = ReplayUtils.FilterInvalidCharacters(replay.Metadata.Song).Trim();
        var filteredAuthor = ReplayUtils.FilterInvalidCharacters(replay.Metadata.Author).Trim();
        var pitchKey = replay.Metadata.Pitch is null ? "Unknown" : "Value";
        var pitch = replay.Metadata.Pitch;
        var xAccuracy = ReplayUtils.GetXAccuracy(replay) * 100;
        var startFloor = replay.Metadata.StartingFloorId;
        var endFloor = ReplayUtils.GetEndingFloorId(replay) + 1;
        var startProgress = startFloor * 100 / replay.Metadata.TotalFloorCount;
        if (replay.Metadata.StartingFloorId != 0 && startProgress == 0) startProgress = 1;
        var endProgress = endFloor * 100 / replay.Metadata.TotalFloorCount;
        var te = ReplayUtils.GetHitMarginCount(replay, HitMargin.TooEarly);
        var e = ReplayUtils.GetHitMarginCount(replay, HitMargin.VeryEarly);
        var ep = ReplayUtils.GetHitMarginCount(replay, HitMargin.EarlyPerfect);
        var pp = ReplayUtils.GetHitMarginCount(replay, HitMargin.Perfect);
        var lp = ReplayUtils.GetHitMarginCount(replay, HitMargin.LatePerfect);
        var l = ReplayUtils.GetHitMarginCount(replay, HitMargin.VeryLate);
        var tl = ReplayUtils.GetHitMarginCount(replay, HitMargin.TooLate);
        var miss = ReplayUtils.GetHitMarginCount(replay, HitMargin.FailMiss);
        var overload = ReplayUtils.GetHitMarginCount(replay, HitMargin.FailOverload);
        var auto = ReplayUtils.GetHitMarginCount(replay, HitMargin.Auto);
        var difficulty = replay.Metadata.Difficulty switch
        {
            Difficulty.Lenient => "Lenient",
            Difficulty.Normal => "Normal",
            Difficulty.Strict => "Strict",
            _ => $"{replay.Metadata.Difficulty}"
        };
        var asyncInput = replay.Metadata.UseAsyncInput ? "True" : "False";
        var noFail = replay.Metadata.NoFailMode ? "True" : "False";
        var holdBehavior = replay.Metadata.HoldBehavior switch
        {
            HoldBehavior.Normal => "Normal",
            HoldBehavior.CanHitEnd => "CanHitEnd",
            HoldBehavior.NoHoldNeeded => "NoHoldNeeded",
            _ => $"{replay.Metadata.HoldBehavior}"
        };
        var hitMarginLimit = replay.Metadata.HitMarginLimit switch
        {
            HitMarginLimit.None => "None",
            HitMarginLimit.PerfectsOnly => "PerfectsOnly",
            HitMarginLimit.PurePerfectOnly => "PurePerfectOnly",
            _ => $"{replay.Metadata.HitMarginLimit}"
        };
        var levelPathKey = replay.Metadata.LevelPath is null ? "Unknown" : "Value";
        var ychVersionKey = replay.Metadata.YchVersion is null ? "Unknown" : "Value";
        var startTimeKey = replay.Metadata.StartTime is null ? "Unknown" : "Value";
        var endTimeKey = replay.EndTime is null ? "Unknown" : "Value";
        var recordingOffsetKey = replay.Metadata.RecordingOffset is null ? "Unknown" : "Value";
        var modListKey = replay.Metadata.ModList is null ? "Unknown" : "Value";
        var inputOffsetKey = replay.Metadata.InputOffset is null ? "Unknown" : "Value";
        var audioBufferSizeKey = replay.Metadata.AudioBufferSize is null ? "Unknown" : "Value";
        var (uniqueKeys, keyCounts) = GetKeyCountInfo(replay);
        var plugins = string.Join(",", replay.CustomPayloads.Keys);
        return
        [
            I18N.Translate("Gui.Replay.ReplayInformation.ReplayFile.Value", replayFileName),
            I18N.Translate($"Gui.Replay.ReplayInformation.LevelPath.{levelPathKey}", replay.Metadata.LevelPath),
            I18N.Translate("Gui.Replay.ReplayInformation.Artist.Value", filteredArtist),
            I18N.Translate("Gui.Replay.ReplayInformation.Song.Value", filteredSong),
            I18N.Translate("Gui.Replay.ReplayInformation.Author.Value", filteredAuthor),
            I18N.Translate($"Gui.Replay.ReplayInformation.Pitch.{pitchKey}", pitch),
            I18N.Translate("Gui.Replay.ReplayInformation.XAccuracy.Value", xAccuracy),
            I18N.Translate("Gui.Replay.ReplayInformation.Progress.Value", startProgress, startFloor, endProgress, endFloor, replay.Metadata.TotalFloorCount),
            I18N.Translate("Gui.Replay.ReplayInformation.Judgements.Value", overload, te, e, ep, pp, auto, lp, l, tl, miss),
            I18N.Translate($"Gui.Replay.ReplayInformation.Difficulty.{difficulty}"),
            I18N.Translate($"Gui.Replay.ReplayInformation.NoFail.{noFail}"),
            I18N.Translate($"Gui.Replay.ReplayInformation.HoldTileBehavior.{holdBehavior}"),
            I18N.Translate($"Gui.Replay.ReplayInformation.LimitJudgements.{hitMarginLimit}"),
            I18N.Translate("Gui.Replay.ReplayInformation.KeyCount.Value", uniqueKeys),
            I18N.Translate("Gui.Replay.ReplayInformation.Plugins.Value", plugins),
            I18N.Translate($"Gui.Replay.ReplayInformation.AsyncInput.{asyncInput}"),
            I18N.Translate("Gui.Replay.ReplayInformation.KeyPressCounts.Value", string.Join(", ", keyCounts)),
            I18N.Translate($"Gui.Replay.ReplayInformation.YchVersion.{ychVersionKey}", replay.Metadata.YchVersion),
            I18N.Translate($"Gui.Replay.ReplayInformation.StartTime.{startTimeKey}", replay.Metadata.StartTime?.ToLocalTime()),
            I18N.Translate($"Gui.Replay.ReplayInformation.EndTime.{endTimeKey}", replay.EndTime?.ToLocalTime()),
            I18N.Translate($"Gui.Replay.ReplayInformation.RecordingOffset.{recordingOffsetKey}", replay.Metadata.RecordingOffset),
            I18N.Translate($"Gui.Replay.ReplayInformation.InputOffset.{inputOffsetKey}", replay.Metadata.InputOffset),
            I18N.Translate($"Gui.Replay.ReplayInformation.AudioBufferSize.{audioBufferSizeKey}", replay.Metadata.AudioBufferSize),
            I18N.Translate($"Gui.Replay.ReplayInformation.ModList.{modListKey}", replay.Metadata.ModList)
        ];
    }

    public static void Draw()
    {
        var settings = SettingsReplay.Instance;
        var group = Group.Begin();
        var advanced = settings.ShowAdvancedOptions;

        Begin(ContainerDirection.Vertical);
        {
            Text(I18N.Translate("Page.Replay.Name"), TextStyle.Title);
            Separator();
            SwitchOption(group, ref Main.Settings.EnableReplay, "Setting.Replay.Enabled", true);
            Separator();
            AddMargin(8);

            Begin(ContainerDirection.Horizontal, options: WidthMax);
            {
                if (Button(I18N.Translate("Gui.Replay.LoadReplay"), options: WidthMax))
                    LoadReplay();

                if (ReplayPlayer.Replay is not null)
                {
                    if (Button(I18N.Translate("Gui.Replay.UnloadReplay"), options: WidthMax))
                        UnloadReplay();
                    if (Button(I18N.Translate("Gui.Replay.JumpToStartingFloor"), options: WidthMax))
                        TryJumpToFloor(ReplayPlayer.Replay.Metadata.StartingFloorId);
                    if (Button(I18N.Translate("Gui.Replay.JumpToEndingFloor"), options: WidthMax))
                        TryJumpToFloor(ReplayUtils.GetEndingFloorId(ReplayPlayer.Replay) + 1);
                }
                else
                {
                    Fill();
                }
            }
            End();

            AddMargin(8);

            var errorGroup = group.Group;
            if (LastError is not null)
            {
                var clearError = false;
                foreach (var line in LastError) clearError |= IconText(errorGroup, IconStyle.Error, line);
                if (clearError) LastError = null;
            }

            List<(int, int)>? keyPressCounts = null;

            var replay = ReplayPlayer.Replay;
            if (replay is not null)
            {
                Text(I18N.Translate("Gui.Replay.ReplayInformation"), TextStyle.Subtitle);

                var keys = ReplayInformationKeys(advanced);
                var (values, keyPressCountsVar) = CachedReplayInformation.Get(
                    new ReplayInformationCacheKey(I18N.SelectedLanguage.Code, replay),
                    _ => (ReplayInformationValues(LoadedReplayFileName, replay), ReplayUtils.GetSortedKeyPressCounts(replay))
                );
                keyPressCounts = keyPressCountsVar;

                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        Begin(ContainerDirection.Horizontal, options: WidthMax);
                        {
                            Text(I18N.Translate(keys[i]), options: Width(120));
                            Text(I18N.Translate(values[i]), options: WidthMax);
                        }
                        End();
                    }
                }
                End();
            }

            Text(I18N.Translate("Gui.Replay.Important"), TextStyle.Subtitle);

            var importantGroup = group.Group;
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                var first = true;
                foreach (var important in ImportantTexts)
                {
                    if (first) first = false;
                    else Separator();
                    IconText(importantGroup, IconStyle.Warning, important);
                }
            }
            End();

            Text(I18N.Translate("Gui.Replay.Settings"), TextStyle.Subtitle);

            var settingsGroup = group.Group;
            Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
            {
                Begin(ContainerDirection.Horizontal, sizes: settingsGroup, options: [WidthMax]);
                PushAlign(0.5);
                {
                    Text(I18N.Translate("Setting.Replay.ReplayStorageLocation"), options: WidthMin);
                    Fill();
                    Save |= TextField(ref settings.ReplayStorageLocation, options: WidthMin);
                    if (Button(I18N.Translate("Setting.Replay.ReplayStorageLocation.Select"), options: WidthMin))
                        SelectReplayStorageLocation();
                }
                PopAlign();
                End();

                Separator();
                CheckboxIntOption(settingsGroup, ref settings.EnableDecoderLimitKeyCount, ref settings.DecoderLimitKeyCount, "Setting.Replay.DecoderLimitKeyCount", true);

                if (advanced)
                {
                    Separator();
                    SwitchOption(settingsGroup, ref settings.StoreSyncKeyCode, "Setting.Replay.StoreSyncKeyCode");
                    Separator();
                    SwitchOption(settingsGroup, ref settings.OnlyStoreLastInMultiReleases, "Setting.Replay.OnlyStoreLastInMultiReleases", true);
                    Separator();
                    DoubleOption(settingsGroup, ref settings.TrailLength, "Setting.Replay.TrailLength", description: true);
                    Separator();
                    SwitchOption(settingsGroup, ref settings.DecoderSortKeyEvents, "Setting.Replay.DecoderSortKeyEvents");
                    Separator();
                    SwitchOption(settingsGroup, ref settings.DisableKeyboardSimulation, "Setting.Replay.DisableKeyboardSimulation");
                    Separator();
                    IconText(settingsGroup, IconStyle.Warning, "Gui.Replay.Settings.OffsetZeroWarning");
                    Separator();
                    DoubleOption(settingsGroup, ref settings.SyncRecordingOffset, "Setting.Replay.SyncRecordingOffset");
                    Separator();
                    DoubleOption(settingsGroup, ref settings.AsyncRecordingOffset, "Setting.Replay.AsyncRecordingOffset");
                    Separator();
                    DoubleOption(settingsGroup, ref settings.PlayingOffset, "Setting.Replay.PlayingOffset");
                }
            }
            End();

            var keySelectionGroup = group.Group;
            if (keyPressCounts is not null)
            {
                Text(I18N.Translate("Gui.Replay.SelectKeys"), TextStyle.Subtitle);

                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    var first = true;

                    foreach (var (keyCode, pressCount) in keyPressCounts)
                    {
                        if (!first) Separator();
                        first = false;
                        Begin(ContainerDirection.Horizontal, ContainerStyle.None, keySelectionGroup, WidthMax);
                        PushAlign(0.5);
                        {
                            var allowed = !ReplayPlayer.IgnoredKeys.Contains(keyCode);
                            if (Checkbox(ref allowed) is not null)
                                if (allowed) ReplayPlayer.IgnoredKeys.Remove(keyCode);
                                else ReplayPlayer.IgnoredKeys.Add(keyCode);
                            Text(I18N.Translate($"Key.{keyCode}.Name"), options: WidthMin);
                            Fill();
                            Text(I18N.Translate("Gui.Replay.SelectKeys.KeyPressCount", pressCount), options: WidthMin);
                        }
                        PopAlign();
                        End();
                    }
                }
                End();
            }

            var debugGroup = group.Group;
            if (advanced)
            {
                Text(I18N.Translate("Gui.Replay.DebugOptions"), TextStyle.Subtitle);

                Begin(ContainerDirection.Vertical, ContainerStyle.Background, options: WidthMax);
                {
                    IconText(debugGroup, IconStyle.Warning, "Gui.Replay.DebugOptions.Warning");
                    Separator();
                    SwitchOption(debugGroup, ref settings.Verbose, "Setting.Replay.Verbose");
                }
                End();
            }

            Separator();
            SwitchOption(group, ref settings.ShowAdvancedOptions, "Setting.Replay.ShowAdvancedOptions");
        }
        End();
    }

    public class ReplayInformationCacheKey(string language, Replay replay)
    {
        private string Language { get; } = language;

        private Replay Replay { get; } = replay;

        public override bool Equals(object? obj)
        {
            if (obj is not ReplayInformationCacheKey key) return false;
            return key.Language == Language && ReferenceEquals(key.Replay, Replay);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Language, Replay);
        }
    }
}