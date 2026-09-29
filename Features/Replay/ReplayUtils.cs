using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YqlossClientHarmony.Features.Replay;

public static class ReplayUtils
{
    private static readonly Regex RegexStyle = new("<.*?>");

    private static readonly List<char> InvalidCharacters = Path.GetInvalidFileNameChars().ToList();

    public static string FilterInvalidCharacters(string path)
    {
        path = RegexStyle.Replace(path, "");
        var builder = new StringBuilder();
        foreach (var c in path)
            if (!InvalidCharacters.Contains(c))
                builder.Append(c);
        return builder.ToString().Trim();
    }

    public static int GetEndingFloorId(Replay replay)
    {
        var floorId = replay.Metadata.StartingFloorId;

        foreach (var judgement in replay.Judgements) floorId += judgement.FloorIdIncrement;

        return floorId;
    }

    // mirrors scrMarginTracker.CalculatePercentAcc of the running game
    public static double GetXAccuracy(Replay replay)
    {
        if (replay.Judgements.Count == 0) return 0;

        var xAccuracy = 0.0;
        var count = 0;

        foreach (var judgement in replay.Judgements)
        {
            var weight = GetXAccuracyWeight(judgement.HitMargin);
            if (weight is null) continue;
            xAccuracy += weight.Value;
            ++count;
        }

        return count == 0 ? 0 : xAccuracy / count;
    }

    private static double? GetXAccuracyWeight(HitMargin hitMargin)
    {
        // 126 and 127 are internal markers, which the game sees as no judgement or a FailMiss
        if (hitMargin == ReplayConstants.HoldPreMiss) hitMargin = HitMargin.FailMiss;
        if (hitMargin == ReplayConstants.HoldExtraPress) return null;

        // 3.4.0 only counts the judgement types in the official weight table,
        // so Midspin, Auto, Multipress and OverPress no longer affect x-accuracy
        if (HitMarginCompat.OfficialWeights is { } weights)
            return weights.TryGetValue(hitMargin, out var weight) ? weight : null;

        return hitMargin.ToString() switch
        {
            "Perfect" or "Auto" => 1.0,
            "EarlyPerfect" or "LatePerfect" => 0.75,
            "VeryEarly" or "VeryLate" => 0.4,
            "TooEarly" or "TooLate" => 0.2,
            _ => 0.0
        };
    }

    // on 3.4.0 XPerfect / PerfectMinus / PerfectPlus together make up the Perfect of old versions
    public static int GetPerfectCount(Replay replay)
    {
        if (!HitMarginCompat.HasSplitPerfect) return GetHitMarginCount(replay, HitMarginCompat.Perfect);

        return GetHitMarginCount(replay, HitMarginCompat.PerfectMinus)
               + GetHitMarginCount(replay, HitMarginCompat.XPerfect)
               + GetHitMarginCount(replay, HitMarginCompat.PerfectPlus);
    }

    public static int GetHitMarginCount(Replay replay, HitMargin hitMargin)
    {
        var count = 0;

        foreach (var judgement in replay.Judgements)
            if (judgement.HitMargin == hitMargin)
                ++count;

        return count;
    }

    public static string ReplayFileName(Replay replay)
    {
        var time = DateTime.Now.ToString("yyyy.MM.dd-HH.mm");
        // var filteredArtist = FilterInvalidCharacters(replay.Metadata.Artist).Trim();
        // var filteredSong = FilterInvalidCharacters(replay.Metadata.Song).Trim();
        // var filteredAuthor = FilterInvalidCharacters(replay.Metadata.Author).Trim();
        // var folderName = $"{filteredArtist} - {filteredSong} - {filteredAuthor}".Trim();
        var pitch = replay.Metadata.Pitch;
        var xAccuracy = GetXAccuracy(replay) * 100;
        var startingProgress = replay.Metadata.StartingFloorId * 100 / replay.Metadata.TotalFloorCount;
        if (replay.Metadata.StartingFloorId != 0 && startingProgress == 0) startingProgress = 1;
        var endingProgress = (GetEndingFloorId(replay) + 1) * 100 / replay.Metadata.TotalFloorCount;
        var fileName = $"{time} ({pitch:0.00}x-{xAccuracy:0.00}%) [{startingProgress}%-{endingProgress}%]";
        // return Path.Combine(Settings.Instance.ReplayStorageLocation, folderName, fileName);
        var suffix = "";
        var count = 1;
        string? path = null;
        while (path is null || File.Exists(path))
        {
            path = Path.Combine(SettingsReplay.Instance.ReplayStorageLocation, fileName + suffix + ".ychreplaygz");
            ++count;
            suffix = $" ({count})";
        }

        return path;
    }

    public static Dictionary<int, int> CalculateKeyPressCounts(Replay replay)
    {
        Dictionary<int, int> keyCount = [];
        foreach (var keyEvent in replay.KeyEvents.Where(keyEvent => !keyEvent.IsKeyUp))
            keyCount[keyEvent.KeyCode] = keyCount.GetValueOrDefault(keyEvent.KeyCode, 0) + 1;
        return keyCount;
    }

    public static List<(int, int)> GetSortedKeyPressCounts(Replay replay)
    {
        var keyCount = CalculateKeyPressCounts(replay);
        var values = keyCount.ToList();
        values.Sort((x, y) => y.Value.CompareTo(x.Value));
        return values.Select(it => (it.Key, it.Value)).ToList();
    }
}