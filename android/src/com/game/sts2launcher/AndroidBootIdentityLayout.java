package com.game.sts2launcher;

final class AndroidBootIdentityLayout {
	static final float WORDMARK_TEXT_SP = 18.0f;

	private static final float MARK_SHORT_EDGE_FRACTION = 0.42f;
	private static final float CONTENT_WIDTH_MARK_MULTIPLIER = 1.55f;
	private static final int MIN_MARK_DP = 112;
	private static final int MAX_MARK_DP = 184;
	private static final int MIN_CONTENT_WIDTH_DP = 200;
	private static final int CONTENT_INSET_DP = 24;
	private static final int WORDMARK_GAP_DP = 12;
	private static final int WORDMARK_LINE_SP = 24;

	private AndroidBootIdentityLayout() {
	}

	static Metrics resolve(
		int viewportWidthPx,
		int viewportHeightPx,
		float density,
		float scaledDensity
	) {
		if (viewportWidthPx <= 0 || viewportHeightPx <= 0) {
			throw new IllegalArgumentException("Viewport dimensions must be positive");
		}
		if (density <= 0.0f || scaledDensity <= 0.0f) {
			throw new IllegalArgumentException("Display densities must be positive");
		}

		int shortEdgePx = Math.min(viewportWidthPx, viewportHeightPx);
		int contentInsetPx = dp(CONTENT_INSET_DP, density);
		int wordmarkGapPx = dp(WORDMARK_GAP_DP, density);
		int wordmarkHeightPx = Math.round(WORDMARK_LINE_SP * scaledDensity);
		int availableWidthPx = Math.max(1, viewportWidthPx - (contentInsetPx * 2));
		int availableHeightPx = Math.max(1, viewportHeightPx - (contentInsetPx * 2));
		int maximumMarkSizePx = Math.max(
			1,
			Math.min(
				dp(MAX_MARK_DP, density),
				Math.min(
					availableWidthPx,
					availableHeightPx - wordmarkGapPx - wordmarkHeightPx
				)
			)
		);
		int markSizePx = clamp(
			Math.round(shortEdgePx * MARK_SHORT_EDGE_FRACTION),
			Math.min(dp(MIN_MARK_DP, density), maximumMarkSizePx),
			maximumMarkSizePx
		);

		int contentWidthPx = Math.min(
			availableWidthPx,
			Math.max(
				markSizePx,
				Math.max(
					Math.min(dp(MIN_CONTENT_WIDTH_DP, density), availableWidthPx),
					Math.round(markSizePx * CONTENT_WIDTH_MARK_MULTIPLIER)
				)
			)
		);
		return new Metrics(
			markSizePx,
			contentWidthPx,
			markSizePx + wordmarkGapPx + wordmarkHeightPx,
			wordmarkGapPx,
			wordmarkHeightPx
		);
	}

	private static int dp(int value, float density) {
		return Math.round(value * density);
	}

	private static int clamp(int value, int minimum, int maximum) {
		return Math.max(minimum, Math.min(maximum, value));
	}

	static final class Metrics {
		private final int markSizePx;
		private final int contentWidthPx;
		private final int contentHeightPx;
		private final int wordmarkGapPx;
		private final int wordmarkHeightPx;

		Metrics(
			int markSizePx,
			int contentWidthPx,
			int contentHeightPx,
			int wordmarkGapPx,
			int wordmarkHeightPx
		) {
			this.markSizePx = markSizePx;
			this.contentWidthPx = contentWidthPx;
			this.contentHeightPx = contentHeightPx;
			this.wordmarkGapPx = wordmarkGapPx;
			this.wordmarkHeightPx = wordmarkHeightPx;
		}

		int markSizePx() {
			return markSizePx;
		}

		int contentWidthPx() {
			return contentWidthPx;
		}

		int contentHeightPx() {
			return contentHeightPx;
		}

		int wordmarkGapPx() {
			return wordmarkGapPx;
		}

		int wordmarkHeightPx() {
			return wordmarkHeightPx;
		}
	}
}
