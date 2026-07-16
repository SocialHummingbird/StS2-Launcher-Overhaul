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
		String normalizedPreference = normalize(preference);
		if (safeLaunch) {
			return new Plan(normalizedPreference, AUTO, true, null, null);
		}

		switch (normalizedPreference) {
			case VULKAN:
				return new Plan(VULKAN, VULKAN, false, "vulkan", "mobile");
			case OPENGL:
				return new Plan(OPENGL, OPENGL, false, "opengl3", "gl_compatibility");
			default:
				return new Plan(AUTO, AUTO, false, null, null);
		}
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
		private final String renderingDriver;
		private final String renderingMethod;

		private Plan(
			String preference,
			String effectiveMode,
			boolean safeLaunchOverride,
			String renderingDriver,
			String renderingMethod
		) {
			this.preference = preference;
			this.effectiveMode = effectiveMode;
			this.safeLaunchOverride = safeLaunchOverride;
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

		String description() {
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
