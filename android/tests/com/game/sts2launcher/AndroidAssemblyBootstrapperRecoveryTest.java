package com.game.sts2launcher;

import org.junit.Test;

import static org.junit.Assert.assertEquals;
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

	@Test
	public void preparationFailureKeepsTechnicalDetailsOutOfPrimaryCopy() {
		AndroidAssemblyBootstrapper.Result result =
			AndroidAssemblyBootstrapper.Result.failure(
				"runtimePackId=secret-internal-id path=/data/user/0/example"
			);

		assertEquals("Game preparation failed", result.title());
		assertTrue(result.message().contains("Repair selected branch"));
		assertTrue(result.message().contains("Saves and other branches stay in place"));
		assertTrue(result.message().contains("Do not uninstall the app or clear app data"));
		assertFalse(result.message().contains("runtimePackId"));
		assertFalse(result.message().contains("/data/user"));
		assertTrue(result.diagnostics().contains("runtimePackId=secret-internal-id"));
	}
}
