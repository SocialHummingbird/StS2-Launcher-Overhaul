package com.game.sts2launcher;

public final class AndroidBootIdentityStateTest {
	public static void main(String[] args) {
		AndroidBootIdentityState state = new AndroidBootIdentityState();
		assertFloat("default Godot opacity", 1.0f, state.godotOpacity());
		assertFloat("default Godot scale", 1.0f, state.godotScale());
		assertFloat("default identity opacity", 0.0f, state.identityOpacity());
		assertFloat("default identity scale", 0.72f, state.identityScale());
		assertFloat("default resolved layer", 1.0f, state.resolvedOpacity());
		assertFloat("default reveal", 1.0f, state.revealProgress());

		assertChanged("Godot opacity", state.setGodotOpacity(0.35f));
		assertChanged("Godot scale", state.setGodotScale(0.82f));
		assertChanged("identity opacity", state.setIdentityOpacity(0.8f));
		assertChanged("identity scale", state.setIdentityScale(1.04f));
		assertChanged("cyan opacity", state.setCyanOpacity(0.4f));
		assertChanged("orange opacity", state.setOrangeOpacity(0.5f));
		assertChanged("resolved opacity", state.setResolvedOpacity(0.6f));
		assertChanged("wordmark opacity", state.setWordmarkOpacity(0.7f));
		assertChanged("cyan x offset", state.setCyanOffsetX(-18.0f));
		assertChanged("cyan y offset", state.setCyanOffsetY(3.0f));
		assertChanged("orange x offset", state.setOrangeOffsetX(18.0f));
		assertChanged("orange y offset", state.setOrangeOffsetY(-3.0f));
		assertChanged("reveal", state.setRevealProgress(0.45f));
		assertChanged("pulse", state.setPulseStrength(0.9f));

		assertFloat("Godot opacity set", 0.35f, state.godotOpacity());
		assertFloat("Godot scale set", 0.82f, state.godotScale());
		assertFloat("identity opacity set", 0.8f, state.identityOpacity());
		assertFloat("identity scale set", 1.04f, state.identityScale());
		assertFloat("cyan opacity set", 0.4f, state.cyanOpacity());
		assertFloat("orange opacity set", 0.5f, state.orangeOpacity());
		assertFloat("resolved opacity set", 0.6f, state.resolvedOpacity());
		assertFloat("wordmark opacity set", 0.7f, state.wordmarkOpacity());
		assertFloat("cyan x set", -18.0f, state.cyanOffsetX());
		assertFloat("cyan y set", 3.0f, state.cyanOffsetY());
		assertFloat("orange x set", 18.0f, state.orangeOffsetX());
		assertFloat("orange y set", -3.0f, state.orangeOffsetY());
		assertFloat("reveal set", 0.45f, state.revealProgress());
		assertFloat("pulse set", 0.9f, state.pulseStrength());

		assertUnchanged("duplicate value", state.setPulseStrength(0.9f));
		state.setGodotOpacity(2.0f);
		state.setIdentityOpacity(-1.0f);
		state.setIdentityScale(4.0f);
		state.setRevealProgress(Float.NaN);
		state.setPulseStrength(Float.POSITIVE_INFINITY);
		state.setCyanOffsetX(Float.NEGATIVE_INFINITY);
		assertFloat("opacity clamps high", 1.0f, state.godotOpacity());
		assertFloat("opacity clamps low", 0.0f, state.identityOpacity());
		assertFloat("scale clamps", 2.0f, state.identityScale());
		assertFloat("NaN reveal normalizes", 0.0f, state.revealProgress());
		assertFloat("infinite pulse normalizes", 0.0f, state.pulseStrength());
		assertFloat("infinite offset normalizes", 0.0f, state.cyanOffsetX());

		state.reset();
		assertFloat("reset identity opacity", 0.0f, state.identityOpacity());
		assertFloat("reset identity scale", 0.72f, state.identityScale());
		assertFloat("reset resolved opacity", 1.0f, state.resolvedOpacity());
		System.out.println("Android boot identity state tests passed.");
	}

	private static void assertChanged(String label, boolean changed) {
		if (!changed) {
			throw new AssertionError(label + ": expected a state change");
		}
	}

	private static void assertUnchanged(String label, boolean changed) {
		if (changed) {
			throw new AssertionError(label + ": expected no state change");
		}
	}

	private static void assertFloat(String label, float expected, float actual) {
		if (Float.compare(expected, actual) != 0) {
			throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
		}
	}
}
