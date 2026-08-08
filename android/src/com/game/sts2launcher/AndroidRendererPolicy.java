package com.game.sts2launcher;

import java.util.List;
import java.util.Locale;

final class AndroidRendererPolicy {
	static final String AUTO = "auto";
	static final String VULKAN = "vulkan";
	static final String OPENGL = "opengl";

	private AndroidRendererPolicy() {
	}

	static Plan resolve(String preference, boolean safeLaunch) {
		return resolve(preference, safeLaunch, "");
	}

	static Plan resolve(String preference, boolean safeLaunch, String graphicsDeviceEvidence) {
		String normalizedPreference = normalize(preference);
		boolean powerVrCompatibility = isPowerVr(graphicsDeviceEvidence);
		if (powerVrCompatibility) {
			return new Plan(
				normalizedPreference,
				OPENGL,
				safeLaunch,
				true,
				"opengl3",
				"gl_compatibility"
			);
		}

		if (safeLaunch) {
			return new Plan(normalizedPreference, AUTO, true, false, null, null);
		}

		switch (normalizedPreference) {
			case VULKAN:
				return new Plan(VULKAN, VULKAN, false, false, "vulkan", "mobile");
			case OPENGL:
				return new Plan(OPENGL, OPENGL, false, false, "opengl3", "gl_compatibility");
			default:
				return new Plan(AUTO, AUTO, false, false, null, null);
		}
	}

	static boolean isPowerVr(String evidence) {
		if (evidence == null) {
			return false;
		}

		String normalized = evidence.toLowerCase(Locale.ROOT);
		if (normalized.contains("powervr compatibility required: true")) {
			return true;
		}
		if (normalized.contains("powervr compatibility required: false")) {
			return false;
		}

		return normalized.contains("powervr")
			|| normalized.contains("imgtec")
			|| normalized.contains("imagination technologies");
	}

	static String normalize(String value) {
		if (value == null) {
			return AUTO;
		}

		String normalized = value.trim().toLowerCase(Locale.ROOT);
		return VULKAN.equals(normalized) || OPENGL.equals(normalized)
			? normalized
			: AUTO;
	}

	static final class Plan {
		private final String preference;
		private final String effectiveMode;
		private final boolean safeLaunchOverride;
		private final boolean powerVrCompatibility;
		private final String renderingDriver;
		private final String renderingMethod;

		private Plan(
			String preference,
			String effectiveMode,
			boolean safeLaunchOverride,
			boolean powerVrCompatibility,
			String renderingDriver,
			String renderingMethod
		) {
			this.preference = preference;
			this.effectiveMode = effectiveMode;
			this.safeLaunchOverride = safeLaunchOverride;
			this.powerVrCompatibility = powerVrCompatibility;
			this.renderingDriver = renderingDriver;
			this.renderingMethod = renderingMethod;
		}

		void appendCommandLine(List<String> commands) {
			if (renderingDriver == null || renderingMethod == null) {
				return;
			}

			commands.add("--rendering-driver");
			commands.add(renderingDriver);
			commands.add("--rendering-method");
			commands.add(renderingMethod);
		}

		String preference() {
			return preference;
		}

		String effectiveMode() {
			return effectiveMode;
		}

		boolean safeLaunchOverride() {
			return safeLaunchOverride;
		}

		boolean powerVrCompatibility() {
			return powerVrCompatibility;
		}

		String description() {
			if (powerVrCompatibility) {
				return "PowerVR compatibility forces OpenGL to preserve touch input";
			}
			if (safeLaunchOverride) {
				return "Safe Start uses the unforced project renderer";
			}
			switch (effectiveMode) {
				case VULKAN:
					return "Vulkan Mobile forced by launcher preference";
				case OPENGL:
					return "OpenGL Compatibility forced by launcher preference";
				default:
					return "Project renderer used without launcher override";
			}
		}
	}
}
