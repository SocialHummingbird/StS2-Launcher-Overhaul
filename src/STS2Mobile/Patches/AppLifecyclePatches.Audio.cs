using System;
using System.Reflection;

namespace STS2Mobile.Patches;

internal static partial class AppLifecyclePatches
{
    private static void MuteFmodAudio()
    {
        try
        {
            SetFmodMasterVolume(0f);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Mute FMOD failed: {ex.Message}");
        }
    }

    private static void RestoreFmodAudio()
    {
        try
        {
            var audioManager = GetFmodAudioManager();
            var saveManager = MegaCrit.Sts2.Core.Saves.SaveManager.Instance;
            if (audioManager == null || saveManager == null)
                return;

            var settings = saveManager.SettingsSave;
            var masterVolume = (float)
                settings
                    .GetType()
                    .GetProperty("VolumeMaster", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(settings);
            SetFmodMasterVolume(audioManager, masterVolume);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Restore audio failed: {ex.Message}");
        }
    }

    private static void SetFmodMasterVolume(float volume)
    {
        var audioManager = GetFmodAudioManager();
        if (audioManager == null)
            return;

        SetFmodMasterVolume(audioManager, volume);
    }

    private static void SetFmodMasterVolume(object audioManager, float volume)
    {
        audioManager
            .GetType()
            .GetMethod("SetMasterVol", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(audioManager, new object[] { volume });
    }

    private static object GetFmodAudioManager()
    {
        var nGameInstance = MegaCrit.Sts2.Core.Nodes.NGame.Instance;
        return nGameInstance == null
            ? null
            : typeof(MegaCrit.Sts2.Core.Nodes.NGame)
                .GetProperty("AudioManager", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(nGameInstance);
    }
}
