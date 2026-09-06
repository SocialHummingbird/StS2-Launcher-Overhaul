package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONObject;
import org.junit.Test;
import java.io.IOException;
import static org.junit.Assert.*;

public final class LauncherAppReleaseTest {
    private JSONObject asset(String abi) throws Exception {
        return new JSONObject().put("name", "StS2Launcher-v0.2.430-" + abi + ".apk")
            .put("browser_download_url", LauncherAppRelease.REPOSITORY + "/releases/download/v0.2.430/app-" + abi + ".apk")
            .put("size", 48000000).put("digest", "sha256:" + String.join("", java.util.Collections.nCopies(64, "a")));
    }
    private JSONObject release(JSONArray assets) throws Exception {
        return new JSONObject().put("tag_name", "v0.2.430").put("assets", assets);
    }
    @Test public void choosesDeviceAbiAndRetainsChecksum() throws Exception {
        LauncherAppRelease result = LauncherAppRelease.parse(release(new JSONArray().put(asset("x86_64")).put(asset("arm64-v8a"))).toString(),
            "0.2.429-issue38-arm64-rc4", new String[] {"arm64-v8a", "armeabi-v7a"});
        assertTrue(result.url.endsWith("arm64-v8a.apk")); assertEquals(48000000, result.size); assertEquals(64, result.sha256.length());
    }
    @Test public void comparesCoreVersionsNumerically() throws Exception {
        assertTrue(LauncherAppRelease.isNewer("v0.2.430", "0.2.99"));
        assertFalse(LauncherAppRelease.isNewer("v0.2.430", "0.2.431"));
        assertFalse(LauncherAppRelease.isNewer("v0.2.430", "0.2.430"));
    }
    @Test public void handlesRevisionsWithoutDowngradingStable() throws Exception {
        assertTrue(LauncherAppRelease.isNewer("v0.2.430-issue37-arm64-rc3", "0.2.430-issue37-arm64-rc2"));
        assertFalse(LauncherAppRelease.isNewer("v0.2.430-issue37-arm64-rc2", "0.2.430-issue37-arm64-rc3"));
        assertTrue(LauncherAppRelease.isNewer("v0.2.430", "0.2.430-rc3"));
        assertFalse(LauncherAppRelease.isNewer("v0.2.430-rc3", "0.2.430"));
    }
    @Test public void rejectsUnknownInstalledVersion() throws Exception {
        assertThrows(IOException.class, () -> LauncherAppRelease.isNewer("v0.2.430", "unknown"));
    }
    @Test public void ignoresDraftsAndUnpublishedPrereleases() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
        assertNull(LauncherAppRelease.parse(json.put("draft", true).toString(), "0.2.429", new String[]{"arm64-v8a"}));
        assertNull(LauncherAppRelease.parse(json.put("draft", false).put("prerelease", true).toString(), "0.2.429", new String[]{"arm64-v8a"}));
    }
    @Test public void equalVersionDoesNotRequireAsset() throws Exception {
        assertNull(LauncherAppRelease.parse(release(new JSONArray()).toString(), "0.2.430", new String[]{"arm64-v8a"}));
    }
    @Test public void missingAbiIsNotReportedAsUpToDate() throws Exception {
        String json = release(new JSONArray().put(asset("x86_64"))).toString();
        assertThrows(IOException.class, () -> LauncherAppRelease.parse(json, "0.2.429", new String[]{"arm64-v8a"}));
    }
    @Test public void rejectsMissingChecksumAndAmbiguousAssets() throws Exception {
        String missing = release(new JSONArray().put(asset("arm64-v8a").put("digest", ""))).toString();
        assertThrows(IOException.class, () -> LauncherAppRelease.parse(missing, "0.2.429", new String[]{"arm64-v8a"}));
        String multiple = release(new JSONArray().put(asset("arm64-v8a")).put(asset("arm64-v8a"))).toString();
        assertThrows(IOException.class, () -> LauncherAppRelease.parse(multiple, "0.2.429", new String[]{"arm64-v8a"}));
    }
    @Test public void restrictsHttpsAndRedirectHosts() {
        assertTrue(LauncherAppRelease.isTrustedUrl("https://release-assets.githubusercontent.com/file?signature=123"));
        assertFalse(LauncherAppRelease.isTrustedUrl("http://github.com/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://github.com.attacker.test/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://user@github.com/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://github.com:444/file"));
    }
    @Test public void rejectsWrongRepositoryAndInvalidSize() throws Exception {
        String wrong = release(new JSONArray().put(asset("arm64-v8a").put("browser_download_url", "https://github.com/another/project/releases/download/v1/app.apk"))).toString();
        assertThrows(IOException.class, () -> LauncherAppRelease.parse(wrong, "0.2.429", new String[]{"arm64-v8a"}));
        String empty = release(new JSONArray().put(asset("arm64-v8a").put("size", 0))).toString();
        assertThrows(IOException.class, () -> LauncherAppRelease.parse(empty, "0.2.429", new String[]{"arm64-v8a"}));
    }
}
