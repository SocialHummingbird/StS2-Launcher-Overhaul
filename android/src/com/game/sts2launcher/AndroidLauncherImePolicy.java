package com.game.sts2launcher;

final class AndroidLauncherImePolicy {
	enum Action {
		NONE,
		SUPPRESS,
		ALLOW
	}

	private boolean launcherUiActive;
	private boolean textEditingRequested;
	private boolean suppressionIssued;
	private boolean destroyed;

	AndroidLauncherImePolicy(boolean launcherUiActive) {
		this.launcherUiActive = launcherUiActive;
	}

	synchronized Action onLauncherStartup() {
		return requestSuppression(true);
	}

	synchronized Action setLauncherUiActive(boolean active) {
		if (destroyed || launcherUiActive == active) {
			return Action.NONE;
		}
		launcherUiActive = active;
		textEditingRequested = false;
		suppressionIssued = false;
		return active ? requestSuppression(true) : Action.ALLOW;
	}

	synchronized Action onResume() {
		return requestSuppression(false);
	}

	synchronized Action onPause() {
		suppressionIssued = false;
		return Action.NONE;
	}

	synchronized Action onWindowFocusChanged(boolean hasFocus) {
		if (!hasFocus) {
			suppressionIssued = false;
			return Action.NONE;
		}
		return requestSuppression(false);
	}

	synchronized Action onBootTransitionCleanup() {
		return requestSuppression(true);
	}

	synchronized Action onTextEditingRequested(boolean requested) {
		if (destroyed || !launcherUiActive || textEditingRequested == requested) {
			return Action.NONE;
		}
		textEditingRequested = requested;
		suppressionIssued = false;
		return requested ? Action.ALLOW : requestSuppression(true);
	}

	synchronized Action onDestroy() {
		destroyed = true;
		launcherUiActive = false;
		textEditingRequested = false;
		suppressionIssued = false;
		return Action.NONE;
	}

	synchronized boolean shouldSuppressIme() {
		return !destroyed && launcherUiActive && !textEditingRequested;
	}

	synchronized boolean isTextEditingRequested() {
		return !destroyed && launcherUiActive && textEditingRequested;
	}

	private Action requestSuppression(boolean force) {
		if (!shouldSuppressIme() || (!force && suppressionIssued)) {
			return Action.NONE;
		}
		suppressionIssued = true;
		return Action.SUPPRESS;
	}
}
