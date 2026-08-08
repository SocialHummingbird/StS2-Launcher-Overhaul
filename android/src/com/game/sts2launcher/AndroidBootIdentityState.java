package com.game.sts2launcher;

final class AndroidBootIdentityState {
	private static final float MIN_SCALE = 0.0f;
	private static final float MAX_SCALE = 2.0f;

	private float godotOpacity;
	private float godotScale;
	private float identityOpacity;
	private float identityScale;
	private float cyanOpacity;
	private float orangeOpacity;
	private float resolvedOpacity;
	private float wordmarkOpacity;
	private float cyanOffsetX;
	private float cyanOffsetY;
	private float orangeOffsetX;
	private float orangeOffsetY;
	private float revealProgress;
	private float pulseStrength;

	AndroidBootIdentityState() {
		reset();
	}

	void reset() {
		godotOpacity = 1.0f;
		godotScale = 1.0f;
		identityOpacity = 0.0f;
		identityScale = 0.72f;
		cyanOpacity = 0.0f;
		orangeOpacity = 0.0f;
		resolvedOpacity = 1.0f;
		wordmarkOpacity = 1.0f;
		cyanOffsetX = 0.0f;
		cyanOffsetY = 0.0f;
		orangeOffsetX = 0.0f;
		orangeOffsetY = 0.0f;
		revealProgress = 1.0f;
		pulseStrength = 0.0f;
	}

	float godotOpacity() {
		return godotOpacity;
	}

	boolean setGodotOpacity(float value) {
		float normalized = unit(value);
		if (same(godotOpacity, normalized)) {
			return false;
		}
		godotOpacity = normalized;
		return true;
	}

	float godotScale() {
		return godotScale;
	}

	boolean setGodotScale(float value) {
		float normalized = scale(value);
		if (same(godotScale, normalized)) {
			return false;
		}
		godotScale = normalized;
		return true;
	}

	float identityOpacity() {
		return identityOpacity;
	}

	boolean setIdentityOpacity(float value) {
		float normalized = unit(value);
		if (same(identityOpacity, normalized)) {
			return false;
		}
		identityOpacity = normalized;
		return true;
	}

	float identityScale() {
		return identityScale;
	}

	boolean setIdentityScale(float value) {
		float normalized = scale(value);
		if (same(identityScale, normalized)) {
			return false;
		}
		identityScale = normalized;
		return true;
	}

	float cyanOpacity() {
		return cyanOpacity;
	}

	boolean setCyanOpacity(float value) {
		float normalized = unit(value);
		if (same(cyanOpacity, normalized)) {
			return false;
		}
		cyanOpacity = normalized;
		return true;
	}

	float orangeOpacity() {
		return orangeOpacity;
	}

	boolean setOrangeOpacity(float value) {
		float normalized = unit(value);
		if (same(orangeOpacity, normalized)) {
			return false;
		}
		orangeOpacity = normalized;
		return true;
	}

	float resolvedOpacity() {
		return resolvedOpacity;
	}

	boolean setResolvedOpacity(float value) {
		float normalized = unit(value);
		if (same(resolvedOpacity, normalized)) {
			return false;
		}
		resolvedOpacity = normalized;
		return true;
	}

	float wordmarkOpacity() {
		return wordmarkOpacity;
	}

	boolean setWordmarkOpacity(float value) {
		float normalized = unit(value);
		if (same(wordmarkOpacity, normalized)) {
			return false;
		}
		wordmarkOpacity = normalized;
		return true;
	}

	float cyanOffsetX() {
		return cyanOffsetX;
	}

	boolean setCyanOffsetX(float value) {
		float normalized = finiteOrZero(value);
		if (same(cyanOffsetX, normalized)) {
			return false;
		}
		cyanOffsetX = normalized;
		return true;
	}

	float cyanOffsetY() {
		return cyanOffsetY;
	}

	boolean setCyanOffsetY(float value) {
		float normalized = finiteOrZero(value);
		if (same(cyanOffsetY, normalized)) {
			return false;
		}
		cyanOffsetY = normalized;
		return true;
	}

	float orangeOffsetX() {
		return orangeOffsetX;
	}

	boolean setOrangeOffsetX(float value) {
		float normalized = finiteOrZero(value);
		if (same(orangeOffsetX, normalized)) {
			return false;
		}
		orangeOffsetX = normalized;
		return true;
	}

	float orangeOffsetY() {
		return orangeOffsetY;
	}

	boolean setOrangeOffsetY(float value) {
		float normalized = finiteOrZero(value);
		if (same(orangeOffsetY, normalized)) {
			return false;
		}
		orangeOffsetY = normalized;
		return true;
	}

	float revealProgress() {
		return revealProgress;
	}

	boolean setRevealProgress(float value) {
		float normalized = unit(value);
		if (same(revealProgress, normalized)) {
			return false;
		}
		revealProgress = normalized;
		return true;
	}

	float pulseStrength() {
		return pulseStrength;
	}

	boolean setPulseStrength(float value) {
		float normalized = unit(value);
		if (same(pulseStrength, normalized)) {
			return false;
		}
		pulseStrength = normalized;
		return true;
	}

	private static float unit(float value) {
		return clamp(finiteOrZero(value), 0.0f, 1.0f);
	}

	private static float scale(float value) {
		return clamp(finiteOrZero(value), MIN_SCALE, MAX_SCALE);
	}

	private static float finiteOrZero(float value) {
		return Float.isFinite(value) ? value : 0.0f;
	}

	private static float clamp(float value, float minimum, float maximum) {
		return Math.max(minimum, Math.min(maximum, value));
	}

	private static boolean same(float first, float second) {
		return Float.compare(first, second) == 0;
	}
}
