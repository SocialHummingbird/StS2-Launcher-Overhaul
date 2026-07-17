package com.game.sts2launcher;

public final class AndroidBootIdentityLayoutTest {
	public static void main(String[] args) {
		AndroidBootIdentityLayout.Metrics portrait = AndroidBootIdentityLayout.resolve(
			1080,
			2340,
			3.0f,
			3.0f
		);
		AndroidBootIdentityLayout.Metrics landscape = AndroidBootIdentityLayout.resolve(
			2340,
			1080,
			3.0f,
			3.0f
		);
		assertEquals("orientation-stable mark", portrait.markSizePx(), landscape.markSizePx());
		assertEquals("orientation-stable width", portrait.contentWidthPx(), landscape.contentWidthPx());
		assertEquals("phone mark uses short-edge fraction", 562, portrait.markSizePx());
		assertFits("portrait", portrait, 1080, 2340);
		assertFits("landscape", landscape, 2340, 1080);

		AndroidBootIdentityLayout.Metrics compact = AndroidBootIdentityLayout.resolve(
			800,
			480,
			2.0f,
			2.0f
		);
		assertFits("compact landscape", compact, 800, 480);
		assertEquals("compact mark respects minimum", 288, compact.markSizePx());

		AndroidBootIdentityLayout.Metrics tablet = AndroidBootIdentityLayout.resolve(
			2560,
			1600,
			2.0f,
			2.0f
		);
		assertFits("tablet", tablet, 2560, 1600);
		assertEquals("tablet mark maximum", 464, tablet.markSizePx());

		assertThrowsInvalidViewport();
		System.out.println("Android boot identity layout tests passed.");
	}

	private static void assertFits(
		String label,
		AndroidBootIdentityLayout.Metrics metrics,
		int viewportWidth,
		int viewportHeight
	) {
		if (metrics.markSizePx() <= 0) {
			throw new AssertionError(label + ": mark size must be positive");
		}
		if (metrics.contentWidthPx() > viewportWidth) {
			throw new AssertionError(label + ": content exceeds viewport width");
		}
		if (metrics.contentHeightPx() > viewportHeight) {
			throw new AssertionError(label + ": content exceeds viewport height");
		}
		if (metrics.contentWidthPx() < metrics.markSizePx()) {
			throw new AssertionError(label + ": content width clips the mark");
		}
		if (metrics.contentHeightPx()
			< metrics.markSizePx() + metrics.wordmarkGapPx() + metrics.wordmarkHeightPx()) {
			throw new AssertionError(label + ": content height clips the wordmark");
		}
	}

	private static void assertThrowsInvalidViewport() {
		try {
			AndroidBootIdentityLayout.resolve(0, 1080, 3.0f, 3.0f);
			throw new AssertionError("invalid viewport should throw");
		} catch (IllegalArgumentException expected) {
			// Expected.
		}
	}

	private static void assertEquals(String label, int expected, int actual) {
		if (expected != actual) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}
}
