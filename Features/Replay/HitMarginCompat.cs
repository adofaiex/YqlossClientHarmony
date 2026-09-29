using System;
using System.Collections.Generic;
using System.Reflection;

namespace YqlossClientHarmony.Features.Replay;

// ADOFAI 3.4.0 splits HitMargin.Perfect into PerfectMinus / XPerfect / PerfectPlus,
// adds Midspin / FailedFloor and shifts every value after Perfect.
// this class maps hit margins between old and new versions and keeps replay files compatible.
public static class HitMarginCompat
{
    // the layout of 3.4.0, which replay files always use
    private static readonly string[] CanonicalNames =
    [
        "TooEarly",
        "VeryEarly",
        "EarlyPerfect",
        "PerfectMinus",
        "XPerfect",
        "PerfectPlus",
        "LatePerfect",
        "VeryLate",
        "TooLate",
        "Multipress",
        "FailMiss",
        "FailOverload",
        "Auto",
        "OverPress",
        "Midspin",
        "FailedFloor"
    ];

    // the layout before 3.4.0, which is used to read old replay files
    private static readonly string[] LegacyNames =
    [
        "TooEarly",
        "VeryEarly",
        "EarlyPerfect",
        "Perfect",
        "LatePerfect",
        "VeryLate",
        "TooLate",
        "Multipress",
        "FailMiss",
        "FailOverload",
        "Auto",
        "OverPress"
    ];

    private static readonly Dictionary<string, HitMargin> ResolvedNames = [];

    private static readonly HitMargin[] CanonicalValues = new HitMargin[CanonicalNames.Length];

    private static readonly HitMargin[] LegacyValues = new HitMargin[LegacyNames.Length];

    static HitMarginCompat()
    {
        HasSplitPerfect = Enum.IsDefined(typeof(HitMargin), "XPerfect");

        // 3.4.0 moves the weights of x-accuracy into HitMarginHelper
        OfficialWeights = (
            typeof(HitMargin)
                .Assembly
                .GetType("HitMarginHelper")
                ?.GetField("PlayerHitMarginWeights", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null)
        ) as IReadOnlyDictionary<HitMargin, double>;

        PerfectMinus = Resolve("PerfectMinus");
        XPerfect = Resolve("XPerfect");
        PerfectPlus = Resolve("PerfectPlus");

        // the Perfect of old versions is closest to XPerfect on 3.4.0
        Perfect = Resolve("Perfect", XPerfect);

        for (var i = 0; i < CanonicalNames.Length; i++)
            CanonicalValues[i] = Resolve(CanonicalNames[i], i switch
            {
                // perfects that only exist on 3.4.0 are all counted as Perfect on old versions
                3 or 4 or 5 => Perfect,
                14 => Resolve("Auto"),
                15 => Resolve("FailMiss"),
                _ => default
            });

        for (var i = 0; i < LegacyNames.Length; i++)
            LegacyValues[i] = Resolve(LegacyNames[i], LegacyNames[i] == "Perfect" ? XPerfect : default);
    }

    // whether the running game has the split perfects of 3.4.0
    public static bool HasSplitPerfect { get; }

    // the official weight table of x-accuracy, which only 3.4.0 has
    public static IReadOnlyDictionary<HitMargin, double>? OfficialWeights { get; }

    // the Perfect of old versions, which falls back to XPerfect on 3.4.0
    public static HitMargin Perfect { get; }

    public static HitMargin PerfectMinus { get; }

    public static HitMargin XPerfect { get; }

    public static HitMargin PerfectPlus { get; }

    public static byte ToSerialized(HitMargin hitMargin)
    {
        var raw = (int)hitMargin;
        if (raw is 126 or 127) return (byte)raw;

        var name = hitMargin.ToString();
        if (name == "Perfect") name = "XPerfect";

        var index = Array.IndexOf(CanonicalNames, name);
        return index < 0 ? (byte)0 : (byte)index;
    }

    public static HitMargin FromSerialized(int version, byte value)
    {
        var raw = (int)value;
        if (raw is 126 or 127) return (HitMargin)raw;

        var values = version <= 1 ? LegacyValues : CanonicalValues;
        return value < values.Length ? values[value] : default;
    }

    private static HitMargin Resolve(string name, HitMargin fallback = default)
    {
        if (ResolvedNames.TryGetValue(name, out var resolved)) return resolved;

        resolved = Enum.IsDefined(typeof(HitMargin), name)
            ? (HitMargin)Enum.Parse(typeof(HitMargin), name)
            : fallback;

        ResolvedNames[name] = resolved;
        return resolved;
    }
}
