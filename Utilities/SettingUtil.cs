namespace YqlossClientHarmony.Utilities;

public static class SettingUtil
{
    public static SettingChangeDetector Save
    {
        get => SettingChangeDetector.Instance;
        set { }
    }

    public class SettingChangeDetector
    {
        private SettingChangeDetector()
        {
        }

        public static SettingChangeDetector Instance { get; } = new();

        public static SettingChangeDetector operator |(SettingChangeDetector instance, object? value)
        {
            if (value is null) return instance;
            Main.Settings.Save(Main.Mod);
            PatchManager.UpdateAllPatches();
            return instance;
        }
    }
}