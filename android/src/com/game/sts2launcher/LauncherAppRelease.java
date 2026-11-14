package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONObject;
import java.io.IOException;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Locale;

/** Release selection is independent of Android UI and installation. */
final class LauncherAppRelease {
    static final String REPOSITORY = "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul";
    static final int MAX_METADATA_BYTES = 64 * 1024;
    interface MetadataSource { byte[] read(String url) throws Exception; }
    final String version, url, sha256;
    final long size, versionCode;

    private LauncherAppRelease(String version, long versionCode, String url, String sha256, long size) {
        this.version = version; this.versionCode = versionCode; this.url = url; this.sha256 = sha256; this.size = size;
    }

    static LauncherAppRelease parse(String json, String installedPackage, long installedVersionCode,
            String[] abis, MetadataSource metadataSource) throws Exception {
        if (installedVersionCode <= 0) throw new IOException("The installed launcher build number is invalid.");
        JSONObject release = new JSONObject(json);
        if (release.optBoolean("draft") || release.optBoolean("prerelease")) return null;
        JSONArray assets = release.getJSONArray("assets");
        for (String abi : abis) {
            JSONObject match = null;
            for (int i = 0; i < assets.length(); i++) {
                JSONObject asset = assets.getJSONObject(i);
                String name = asset.optString("name").toLowerCase(Locale.ROOT);
                if (!name.endsWith("-" + abi.toLowerCase(Locale.ROOT) + ".apk")) continue;
                if (match != null) throw new IOException("This release has multiple APKs for this device. In-app update cannot choose safely.");
                match = asset;
            }
            if (match != null) return selectUpdate(match, assets, installedPackage, installedVersionCode, metadataSource);
        }
        throw new IOException("The latest release has no APK for this device's architecture.");
    }

    private static LauncherAppRelease selectUpdate(JSONObject apk, JSONArray assets, String installedPackage,
            long installedVersionCode, MetadataSource source) throws Exception {
        String name = apk.getString("name");
        String url = downloadUrl(apk);
        String digest = digest(apk);
        long size = apk.getLong("size");
        if (size <= 0 || size > 1024L * 1024 * 1024) throw new IOException("The release APK size is invalid.");

        JSONObject companion = null;
        for (int i = 0; i < assets.length(); i++) {
            JSONObject asset = assets.getJSONObject(i);
            if (!asset.optString("name").equals(name + ".json")) continue;
            if (companion != null) throw new IOException("The release has duplicate APK build metadata.");
            companion = asset;
        }
        if (companion == null) throw new IOException("This release has no APK build metadata. Its build number cannot be checked.");
        String metadataUrl = downloadUrl(companion);
        if (!metadataUrl.equals(url + ".json")) throw new IOException("The APK build metadata download address does not match.");
        String metadataDigest = digest(companion);
        long metadataSize = companion.getLong("size");
        if (metadataSize <= 0 || metadataSize > MAX_METADATA_BYTES) throw new IOException("The APK build metadata size is invalid.");
        byte[] bytes = source.read(metadataUrl);
        if (bytes == null || bytes.length != metadataSize || !sha256(bytes).equalsIgnoreCase(metadataDigest))
            throw new IOException("APK build metadata verification failed.");
        JSONObject metadata = new JSONObject(new String(bytes, StandardCharsets.UTF_8));
        if (!name.equals(metadata.optString("apk")) || !digest.equalsIgnoreCase(metadata.optString("sha256")))
            throw new IOException("The build metadata describes a different APK.");
        if (!installedPackage.equals(metadata.optString("packageName")))
            throw new IOException("This release is for a different app package.");
        Object rawCode = metadata.opt("versionCode");
        if (!(rawCode instanceof Number) || !rawCode.toString().matches("[1-9][0-9]*"))
            throw new IOException("The release build number is invalid.");
        long code;
        try { code = Long.parseLong(rawCode.toString()); }
        catch (NumberFormatException error) { throw new IOException("The release build number is invalid.", error); }
        String version = metadata.getString("versionName");
        if (version.trim().isEmpty()) throw new IOException("The release version name is missing.");
        // Android orders APKs by versionCode. A stable tag must not downgrade a
        // later build whose display name still has a local or validation suffix.
        return code > installedVersionCode ? new LauncherAppRelease(version, code, url, digest, size) : null;
    }

    private static String downloadUrl(JSONObject asset) throws Exception {
        String url = asset.getString("browser_download_url");
        if (!url.startsWith(REPOSITORY + "/releases/download/") || !isTrustedUrl(url))
            throw new IOException("The release contains an unexpected download address.");
        return url;
    }

    private static String digest(JSONObject asset) throws IOException {
        String digest = asset.optString("digest");
        if (!digest.matches("sha256:[a-fA-F0-9]{64}"))
            throw new IOException("This release has no SHA-256 checksum. In-app update is unavailable.");
        return digest.substring(7);
    }

    private static String sha256(byte[] bytes) throws Exception {
        StringBuilder result = new StringBuilder();
        for (byte value : MessageDigest.getInstance("SHA-256").digest(bytes))
            result.append(String.format(Locale.ROOT, "%02x", value & 255));
        return result.toString();
    }

    static boolean isTrustedUrl(String value) {
        try {
            URI uri = URI.create(value);
            String host = uri.getHost();
            return "https".equalsIgnoreCase(uri.getScheme()) && uri.getUserInfo() == null
                && (uri.getPort() == -1 || uri.getPort() == 443)
                && ("github.com".equalsIgnoreCase(host) || "api.github.com".equalsIgnoreCase(host)
                    || "release-assets.githubusercontent.com".equalsIgnoreCase(host)
                    || "objects.githubusercontent.com".equalsIgnoreCase(host));
        } catch (Exception ignored) { return false; }
    }

}
