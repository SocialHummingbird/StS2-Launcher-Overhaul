package com.game.sts2launcher;

final class AndroidStartupRouteGate {
	private boolean claimed;

	synchronized boolean tryClaim() {
		if (claimed) {
			return false;
		}
		claimed = true;
		return true;
	}
}
