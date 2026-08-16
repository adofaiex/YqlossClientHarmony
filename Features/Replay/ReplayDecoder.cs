using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace YqlossClientHarmony.Features.Replay;

public static class ReplayDecoder
{
    private static void CheckMagicNumber(ulong magicNumber)
    {
        if (magicNumber != ReplayConstants.MagicNumber) throw new InvalidDataException("magic number mismatch");
    }

    private static int CheckVersion(int version, int currentVersion)
    {
        if (version <= 0) throw new InvalidDataException("version must be positive");
        if (version > currentVersion) throw new InvalidDataException("the replay is created by newer versions of YCH. please update your mod!");
        return version;
    }

    private static bool ProcessMetadata1(
        BinaryReader reader,
        ref Replay.MetadataType? metadata
    )
    {
        if (metadata is not null) throw new InvalidDataException("multiple metadata blocks");

        metadata = new Replay.MetadataType(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString(),
            (Difficulty)reader.ReadByte(),
            reader.ReadBoolean(),
            reader.ReadBoolean(),
            (HoldBehavior)reader.ReadByte(),
            (HitMarginLimit)reader.ReadByte(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        return false;
    }

    private static bool ProcessMetadata2(
        BinaryReader reader,
        ref DateTimeOffset? endTime,
        ref Replay.MetadataType? metadata
    )
    {
        if (metadata is not null) throw new InvalidDataException("multiple metadata blocks");

        metadata = new Replay.MetadataType(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString(),
            (Difficulty)reader.ReadByte(),
            reader.ReadBoolean(),
            reader.ReadBoolean(),
            (HoldBehavior)reader.ReadByte(),
            (HitMarginLimit)reader.ReadByte(),
            TakeFirst(ValidTime(), endTime = ValidTime()),
            FiniteDouble(),
            NonEmptyString(),
            NonEmptyString(),
            NonEmptyString(),
            FiniteDouble(),
            NonZeroInt(),
            null
        );

        return false;


        T TakeFirst<T>(T first, params object?[] _)
        {
            return first;
        }

        DateTimeOffset? ValidTime()
        {
            var value = reader.ReadInt64();
            return value == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(value);
        }

        double? FiniteDouble()
        {
            var value = reader.ReadDouble();
            return double.IsFinite(value) ? value : null;
        }

        string? NonEmptyString()
        {
            var value = reader.ReadString();
            return value.IsNullOrEmpty() ? null : value;
        }

        int? NonZeroInt()
        {
            var value = reader.ReadInt32();
            return value == 0 ? null : value;
        }
    }

    private static bool ProcessMetadata(
        BinaryReader reader,
        ref DateTimeOffset? endTime,
        ref Replay.MetadataType? metadata
    )
    {
        if (metadata is not null) throw new InvalidDataException("multiple metadata blocks");

        var version = CheckVersion(reader.ReadInt32(), ReplayConstants.MetadataFormatVersion);

        if (version < ReplayConstants.MetadataFormatVersion)
            return version switch
            {
                1 => ProcessMetadata1(reader, ref metadata),
                2 => ProcessMetadata2(reader, ref endTime, ref metadata),
                _ => throw new InvalidDataException($"illegal metadata format version {version}")
            };

        metadata = new Replay.MetadataType(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString(),
            (Difficulty)reader.ReadByte(),
            reader.ReadBoolean(),
            reader.ReadBoolean(),
            (HoldBehavior)reader.ReadByte(),
            (HitMarginLimit)reader.ReadByte(),
            TakeFirst(ValidTime(), endTime = ValidTime()),
            FiniteDouble(),
            NonEmptyString(),
            NonEmptyString(),
            NonEmptyString(),
            FiniteDouble(),
            NonZeroInt(),
            FiniteDouble()
        );

        return false;

        T TakeFirst<T>(T first, params object?[] _)
        {
            return first;
        }

        DateTimeOffset? ValidTime()
        {
            var value = reader.ReadInt64();
            return value == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(value);
        }

        double? FiniteDouble()
        {
            var value = reader.ReadDouble();
            return double.IsFinite(value) ? value : null;
        }

        string? NonEmptyString()
        {
            var value = reader.ReadString();
            return value.IsNullOrEmpty() ? null : value;
        }

        int? NonZeroInt()
        {
            var value = reader.ReadInt32();
            return value == 0 ? null : value;
        }
    }

    private static bool ProcessKeyEvents1(
        BinaryReader reader,
        ref List<Replay.KeyEventType>? keyEvents
    )
    {
        if (keyEvents is not null) throw new InvalidDataException("multiple key event blocks");

        var count = reader.ReadInt32();
        keyEvents = [];

        for (var i = 0; i < count; i++)
            keyEvents.Add(new Replay.KeyEventType(
                reader.ReadDouble(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadByte(),
                false,
                false,
                true
            ));

        return false;
    }

    private static bool ProcessKeyEvents(
        BinaryReader reader,
        ref List<Replay.KeyEventType>? keyEvents
    )
    {
        if (keyEvents is not null) throw new InvalidDataException("multiple key event blocks");

        var version = CheckVersion(reader.ReadInt32(), ReplayConstants.KeyEventFormatVersion);

        if (version < ReplayConstants.KeyEventFormatVersion)
            return version switch
            {
                1 => ProcessKeyEvents1(reader, ref keyEvents),
                _ => throw new InvalidDataException($"illegal key event format version {version}")
            };

        var count = reader.ReadInt32();
        keyEvents = [];

        for (var i = 0; i < count; i++)
            keyEvents.Add(new Replay.KeyEventType(
                reader.ReadDouble(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadByte(),
                reader.ReadBoolean(),
                reader.ReadBoolean()
            ));

        return false;
    }

    private static bool ProcessJudgements(
        BinaryReader reader,
        ref List<Replay.JudgementType>? judgements
    )
    {
        if (judgements is not null) throw new InvalidDataException("multiple judgement blocks");

        var version = CheckVersion(reader.ReadInt32(), ReplayConstants.JudgementFormatVersion);

        var count = reader.ReadInt32();
        judgements = [];

        for (var i = 0; i < count; i++)
            judgements.Add(new Replay.JudgementType(
                reader.ReadDouble(),
                (HitMargin)reader.ReadByte(),
                reader.ReadByte()
            ));

        return false;
    }


    private static bool ProcessCustomPayload(
        BinaryReader reader,
        Dictionary<string, byte[]> customPayloads
    )
    {
        var version = CheckVersion(reader.ReadInt32(), ReplayConstants.CustomPayloadFormatVersion);

        var ns = reader.ReadString();

        if (customPayloads.ContainsKey(ns))
            throw new InvalidDataException($"multiple custom payloads with name {ns}");

        var length = reader.ReadInt64();
        if (length is < 0 or > int.MaxValue)
            throw new InvalidDataException($"invalid custom payload length {length} for namespace {ns}");

        var data = reader.ReadBytes((int)length);
        customPayloads[ns] = data;

        return false;
    }

    private static bool WarnInvalidDataBlock(ulong magicNumber)
    {
        Main.Mod.Logger.Warning($"invalid data block magic number: {magicNumber}");
        return false;
    }

    private static void ProcessBlock(
        byte[] block,
        ref Replay.MetadataType? metadata,
        ref DateTimeOffset? endTime,
        ref List<Replay.KeyEventType>? keyEvents,
        ref List<Replay.JudgementType>? judgements,
        Dictionary<string, byte[]> customPayloads
    )
    {
        using var stream = new MemoryStream(block);
        var reader = new BinaryReader(stream);

        var magicNumber = reader.ReadUInt64();
        _ = magicNumber switch
        {
            ReplayConstants.MetadataMagicNumber => ProcessMetadata(reader, ref endTime, ref metadata),
            ReplayConstants.KeyEventMagicNumber => ProcessKeyEvents(reader, ref keyEvents),
            ReplayConstants.JudgementMagicNumber => ProcessJudgements(reader, ref judgements),
            ReplayConstants.CustomPayloadMagicNumber => ProcessCustomPayload(reader, customPayloads),
            _ => WarnInvalidDataBlock(magicNumber)
        };
    }

    public static Replay ParseReplay(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);

        CheckMagicNumber(reader.ReadUInt64());
        var version = CheckVersion(reader.ReadInt32(), ReplayConstants.FormatVersion);

        var blockCount = reader.ReadUInt32();
        Replay.MetadataType? metadata = null;
        DateTimeOffset? endTime = null;
        List<Replay.KeyEventType>? keyEvents = null;
        List<Replay.JudgementType>? judgements = null;
        Dictionary<string, byte[]> customPayloads = [];

        for (var i = 0u; i < blockCount; i++)
        {
            var blockSize = reader.ReadUInt64();
            ProcessBlock(
                reader.ReadBytes((int)blockSize),
                ref metadata,
                ref endTime,
                ref keyEvents,
                ref judgements,
                customPayloads
            );
        }

        if (metadata is null) throw new InvalidDataException("missing metadata");
        if (keyEvents is null) throw new InvalidDataException("missing key events");

        var replay = new Replay(metadata.Value)
        {
            EndTime = endTime
        };
        replay.KeyEvents.AddRange(keyEvents);
        replay.Judgements.AddRange(judgements ?? []);

        foreach (var (ns, data) in customPayloads)
            replay.CustomPayloads.Add(ns, data);

        return replay;
    }

    public static Replay? ReadCompressedReplay(string path)
    {
        try
        {
            byte[] decompressedBytes;

            {
                using var fileStream = new FileStream(path, FileMode.Open);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var memoryStream = new MemoryStream();
                gzipStream.CopyTo(memoryStream);
                decompressedBytes = memoryStream.ToArray();
            }

            return ParseReplay(decompressedBytes);
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Error($"failed to read replay: {exception}");
            return null;
        }
    }
}