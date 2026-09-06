package com.game.sts2launcher;

import org.json.JSONArray;
import org.json.JSONObject;
import java.io.IOException;
import java.net.URI;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/** Release selection is independent of Android UI and installation. */
final class LauncherAppRelease {
    static final String REPOSITORY = "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul";
    final String version, url, sha256;
    final long size;

    private LauncherAppRelease(String version, String url, String sha256, long size) {
        this.version = version; this.url = url; this.sha256 = sha256; this.size = size;
    }

    static LauncherAppRelease parse(String json, String installed, String[] abis) throws Exception {
        JSONObject release = new JSONObject(json);
        if (release.optBoolean("draft") || release.optBoolean("prerelease")) return null;
        String version = release.getString("tag_name");
        if (!isNewer(version, installed)) return null;
        JSONArray assets = release.getJSONArray("assets");
        for (String abi : abis) {
            LauncherAppRelease match = null;
            for (int i = 0; i < assets.length(); i++) {
                JSONObject asset = assets.getJSONObject(i);
                String name = asset.optString("name").toLowerCase(Locale.ROOT);
                if (!name.endsWith("-" + abi.toLowerCase(Locale.ROOT) + ".apk")) continue;
                String url = asset.getString("browser_download_url");
                if (!url.startsWith(REPOSITORY + "/releases/download/") || !isTrustedUrl(url))
                    throw new IOException("The release contains an unexpected APK download address.");
                String digest = asset.optString("digest");
                if (!digest.matches("sha256:[a-fA-F0-9]{64}"))
                    throw new IOException("This release has no SHA-256 checksum. In-app update is unavailable.");
                long size = asset.getLong("size");
                if (size <= 0 || size > 1024L * 1024 * 1024)
                    throw new IOException("The release APK size is invalid.");
                if (match != null) throw new IOException("This release has multiple APKs for this device. In-app update cannot choose safely.");
                match = new LauncherAppRelease(version, url, digest.substring(7), size);
            }
            if (match != null) return match;
        }
        throw new IOException("A newer release exists, but it has no APK for this device's architecture.");
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

    static boolean isNewer(String candidate, String installed) throws IOException {
        Pattern pattern = Pattern.compile("^[vV]?(\\d+)\\.(\\d+)\\.(\\d+)(.*)$");
        Matcher a = pattern.matcher(candidate == null ? "" : candidate);
        Matcher b = pattern.matcher(installed == null ? "" : installed);
        if (!a.matches() || !b.matches()) throw new IOException("The launcher version could not be compared.");
        for (int i = 1; i <= 3; i++) {
            int compare = new java.math.BigInteger(a.group(i)).compareTo(new java.math.BigInteger(b.group(i)));
            if (compare != 0) return compare > 0;
        }
        if (a.group(4).equals(b.group(4))) return false;
        if (a.group(4).isEmpty()) return true;
        if (b.group(4).isEmpty()) return false;
        // Compare revisions only within the same release family (e.g. rc2 -> rc3).
        Pattern revision = Pattern.compile("^(.*?)(\\d+)$");
        Matcher ar = revision.matcher(a.group(4)), br = revision.matcher(b.group(4));
        return ar.matches() && br.matches() && ar.group(1).equals(br.group(1))
            && new java.math.BigInteger(ar.group(2)).compareTo(new java.math.BigInteger(br.group(2))) > 0;
    }
}
