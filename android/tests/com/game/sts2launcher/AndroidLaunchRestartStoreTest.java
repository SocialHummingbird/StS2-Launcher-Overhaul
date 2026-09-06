package com.game.sts2launcher;

import org.junit.Test;
import org.json.JSONObject;
import static org.junit.Assert.*;

public class AndroidLaunchRestartStoreTest {
    static final long NOW = 1000000;
    static class Memory implements AndroidLaunchRestartStore.Storage {
        String value = "";
        boolean writable = true;
        public String read() { return value; }
        public boolean write(String next) { if (!writable) return false; value = next; return true; }
    }
    String request() throws Exception {
        return new JSONObject().put("version", 1).put("attemptId", "attempt-1")
            .put("branch", "public").put("safe", true).put("gameIdentityId", "game-1")
            .put("runtimePackId", "pack-1").put("generation", "generation-1")
            .put("createdAtUnixMs", NOW).put("state", "pending").toString();
    }
    @Test public void acceptanceRequiresTargetAndDurableCommit() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        assertFalse(store.accept(request(), false, NOW).isEmpty());
        assertEquals("", memory.value);
        memory.writable = false;
        assertFalse(store.accept(request(), true, NOW).isEmpty());
    }
    @Test public void claimSurvivesUntilManagedConsumptionButCannotReplayAfterDeath() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        assertEquals("", store.accept(request(), true, NOW));
        assertFalse(store.claim("public", NOW).isEmpty());
        assertEquals("claimed", new JSONObject(memory.value).getString("state"));
        assertEquals("", new AndroidLaunchRestartStore(memory).claim("public", NOW));
        assertEquals("attempt-1", new JSONObject(store.consume()).getString("attemptId"));
        assertEquals("", store.consume());
        assertEquals("consumed", new JSONObject(memory.value).getString("state"));
    }
    @Test public void invalidStaleOrDifferentBranchCannotClaim() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        assertFalse(store.accept("{}", true, NOW).isEmpty());
        assertFalse(store.accept(request(), true, NOW + 86400001).isEmpty());
        assertEquals("", store.accept(request(), true, NOW));
        assertEquals("", store.claim("beta", NOW));
        assertEquals("pending", new JSONObject(memory.value).getString("state"));
    }
    @Test public void failedConsumptionDoesNotDeliverRequest() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        store.accept(request(), true, NOW);
        store.claim("public", NOW);
        memory.writable = false;
        assertEquals("", store.consume());
        memory.writable = true;
        assertFalse(store.consume().isEmpty());
    }
    @Test public void recoveryCancellationInvalidatesInMemoryClaim() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        store.accept(request(), true, NOW);
        store.claim("public", NOW);
        memory.value = new JSONObject(memory.value).put("state", "cancelled").toString();
        assertEquals("", store.consume());
    }
    @Test public void targetStartFailureRejectsAcceptanceAndRetainsFailure() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        String problem = store.acceptAndStart(request(), true, NOW, () -> {
            throw new IllegalStateException("target-start-failed");
        });
        assertFalse(problem.isEmpty());
        boolean[] exited = { false };
        assertFalse(store.finish("attempt-1", () -> exited[0] = true).isEmpty());
        assertFalse(exited[0]);
        assertEquals("failed", new JSONObject(memory.value).getString("state"));
        assertEquals("", store.claim("public", NOW));
    }
    @Test public void originatingProcessCannotClaimAndFinishRequiresAcceptedIdentity() throws Exception {
        Memory memory = new Memory();
        AndroidLaunchRestartStore store = new AndroidLaunchRestartStore(memory);
        assertEquals("", store.acceptAndStart(request(), true, NOW, () -> {}));
        assertEquals("", store.claim("public", NOW));
        boolean[] exited = { false };
        assertFalse(store.finish("wrong-attempt", () -> exited[0] = true).isEmpty());
        assertFalse(exited[0]);
        assertEquals("", store.finish("attempt-1", () -> exited[0] = true));
        assertTrue(exited[0]);
        assertFalse(new AndroidLaunchRestartStore(memory).claim("public", NOW).isEmpty());
    }
}
