namespace YqlossClientHarmony.Features.Replay;

public static class ReplayConstants
{
    public const ulong MagicNumber = 0x73736F6C71592107;
    public const ulong MetadataMagicNumber = 0x617461646174654D;
    public const ulong KeyEventMagicNumber = 0x746E65764579654B;
    public const ulong JudgementMagicNumber = 0x6E656D656764754A;
    public const ulong CustomPayloadMagicNumber = 0x61506D6F74737543;
    public const int FormatVersion = 1;
    public const int MetadataFormatVersion = 3;
    public const int KeyEventFormatVersion = 2;
    public const int JudgementFormatVersion = 1;
    public const int CustomPayloadFormatVersion = 1;
    public const HitMargin HoldPreMiss = (HitMargin)127;
    public const HitMargin HoldExtraPress = (HitMargin)126;
}