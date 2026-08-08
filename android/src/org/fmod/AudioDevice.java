package org.fmod;

import android.content.Context;
import android.media.AudioDeviceInfo;
import android.media.AudioManager;
import android.os.Build;

public final class AudioDevice {
	private static Context context;

	private AudioDevice() {
	}

	public static void setContext(Context value) {
		context = value;
	}

	public static AudioDeviceInfo[] getAudioDevices(int flags) {
		Context activeContext = context;
		if (activeContext == null || Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
			return new AudioDeviceInfo[0];
		}

		AudioManager audioManager = (AudioManager) activeContext.getSystemService(Context.AUDIO_SERVICE);
		if (audioManager == null) {
			return new AudioDeviceInfo[0];
		}

		return audioManager.getDevices(flags);
	}
}
