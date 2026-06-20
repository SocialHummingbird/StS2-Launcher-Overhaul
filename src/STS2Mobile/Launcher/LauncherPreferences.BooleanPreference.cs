using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private readonly struct BooleanPreference
    {
        internal BooleanPreference(
            string key,
            Func<bool> defaultValue,
            Action<bool> apply,
            Action<bool>? beforeSave = null
        )
        {
            Storage = new PreferenceFile(key);
            DefaultValue = defaultValue;
            Apply = apply;
            BeforeSave = beforeSave;
        }

        private PreferenceFile Storage { get; }
        private Func<bool> DefaultValue { get; }
        private Action<bool> Apply { get; }
        private Action<bool>? BeforeSave { get; }

        internal bool Read()
            => Storage.ReadBoolean(DefaultValue());

        internal bool LoadAndApply()
        {
            var enabled = Read();
            Apply(enabled);
            return enabled;
        }

        internal void Save(bool enabled)
        {
            BeforeSave?.Invoke(enabled);
            Apply(enabled);
            Storage.WriteBoolean(enabled);
        }
    }
}
