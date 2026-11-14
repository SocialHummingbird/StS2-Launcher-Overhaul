package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONObject;
import org.junit.Test;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import static org.junit.Assert.*;

public final class LauncherAppReleaseTest {
    private static final String PACKAGE = "com.sts2launcher.overhaul.fork.local";
    private final Map<String, byte[]> metadataBodies = new HashMap<>();
    private JSONObject asset(String abi) throws Exception {
        return asset(abi, "0.2.430");
    }
    private JSONObject asset(String abi, String version) throws Exception {
        String name = "StS2Launcher-v" + version + "-" + abi + ".apk";
        return new JSONObject().put("name", name)
            .put("browser_download_url", LauncherAppRelease.REPOSITORY + "/releases/download/v" + version + "/" + name)
            .put("size", 48000000).put("digest", "sha256:" + String.join("", java.util.Collections.nCopies(64, "a")));
    }
    private JSONObject release(JSONArray assets) throws Exception {
        return release(assets, 430000, "0.2.430");
    }
    private JSONObject release(JSONArray apks, long code, String version) throws Exception {
        JSONArray assets = new JSONArray();
        for (int i = 0; i < apks.length(); i++) {
            JSONObject apk = apks.getJSONObject(i);
            JSONObject metadata = new JSONObject().put("versionName", version).put("versionCode", code)
                .put("packageName", PACKAGE).put("apk", apk.getString("name"))
                .put("sha256", apk.optString("digest").replaceFirst("^sha256:", ""));
            JSONObject companion = new JSONObject().put("name", apk.getString("name") + ".json")
                .put("browser_download_url", apk.getString("browser_download_url") + ".json");
            store(companion, metadata.toString().getBytes(StandardCharsets.UTF_8));
            assets.put(apk).put(companion);
        }
        return new JSONObject().put("tag_name", "v" + version).put("assets", assets);
    }
    private void store(JSONObject companion, byte[] bytes) throws Exception {
        StringBuilder hash = new StringBuilder();
        for (byte value : MessageDigest.getInstance("SHA-256").digest(bytes))
            hash.append(String.format(Locale.ROOT, "%02x", value & 255));
        companion.put("size", bytes.length).put("digest", "sha256:" + hash);
        metadataBodies.put(companion.getString("browser_download_url"), bytes);
    }
    private JSONObject metadata(JSONObject release) throws Exception {
        String url = release.getJSONArray("assets").getJSONObject(1).getString("browser_download_url");
        return new JSONObject(new String(metadataBodies.get(url), StandardCharsets.UTF_8));
    }
    private void rewriteMetadata(JSONObject release, JSONObject metadata) throws Exception {
        store(release.getJSONArray("assets").getJSONObject(1), metadata.toString().getBytes(StandardCharsets.UTF_8));
    }
    private LauncherAppRelease parse(JSONObject release, long installedCode) throws Exception {
        return parse(release, installedCode, new String[]{"arm64-v8a"});
    }
    private LauncherAppRelease parse(JSONObject release, long installedCode, String[] abis) throws Exception {
        return LauncherAppRelease.parse(release.toString(), PACKAGE, installedCode, abis, url -> {
            byte[] body = metadataBodies.get(url);
            if (body == null) throw new IOException("Metadata unavailable.");
            return body;
        });
    }
    @Test public void choosesDeviceAbiAndRetainsChecksum() throws Exception {
        LauncherAppRelease result = parse(release(new JSONArray().put(asset("x86_64")).put(asset("arm64-v8a"))),
            429004, new String[] {"arm64-v8a", "armeabi-v7a"});
        assertTrue(result.url.endsWith("arm64-v8a.apk")); assertEquals(48000000, result.size); assertEquals(64, result.sha256.length());
        assertEquals(430000, result.versionCode);
    }
    @Test public void comparesBuildNumbersInsteadOfReleaseLabels() throws Exception {
        assertNotNull(parse(release(new JSONArray().put(asset("arm64-v8a"))), 99000));
        assertNull(parse(release(new JSONArray().put(asset("arm64-v8a"))), 431000));
        assertNull(parse(release(new JSONArray().put(asset("arm64-v8a"))), 430000));
        JSONObject misleadingTag = release(new JSONArray().put(asset("arm64-v8a"))).put("tag_name", "v99.0.0");
        assertNull(parse(misleadingTag, 431000));
    }
    @Test public void sameReleaseLabelDoesNotOfferAnOlderInstalledDevelopmentBuild() throws Exception {
        // Published stable 438015 is older than the Fold's 0.2.438-* builds.
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a", "0.2.438")), 438015, "0.2.438");
        for (long installed : new long[]{438015, 438025, 438026, 438027}) assertNull(parse(json, installed));
    }
    @Test public void genuineNewerBuildIsOfferedEvenWithTheSameDisplayVersion() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a", "0.2.438")), 438028, "0.2.438");
        assertEquals(438028, parse(json, 438027).versionCode);
        assertEquals("0.2.438", parse(json, 438027).version);
    }
    @Test public void supportsLongAndroidBuildNumbers() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a")), 5_000_000_000L, "0.2.430");
        assertEquals(5_000_000_000L, parse(json, 4_999_999_999L).versionCode);
        assertNull(parse(json, 5_000_000_000L));
    }
    @Test public void rejectsUnknownInstalledBuildNumber() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
        assertThrows(IOException.class, () -> parse(json, 0));
        assertThrows(IOException.class, () -> parse(json, -1));
    }
    @Test public void ignoresDraftsAndUnpublishedPrereleases() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
        assertNull(parse(json.put("draft", true), 429000));
        assertNull(parse(json.put("draft", false).put("prerelease", true), 429000));
    }
    @Test public void missingAbiIsNotReportedAsUpToDate() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("x86_64")));
        assertThrows(IOException.class, () -> parse(json, 429000));
    }
    @Test public void rejectsMissingChecksumAndAmbiguousAssets() throws Exception {
        JSONObject missing = release(new JSONArray().put(asset("arm64-v8a").put("digest", "")));
        assertThrows(IOException.class, () -> parse(missing, 429000));
        JSONObject multiple = release(new JSONArray().put(asset("arm64-v8a")).put(asset("arm64-v8a")));
        assertThrows(IOException.class, () -> parse(multiple, 429000));
    }
    @Test public void restrictsHttpsAndRedirectHosts() {
        assertTrue(LauncherAppRelease.isTrustedUrl("https://release-assets.githubusercontent.com/file?signature=123"));
        assertFalse(LauncherAppRelease.isTrustedUrl("http://github.com/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://github.com.attacker.test/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://user@github.com/file"));
        assertFalse(LauncherAppRelease.isTrustedUrl("https://github.com:444/file"));
    }
    @Test public void rejectsWrongRepositoryAndInvalidSize() throws Exception {
        JSONObject wrong = release(new JSONArray().put(asset("arm64-v8a").put("browser_download_url", "https://github.com/another/project/releases/download/v1/app.apk")));
        assertThrows(IOException.class, () -> parse(wrong, 429000));
        JSONObject empty = release(new JSONArray().put(asset("arm64-v8a").put("size", 0)));
        assertThrows(IOException.class, () -> parse(empty, 429000));
    }
    @Test public void missingOrUnavailableMetadataDoesNotFallBackToVersionNames() throws Exception {
        JSONObject missing = release(new JSONArray().put(asset("arm64-v8a")));
        missing.getJSONArray("assets").remove(1);
        assertThrows(IOException.class, () -> parse(missing, 429000));
        JSONObject unavailable = release(new JSONArray().put(asset("arm64-v8a")));
        metadataBodies.clear();
        assertThrows(IOException.class, () -> parse(unavailable, 429000));
    }
    @Test public void metadataChecksumAndLengthMustMatch() throws Exception {
        JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
        JSONObject companion = json.getJSONArray("assets").getJSONObject(1);
        String digest = companion.getString("digest");
        companion.put("digest", "");
        assertThrows(IOException.class, () -> parse(json, 429000));
        companion.put("digest", "sha256:" + String.join("", java.util.Collections.nCopies(64, "0")));
        assertThrows(IOException.class, () -> parse(json, 429000));
        companion.put("digest", digest).put("size", companion.getLong("size") + 1);
        assertThrows(IOException.class, () -> parse(json, 429000));
    }
    @Test public void rejectsMetadataForDifferentApkOrPackage() throws Exception {
        for (String key : new String[]{"apk", "sha256", "packageName"}) {
            JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
            JSONObject metadata = metadata(json).put(key, "wrong");
            rewriteMetadata(json, metadata);
            assertThrows(IOException.class, () -> parse(json, 429000));
        }
    }
    @Test public void rejectsMissingNonIntegerAndOutOfRangeBuildNumbers() throws Exception {
        for (Object invalid : new Object[]{JSONObject.NULL, 0, -1, 438015.5, "438015", new java.math.BigInteger("9223372036854775808")}) {
            JSONObject json = release(new JSONArray().put(asset("arm64-v8a")));
            rewriteMetadata(json, metadata(json).put("versionCode", invalid));
            assertThrows(IOException.class, () -> parse(json, 429000));
        }
    }
    @Test public void rejectsDuplicateUntrustedAndOversizedMetadata() throws Exception {
        JSONObject duplicate = release(new JSONArray().put(asset("arm64-v8a")));
        duplicate.getJSONArray("assets").put(duplicate.getJSONArray("assets").getJSONObject(1));
        assertThrows(IOException.class, () -> parse(duplicate, 429000));
        JSONObject untrusted = release(new JSONArray().put(asset("arm64-v8a")));
        untrusted.getJSONArray("assets").getJSONObject(1).put("browser_download_url", "https://github.com/another/project/metadata.json");
        assertThrows(IOException.class, () -> parse(untrusted, 429000));
        JSONObject oversized = release(new JSONArray().put(asset("arm64-v8a")));
        oversized.getJSONArray("assets").getJSONObject(1).put("size", LauncherAppRelease.MAX_METADATA_BYTES + 1);
        assertThrows(IOException.class, () -> parse(oversized, 429000));
    }
}
