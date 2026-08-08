package com.game.sts2launcher;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Typeface;
import android.graphics.drawable.Drawable;
import android.util.Property;
import android.view.View;

final class AndroidBootIdentityView extends View {
	static final Property<AndroidBootIdentityView, Float> GODOT_OPACITY =
		new FloatViewProperty("godotOpacity") {
			@Override
			public void setValue(AndroidBootIdentityView view, float value) {
				view.setGodotOpacity(value);
			}

			@Override
			public float getValue(AndroidBootIdentityView view) {
				return view.getGodotOpacity();
			}
		};
	static final Property<AndroidBootIdentityView, Float> GODOT_SCALE =
		new FloatViewProperty("godotScale") {
			@Override
			public void setValue(AndroidBootIdentityView view, float value) {
				view.setGodotScale(value);
			}

			@Override
			public float getValue(AndroidBootIdentityView view) {
				return view.getGodotScale();
			}
		};
	static final Property<AndroidBootIdentityView, Float> IDENTITY_OPACITY =
		new FloatViewProperty("identityOpacity") {
			@Override
			public void setValue(AndroidBootIdentityView view, float value) {
				view.setIdentityOpacity(value);
			}

			@Override
			public float getValue(AndroidBootIdentityView view) {
				return view.getIdentityOpacity();
			}
		};
	static final Property<AndroidBootIdentityView, Float> IDENTITY_SCALE =
		new FloatViewProperty("identityScale") {
			@Override
			public void setValue(AndroidBootIdentityView view, float value) {
				view.setIdentityScale(value);
			}

			@Override
			public float getValue(AndroidBootIdentityView view) {
				return view.getIdentityScale();
			}
		};

	private static final float PULSE_ALPHA = 0.42f;
	private static final float PULSE_SCALE = 0.018f;

	private final AndroidBootIdentityState state = new AndroidBootIdentityState();
	private final Drawable godotMark;
	private final Drawable cyanLayer;
	private final Drawable orangeLayer;
	private final Drawable resolvedMark;
	private final Paint wordmarkPaint = new Paint(Paint.ANTI_ALIAS_FLAG | Paint.SUBPIXEL_TEXT_FLAG);

	private AndroidBootIdentityLayout.Metrics metrics;
	private String wordmark;
	private int godotContainerSizePx;
	private float centerX;
	private float centerY;
	private float identityMarkCenterY;
	private float wordmarkBaseline;

	AndroidBootIdentityView(Context context) {
		super(context);
		godotMark = requireDrawable(context, R.drawable.godot_boot_mark);
		cyanLayer = requireDrawable(context, R.drawable.sts2_boot_mark_cyan);
		orangeLayer = requireDrawable(context, R.drawable.sts2_boot_mark_orange);
		resolvedMark = requireDrawable(context, R.drawable.sts2_boot_mark_full);
		wordmark = context.getString(R.string.sts2_boot_wordmark);

		wordmarkPaint.setColor(context.getColor(R.color.sts2_boot_wordmark));
		wordmarkPaint.setTextAlign(Paint.Align.CENTER);
		wordmarkPaint.setTypeface(Typeface.create("sans-serif", Typeface.BOLD));
		setImportantForAccessibility(View.IMPORTANT_FOR_ACCESSIBILITY_NO);
	}

	void setGodotContainerSizePx(int value) {
		int normalized = Math.max(1, value);
		if (godotContainerSizePx == normalized) {
			return;
		}
		godotContainerSizePx = normalized;
		updateDrawableBounds();
		invalidate();
	}

	void resetVisualState() {
		state.reset();
		invalidate();
	}

	public float getGodotOpacity() {
		return state.godotOpacity();
	}

	public void setGodotOpacity(float value) {
		invalidateWhen(state.setGodotOpacity(value));
	}

	public float getGodotScale() {
		return state.godotScale();
	}

	public void setGodotScale(float value) {
		invalidateWhen(state.setGodotScale(value));
	}

	public float getIdentityOpacity() {
		return state.identityOpacity();
	}

	public void setIdentityOpacity(float value) {
		invalidateWhen(state.setIdentityOpacity(value));
	}

	public float getIdentityScale() {
		return state.identityScale();
	}

	public void setIdentityScale(float value) {
		invalidateWhen(state.setIdentityScale(value));
	}

	public float getCyanOpacity() {
		return state.cyanOpacity();
	}

	public void setCyanOpacity(float value) {
		invalidateWhen(state.setCyanOpacity(value));
	}

	public float getOrangeOpacity() {
		return state.orangeOpacity();
	}

	public void setOrangeOpacity(float value) {
		invalidateWhen(state.setOrangeOpacity(value));
	}

	public float getResolvedOpacity() {
		return state.resolvedOpacity();
	}

	public void setResolvedOpacity(float value) {
		invalidateWhen(state.setResolvedOpacity(value));
	}

	public float getWordmarkOpacity() {
		return state.wordmarkOpacity();
	}

	public void setWordmarkOpacity(float value) {
		invalidateWhen(state.setWordmarkOpacity(value));
	}

	public float getCyanOffsetX() {
		return state.cyanOffsetX();
	}

	public void setCyanOffsetX(float value) {
		invalidateWhen(state.setCyanOffsetX(value));
	}

	public float getCyanOffsetY() {
		return state.cyanOffsetY();
	}

	public void setCyanOffsetY(float value) {
		invalidateWhen(state.setCyanOffsetY(value));
	}

	public float getOrangeOffsetX() {
		return state.orangeOffsetX();
	}

	public void setOrangeOffsetX(float value) {
		invalidateWhen(state.setOrangeOffsetX(value));
	}

	public float getOrangeOffsetY() {
		return state.orangeOffsetY();
	}

	public void setOrangeOffsetY(float value) {
		invalidateWhen(state.setOrangeOffsetY(value));
	}

	public float getRevealProgress() {
		return state.revealProgress();
	}

	public void setRevealProgress(float value) {
		invalidateWhen(state.setRevealProgress(value));
	}

	public float getPulseStrength() {
		return state.pulseStrength();
	}

	public void setPulseStrength(float value) {
		invalidateWhen(state.setPulseStrength(value));
	}

	public String getWordmark() {
		return wordmark;
	}

	public void setWordmark(String value) {
		String normalized = value == null ? "" : value;
		if (wordmark.equals(normalized)) {
			return;
		}
		wordmark = normalized;
		updateWordmarkMetrics();
		invalidate();
	}

	@Override
	protected void onSizeChanged(int width, int height, int oldWidth, int oldHeight) {
		super.onSizeChanged(width, height, oldWidth, oldHeight);
		if (width <= 0 || height <= 0) {
			return;
		}
		metrics = AndroidBootIdentityLayout.resolve(
			width,
			height,
			getResources().getDisplayMetrics().density,
			getResources().getDisplayMetrics().scaledDensity
		);
		centerX = width * 0.5f;
		centerY = height * 0.5f;
		float contentTop = centerY - (metrics.contentHeightPx() * 0.5f);
		identityMarkCenterY = contentTop + (metrics.markSizePx() * 0.5f);
		updateDrawableBounds();
		updateWordmarkMetrics();
	}

	@Override
	protected void onDraw(Canvas canvas) {
		super.onDraw(canvas);
		if (metrics == null) {
			return;
		}

		drawDrawable(
			canvas,
			godotMark,
			centerX,
			centerY,
			state.godotScale(),
			0.0f,
			0.0f,
			state.godotOpacity()
		);

		float identityOpacity = state.identityOpacity();
		if (identityOpacity <= 0.0f) {
			return;
		}

		canvas.save();
		canvas.translate(centerX, centerY);
		canvas.scale(state.identityScale(), state.identityScale());
		canvas.translate(-centerX, -centerY);
		drawIdentityLayers(canvas, identityOpacity);
		drawWordmark(canvas, identityOpacity);
		canvas.restore();
	}

	private void drawIdentityLayers(Canvas canvas, float identityOpacity) {
		float revealProgress = state.revealProgress();
		if (revealProgress <= 0.0f) {
			return;
		}

		float revealHalfWidth = metrics.markSizePx() * revealProgress * 0.5f;
		float markHalfHeight = metrics.markSizePx() * 0.5f;
		canvas.save();
		if (revealProgress < 1.0f) {
			canvas.clipRect(
				centerX - revealHalfWidth,
				identityMarkCenterY - markHalfHeight,
				centerX + revealHalfWidth,
				identityMarkCenterY + markHalfHeight
			);
		}
		drawDrawable(
			canvas,
			cyanLayer,
			centerX,
			identityMarkCenterY,
			1.0f,
			state.cyanOffsetX(),
			state.cyanOffsetY(),
			identityOpacity * state.cyanOpacity()
		);
		drawDrawable(
			canvas,
			orangeLayer,
			centerX,
			identityMarkCenterY,
			1.0f,
			state.orangeOffsetX(),
			state.orangeOffsetY(),
			identityOpacity * state.orangeOpacity()
		);
		drawDrawable(
			canvas,
			resolvedMark,
			centerX,
			identityMarkCenterY,
			1.0f,
			0.0f,
			0.0f,
			identityOpacity * state.resolvedOpacity()
		);

		float pulseStrength = state.pulseStrength();
		if (pulseStrength > 0.0f) {
			float scale = 1.0f + (pulseStrength * PULSE_SCALE);
			float opacity = identityOpacity * pulseStrength * PULSE_ALPHA;
			drawDrawable(
				canvas,
				cyanLayer,
				centerX,
				identityMarkCenterY,
				scale,
				0.0f,
				0.0f,
				opacity
			);
			drawDrawable(
				canvas,
				orangeLayer,
				centerX,
				identityMarkCenterY,
				scale,
				0.0f,
				0.0f,
				opacity
			);
		}
		canvas.restore();
	}

	private void drawWordmark(Canvas canvas, float identityOpacity) {
		float opacity = identityOpacity * state.wordmarkOpacity();
		if (opacity <= 0.0f || wordmark.isEmpty()) {
			return;
		}
		wordmarkPaint.setAlpha(alpha(opacity));
		canvas.drawText(wordmark, centerX, wordmarkBaseline, wordmarkPaint);
	}

	private void updateDrawableBounds() {
		if (godotContainerSizePx > 0) {
			setCenteredBounds(godotMark, godotContainerSizePx);
		}
		if (metrics != null) {
			setCenteredBounds(cyanLayer, metrics.markSizePx());
			setCenteredBounds(orangeLayer, metrics.markSizePx());
			setCenteredBounds(resolvedMark, metrics.markSizePx());
		}
	}

	private void updateWordmarkMetrics() {
		if (metrics == null) {
			return;
		}
		float scaledDensity = getResources().getDisplayMetrics().scaledDensity;
		float textSize = AndroidBootIdentityLayout.WORDMARK_TEXT_SP * scaledDensity;
		wordmarkPaint.setTextSize(textSize);
		float measuredWidth = wordmarkPaint.measureText(wordmark);
		if (measuredWidth > metrics.contentWidthPx() && measuredWidth > 0.0f) {
			wordmarkPaint.setTextSize(textSize * (metrics.contentWidthPx() / measuredWidth));
		}
		Paint.FontMetrics fontMetrics = wordmarkPaint.getFontMetrics();
		float wordmarkTop = identityMarkCenterY
			+ (metrics.markSizePx() * 0.5f)
			+ metrics.wordmarkGapPx();
		wordmarkBaseline = wordmarkTop
			+ ((metrics.wordmarkHeightPx() - fontMetrics.descent - fontMetrics.ascent) * 0.5f);
	}

	private static void drawDrawable(
		Canvas canvas,
		Drawable drawable,
		float centerX,
		float centerY,
		float scale,
		float offsetX,
		float offsetY,
		float opacity
	) {
		if (opacity <= 0.0f || scale <= 0.0f) {
			return;
		}
		drawable.setAlpha(alpha(opacity));
		canvas.save();
		canvas.translate(centerX + offsetX, centerY + offsetY);
		canvas.scale(scale, scale);
		drawable.draw(canvas);
		canvas.restore();
	}

	private static void setCenteredBounds(Drawable drawable, int size) {
		int half = size / 2;
		drawable.setBounds(-half, -half, size - half, size - half);
	}

	private static int alpha(float opacity) {
		return Math.round(Math.max(0.0f, Math.min(1.0f, opacity)) * 255.0f);
	}

	private static Drawable requireDrawable(Context context, int resourceId) {
		Drawable drawable = context.getDrawable(resourceId);
		if (drawable == null) {
			throw new IllegalStateException("Missing boot identity drawable: " + resourceId);
		}
		return drawable.mutate();
	}

	private void invalidateWhen(boolean changed) {
		if (changed) {
			invalidate();
		}
	}

	private abstract static class FloatViewProperty
		extends Property<AndroidBootIdentityView, Float> {
		FloatViewProperty(String name) {
			super(Float.class, name);
		}

		@Override
		public final void set(AndroidBootIdentityView view, Float value) {
			setValue(view, value);
		}

		@Override
		public final Float get(AndroidBootIdentityView view) {
			return getValue(view);
		}

		public abstract void setValue(AndroidBootIdentityView view, float value);

		public abstract float getValue(AndroidBootIdentityView view);
	}
}
