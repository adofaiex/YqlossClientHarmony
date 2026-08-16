using System;
using System.Collections.Generic;

namespace YqlossClientHarmony.Features.Replay;

public class Replay(Replay.MetadataType metadata)
{
    public DateTimeOffset? EndTime = null;
    public MetadataType Metadata { get; } = metadata;
    public List<KeyEventType> KeyEvents { get; } = [];
    public List<JudgementType> Judgements { get; } = [];
    public Dictionary<string, byte[]> CustomPayloads { get; } = [];

    public readonly struct MetadataType(
        int startingFloorId,
        // data below don't affect playing
        int totalFloorCount,
        string artist,
        string song,
        string author,
        Difficulty difficulty,
        bool noFailMode,
        bool useAsyncInput,
        HoldBehavior holdBehavior,
        HitMarginLimit hitMarginLimit,
        DateTimeOffset? startTime,
        double? recordingOffset,
        string? levelPath,
        string? ychVersion,
        string? modList,
        double? inputOffset,
        int? audioBufferSize,
        double? pitch
    )
    {
        public readonly int StartingFloorId = startingFloorId;
        public readonly int TotalFloorCount = totalFloorCount;
        public readonly string Artist = artist;
        public readonly string Song = song;
        public readonly string Author = author;
        public readonly Difficulty Difficulty = difficulty;
        public readonly bool NoFailMode = noFailMode;
        public readonly bool UseAsyncInput = useAsyncInput;
        public readonly HoldBehavior HoldBehavior = holdBehavior;
        public readonly HitMarginLimit HitMarginLimit = hitMarginLimit;
        public readonly DateTimeOffset? StartTime = startTime;
        public readonly double? RecordingOffset = recordingOffset;
        public readonly string? LevelPath = levelPath;
        public readonly string? YchVersion = ychVersion;
        public readonly string? ModList = modList;
        public readonly double? InputOffset = inputOffset;
        public readonly int? AudioBufferSize = audioBufferSize;
        public readonly double? Pitch = pitch;
    }

    public readonly struct KeyEventType(
        double songSeconds,
        int keyCode,
        bool isKeyUp,
        int floorIdIncrement,
        bool isAutoFloor,
        bool isInputLocked,
        // data below are not serialized
        bool version1 = false
    )
    {
        public readonly double SongSeconds = songSeconds;
        public readonly int KeyCode = keyCode;
        public readonly bool IsKeyUp = isKeyUp;
        public readonly int FloorIdIncrement = floorIdIncrement;
        public readonly bool IsAutoFloor = isAutoFloor;
        public readonly bool IsInputLocked = isInputLocked;
        public readonly bool Version1 = version1;
    }

    public readonly struct JudgementType(
        double errorMeter,
        HitMargin hitMargin,
        int floorIdIncrement
    )
    {
        public readonly double ErrorMeter = errorMeter;
        public readonly HitMargin HitMargin = hitMargin;
        public readonly int FloorIdIncrement = floorIdIncrement;
    }
}