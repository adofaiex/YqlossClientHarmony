using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Forms;

namespace YqlossClientHarmony.Features.Replay;

public static class ReplayEncoder
{
    private static byte[] EncodeMetadata(Replay replay)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        var metadata = replay.Metadata;

        writer.Write(ReplayConstants.MetadataMagicNumber);
        writer.Write(ReplayConstants.MetadataFormatVersion);
        writer.Write(metadata.StartingFloorId);
        writer.Write(metadata.TotalFloorCount);
        writer.Write(metadata.Artist);
        writer.Write(metadata.Song);
        writer.Write(metadata.Author);
        writer.Write((byte)metadata.Difficulty);
        writer.Write(metadata.NoFailMode);
        writer.Write(metadata.UseAsyncInput);
        writer.Write((byte)metadata.HoldBehavior);
        writer.Write((byte)metadata.HitMarginLimit);
        writer.Write((metadata.StartTime ?? DateTimeOffset.FromUnixTimeMilliseconds(0)).ToUnixTimeMilliseconds());
        writer.Write((replay.EndTime ?? DateTimeOffset.FromUnixTimeMilliseconds(0)).ToUnixTimeMilliseconds());
        writer.Write(metadata.RecordingOffset ?? double.NaN);
        writer.Write(metadata.LevelPath ?? "");
        writer.Write(metadata.YchVersion ?? "");
        writer.Write(metadata.ModList ?? "");
        writer.Write(metadata.InputOffset ?? double.NaN);
        writer.Write(metadata.AudioBufferSize ?? 0);
        writer.Write(metadata.Pitch ?? 0);

        writer.Close();
        return stream.ToArray();
    }

    private static byte[] EncodeKeyEvents(Replay replay)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(ReplayConstants.KeyEventMagicNumber);
        writer.Write(ReplayConstants.KeyEventFormatVersion);
        writer.Write(replay.KeyEvents.Count);

        foreach (var keyEvent in replay.KeyEvents)
        {
            // 0-8
            writer.Write(keyEvent.SongSeconds);
            // 8-12
            writer.Write(keyEvent.KeyCode);
            // 12-13
            writer.Write(keyEvent.IsKeyUp);
            // 13-14
            writer.Write((byte)keyEvent.FloorIdIncrement);
            // 14-15
            writer.Write(keyEvent.IsAutoFloor);
            // 15-16
            writer.Write(keyEvent.IsInputLocked);
        }

        writer.Close();
        return stream.ToArray();
    }

    private static byte[] EncodeJudgements(Replay replay)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(ReplayConstants.JudgementMagicNumber);
        writer.Write(ReplayConstants.JudgementFormatVersion);
        writer.Write(replay.Judgements.Count);

        foreach (var judgement in replay.Judgements)
        {
            // 0-8
            writer.Write(judgement.ErrorMeter);
            // 8-9
            writer.Write((byte)judgement.HitMargin);
            // 9-10
            writer.Write((byte)judgement.FloorIdIncrement);
        }

        writer.Close();
        return stream.ToArray();
    }

    public static byte[] EncodeCustomPayload(string ns, byte[] data)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(ReplayConstants.CustomPayloadMagicNumber);
        writer.Write(ReplayConstants.CustomPayloadFormatVersion);
        writer.Write(ns);
        writer.Write(data.LongLength);
        writer.Write(data);

        writer.Close();
        return stream.ToArray();
    }

    public static byte[] Encode(Replay replay)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(ReplayConstants.MagicNumber);
        writer.Write(ReplayConstants.FormatVersion);

        List<byte[]> blocks =
        [
            EncodeMetadata(replay),
            EncodeKeyEvents(replay),
            EncodeJudgements(replay)
        ];

        foreach (var (ns, data) in replay.CustomPayloads)
            blocks.Add(EncodeCustomPayload(ns, data));

        writer.Write(blocks.Count);
        blocks.ForEach(WriteBlock);

        writer.Close();
        return stream.ToArray();

        void WriteBlock(byte[] block)
        {
            writer.Write(block.LongLength);
            writer.Write(block);
        }
    }

    public static void CompressAndSaveAs(Replay replay, string path)
    {
        var data = Encode(replay);
        try
        {
            using var fileStream = new FileStream(path, FileMode.Create);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
            gzipStream.Write(data);
        }
        catch (Exception exception)
        {
            Main.Mod.Logger.Error($"failed to save replay as {path}");
            Main.Mod.Logger.Error($"{exception}");
            Main.Mod.Logger.Error("replay binary data are as follow, in base64 format:");
            Main.Mod.Logger.Error($"{Convert.ToBase64String(data)}");
            MessageBox.Show(
                I18N.Translate("Dialog.Replay.SaveFailure.Text"),
                I18N.Translate("Dialog.Replay.SaveFailure.Title")
            );
        }
    }

    public static void LaunchCompressAndSaveAs(Replay replay, string path)
    {
        new Thread(() =>
        {
            try
            {
                CompressAndSaveAs(replay, path);
                Main.Mod.Logger.Log($"successfully saved replay as {path}");
            }
            catch (Exception exception)
            {
                Main.Mod.Logger.Error($"failed to save replay as {path}");
                Main.Mod.Logger.Error($"{exception}");
                MessageBox.Show(
                    I18N.Translate("Dialog.Replay.EncodeFailure.Text"),
                    I18N.Translate("Dialog.Replay.EncodeFailure.Title")
                );
            }
        }) { IsBackground = false }.Start();
    }
}