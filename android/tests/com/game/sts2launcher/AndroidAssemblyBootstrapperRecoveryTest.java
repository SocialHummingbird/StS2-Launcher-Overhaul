package com.game.sts2launcher;

import org.junit.Test;

import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class AndroidAssemblyBootstrapperRecoveryTest {
	@Test
	public void selectedBranchCacheEvidenceIsClearedButAnotherBranchIsPreserved() {
		assertTrue(AndroidAssemblyBootstrapper.cacheEvidenceMustBeCleared(
			"PUBLIC-BETA",
			"public-beta"
		));
		assertFalse(AndroidAssemblyBootstrapper.cacheEvidenceMustBeCleared(
			"public",
			"public-beta"
		));
		assertFalse(AndroidAssemblyBootstrapper.cacheEvidenceMustBeCleared(
			"",
			"public-beta"
		));
	}

	@Test
	public void corruptCacheBranchEvidenceIsCleared() {
		assertTrue(AndroidAssemblyBootstrapper.cacheEvidenceMustBeCleared(
			"public-beta\nother",
			"public-beta"
		));
	}
}
