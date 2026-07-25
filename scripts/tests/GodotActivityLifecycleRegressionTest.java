package com.game.sts2launcher;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public final class GodotActivityLifecycleRegressionTest {
	private static final String ON_CREATE_SIGNATURE =
		"public void onCreate(Bundle savedInstanceState)";
	private static final String SUPER_ON_CREATE =
		"super.onCreate(savedInstanceState);";

	public static void main(String[] args) throws Exception {
		if (args.length != 1) {
			throw new IllegalArgumentException(
				"Expected the path to GodotApp.java."
			);
		}

		verifyDetectorRejectsDoublePreparationFailure();
		verifyDetectorRejectsDuplicateSuperclassCalls();
		verifyDetectorRejectsPreparationWithoutEarlyReturn();
		verifyDetectorAcceptsSuperclassFirst();

		Path godotAppPath = Path.of(args[0]);
		String godotAppSource = Files.readString(
			godotAppPath,
			StandardCharsets.UTF_8
		);
		Violation violation = findLifecycleViolation(godotAppSource);
		if (violation != null) {
			throw new AssertionError(
				"GodotApp lifecycle regression at line "
					+ violation.line()
					+ ": "
					+ violation.message()
			);
		}

		System.out.println("Godot activity lifecycle regression tests passed.");
	}

	private static void verifyDetectorRejectsDoublePreparationFailure() {
		String vulnerableSource = """
			public void onCreate(Bundle savedInstanceState) {
				try {
					setupAssemblies();
				} catch (RuntimeException firstFailure) {
					resetAssemblyCacheState();
					try {
						setupAssemblies();
					} catch (RuntimeException secondFailure) {
						showNativeFailure();
						return;
					}
				}
				super.onCreate(savedInstanceState);
			}
			""";

		Violation violation = findLifecycleViolation(vulnerableSource);
		if (violation == null) {
			throw new AssertionError(
				"Detector accepted a double assembly-preparation failure "
					+ "that returns before super.onCreate()."
			);
		}
		if (!violation.message().contains("assembly-preparation failure")) {
			throw new AssertionError(
				"Detector did not identify the assembly-preparation failure path: "
					+ violation.message()
			);
		}
	}

	private static void verifyDetectorRejectsDuplicateSuperclassCalls() {
		String vulnerableSource = """
			public void onCreate(Bundle savedInstanceState) {
				super.onCreate(savedInstanceState);
				super.onCreate(savedInstanceState);
			}
			""";

		Violation violation = findLifecycleViolation(vulnerableSource);
		if (
			violation == null
				|| !violation.message().contains("exactly once")
		) {
			throw new AssertionError(
				"Detector accepted duplicate GodotActivity.super.onCreate() calls."
			);
		}
	}

	private static void verifyDetectorRejectsPreparationWithoutEarlyReturn() {
		String vulnerableSource = """
			public void onCreate(Bundle savedInstanceState) {
				AndroidAssemblyBootstrapper assemblyBootstrapper = createBootstrapper();
				assemblyBootstrapper.prepare();
				super.onCreate(savedInstanceState);
			}
			""";

		Violation violation = findLifecycleViolation(vulnerableSource);
		if (
			violation == null
				|| !violation.message().contains("must not perform assembly preparation")
		) {
			throw new AssertionError(
				"Detector accepted assembly preparation inside GodotApp.onCreate()."
			);
		}
	}

	private static void verifyDetectorAcceptsSuperclassFirst() {
		String safeSource = """
			public void onCreate(Bundle savedInstanceState) {
				super.onCreate(savedInstanceState);
				if (shouldClose()) {
					return;
				}
			}
			""";

		Violation violation = findLifecycleViolation(safeSource);
		if (violation != null) {
			throw new AssertionError(
				"Detector rejected a return after super.onCreate(): "
					+ violation.message()
			);
		}
	}

	private static Violation findLifecycleViolation(String source) {
		String code = maskCommentsAndLiterals(source);
		int signatureIndex = code.indexOf(ON_CREATE_SIGNATURE);
		if (signatureIndex < 0) {
			throw new AssertionError("Could not locate GodotApp.onCreate().");
		}

		int bodyStart = code.indexOf('{', signatureIndex);
		if (bodyStart < 0) {
			throw new AssertionError("Could not locate GodotApp.onCreate() body.");
		}
		int bodyEnd = findMatchingBrace(code, bodyStart);
		String body = code.substring(bodyStart + 1, bodyEnd);

		int superCallCount = countOccurrences(body, SUPER_ON_CREATE);
		if (superCallCount == 0) {
			return new Violation(
				lineNumber(source, bodyStart),
				"GodotActivity.onCreate() does not call super.onCreate()."
			);
		}
		if (superCallCount != 1) {
			return new Violation(
				lineNumber(source, bodyStart),
				"GodotActivity.onCreate() must call super.onCreate() exactly once; "
					+ "found " + superCallCount + " calls."
			);
		}

		int superIndex = body.indexOf(SUPER_ON_CREATE);
		int returnIndex = findReturnStatement(body, 0, superIndex);
		if (returnIndex >= 0) {
			String beforeReturn = body.substring(0, returnIndex);
			int preparationAttempts = countOccurrences(
				beforeReturn,
				"setupAssemblies();"
			);
			boolean preparesWithBootstrapper =
				beforeReturn.contains("assemblyBootstrapper.prepare();");
			boolean routesToNativeFailure =
				beforeReturn.contains("showNativeFailure(");
			String path;
			if (preparationAttempts >= 2 && routesToNativeFailure) {
				path = "The double assembly-preparation failure path";
			} else if (preparesWithBootstrapper && routesToNativeFailure) {
				path = "The assembly-preparation failure path";
			} else {
				path = "A startup path";
			}

			return new Violation(
				lineNumber(source, bodyStart + 1 + returnIndex),
				path
					+ " returns before GodotActivity.super.onCreate(), "
					+ "which causes Android SuperNotCalledException."
			);
		}

		if (
			body.contains("setupAssemblies();")
				|| body.contains("assemblyBootstrapper.prepare();")
				|| body.contains("AndroidAssemblyBootstrapper")
		) {
			return new Violation(
				lineNumber(source, bodyStart),
				"GodotApp.onCreate() must not perform assembly preparation; "
					+ "LauncherActivity owns that work."
			);
		}

		return null;
	}

	private static int findMatchingBrace(String code, int openingBrace) {
		int depth = 0;
		for (int index = openingBrace; index < code.length(); index++) {
			char current = code.charAt(index);
			if (current == '{') {
				depth++;
			} else if (current == '}') {
				depth--;
				if (depth == 0) {
					return index;
				}
			}
		}
		throw new AssertionError("GodotApp.onCreate() has unmatched braces.");
	}

	private static int findReturnStatement(
		String body,
		int startInclusive,
		int endExclusive
	) {
		for (int index = startInclusive; index < endExclusive; index++) {
			if (!body.startsWith("return", index)) {
				continue;
			}
			boolean startsAtBoundary =
				index == 0 || !Character.isJavaIdentifierPart(body.charAt(index - 1));
			int afterKeyword = index + "return".length();
			boolean endsAtBoundary =
				afterKeyword >= body.length()
					|| !Character.isJavaIdentifierPart(body.charAt(afterKeyword));
			if (!startsAtBoundary || !endsAtBoundary) {
				continue;
			}

			int statementIndex = afterKeyword;
			while (
				statementIndex < endExclusive
					&& Character.isWhitespace(body.charAt(statementIndex))
			) {
				statementIndex++;
			}
			if (
				statementIndex < endExclusive
					&& body.charAt(statementIndex) == ';'
			) {
				return index;
			}
		}
		return -1;
	}

	private static int countOccurrences(String text, String expected) {
		int count = 0;
		int index = 0;
		while ((index = text.indexOf(expected, index)) >= 0) {
			count++;
			index += expected.length();
		}
		return count;
	}

	private static int lineNumber(String source, int index) {
		int line = 1;
		for (int current = 0; current < index && current < source.length(); current++) {
			if (source.charAt(current) == '\n') {
				line++;
			}
		}
		return line;
	}

	private static String maskCommentsAndLiterals(String source) {
		StringBuilder masked = new StringBuilder(source.length());
		State state = State.CODE;

		for (int index = 0; index < source.length(); index++) {
			char current = source.charAt(index);
			char next = index + 1 < source.length()
				? source.charAt(index + 1)
				: '\0';

			switch (state) {
				case CODE:
					if (current == '/' && next == '/') {
						masked.append("  ");
						index++;
						state = State.LINE_COMMENT;
					} else if (current == '/' && next == '*') {
						masked.append("  ");
						index++;
						state = State.BLOCK_COMMENT;
					} else if (current == '"') {
						masked.append(' ');
						state = State.STRING;
					} else if (current == '\'') {
						masked.append(' ');
						state = State.CHARACTER;
					} else {
						masked.append(current);
					}
					break;
				case LINE_COMMENT:
					masked.append(current == '\n' ? '\n' : ' ');
					if (current == '\n') {
						state = State.CODE;
					}
					break;
				case BLOCK_COMMENT:
					if (current == '*' && next == '/') {
						masked.append("  ");
						index++;
						state = State.CODE;
					} else {
						masked.append(current == '\n' ? '\n' : ' ');
					}
					break;
				case STRING:
					if (current == '\\' && next != '\0') {
						masked.append("  ");
						index++;
					} else {
						masked.append(current == '\n' ? '\n' : ' ');
						if (current == '"') {
							state = State.CODE;
						}
					}
					break;
				case CHARACTER:
					if (current == '\\' && next != '\0') {
						masked.append("  ");
						index++;
					} else {
						masked.append(current == '\n' ? '\n' : ' ');
						if (current == '\'') {
							state = State.CODE;
						}
					}
					break;
			}
		}

		return masked.toString();
	}

	private enum State {
		CODE,
		LINE_COMMENT,
		BLOCK_COMMENT,
		STRING,
		CHARACTER
	}

	private record Violation(int line, String message) {
	}
}
