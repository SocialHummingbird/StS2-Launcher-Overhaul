package org.fmod;

import android.content.Context;
import android.content.pm.PackageManager;
import android.content.res.AssetManager;
import android.media.AudioDeviceInfo;
import android.os.Build;

public final class FMOD {
	private static boolean initialized;
	private static Context context;

	static {
		System.loadLibrary("fmod");
		System.loadLibrary("sts2fmodbridge");
	}

	private FMOD() {
	}

	public static void init(Context context) {
		FMOD.context = context == null ? null : context.getApplicationContext();
		nativeInit(context);
		initialized = true;
	}

	public static void close() {
		try {
			nativeClose();
		} finally {
			initialized = false;
			context = null;
		}
	}

	public static boolean checkInit() {
		return initialized;
	}

	public static AssetManager getAssetManager() {
		Context activeContext = context;
		return activeContext == null ? null : activeContext.getAssets();
	}

	public static boolean supportsLowLatency() {
		Context activeContext = context;
		return activeContext != null
			&& activeContext.getPackageManager().hasSystemFeature(PackageManager.FEATURE_AUDIO_LOW_LATENCY);
	}

	public static boolean supportsAAudio() {
		return Build.VERSION.SDK_INT >= Build.VERSION_CODES.O;
	}

	public static AudioDeviceInfo[] getAudioDevices(int flags) {
		return AudioDevice.getAudioDevices(flags);
	}

	private static native void nativeInit(Context context);

	private static native void nativeClose();
}
