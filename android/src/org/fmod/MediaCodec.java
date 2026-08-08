package org.fmod;

public final class MediaCodec {
	private MediaCodec() {
	}

	public static native int fmodGetSize(long handle);

	public static native int fmodReadAt(long handle, byte[] buffer, int offset, int size);
}
