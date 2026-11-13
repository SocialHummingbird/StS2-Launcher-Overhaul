package com.sts2launcher.downloadvalidation;

import android.app.Instrumentation;
import android.os.Bundle;
import java.io.*;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.net.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** A same-signed helper. It never decrypts credentials or invokes launcher/cloud code. */
public final class Preservation extends Instrumentation {
    private Bundle arguments;
    private final JSONArray inventory = new JSONArray();
    private int backupCount;
    private int hashedCount;

    @Override public void onCreate(Bundle args) {
        super.onCreate(args);
        arguments = args == null ? new Bundle() : args;
        start();
    }

    private static String identifier(String value) {
        if (value == null || !value.matches("[a-zA-Z0-9][a-zA-Z0-9-]{0,79}"))
            throw new IllegalArgumentException("Invalid evidence/run identifier");
        return value;
    }

    private File validationRoot() throws Exception {
        File root = new File(getTargetContext().getFilesDir(), "download-validation");
        if (!root.mkdirs() && !root.isDirectory()) throw new IOException("Cannot create validation root");
        return root;
    }

    private File evidenceRoot(String name) throws Exception {
        File root = new File(getTargetContext().getExternalFilesDir(null), "download-validation-evidence/" + identifier(name));
        if (!root.mkdirs() && !root.isDirectory()) throw new IOException("Cannot create evidence directory");
        return root;
    }

    private static void writeJson(File file, JSONObject value) throws Exception {
        File temporary = new File(file.getParentFile(), file.getName() + ".tmp");
        try (Writer out = new OutputStreamWriter(new FileOutputStream(temporary), "UTF-8")) {
            out.write(value.toString(2));
        }
        Files.move(temporary.toPath(), file.toPath(), java.nio.file.StandardCopyOption.REPLACE_EXISTING);
    }

    private static String hash(File file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        try (InputStream input = new BufferedInputStream(new FileInputStream(file))) {
            byte[] block = new byte[1024 * 1024];
            for (int count; (count = input.read(block)) != -1;) digest.update(block, 0, count);
        }
        StringBuilder result = new StringBuilder();
        for (byte value : digest.digest()) result.append(String.format(Locale.ROOT, "%02x", value & 255));
        return result.toString();
    }

    private static String kind(String path) {
        String lower = path.toLowerCase(Locale.ROOT);
        if (lower.equals("private/files/profileinstalled")) return "runtime-metadata";
        if (lower.endsWith("/steam_credentials.enc")) return "credential";
        if (lower.startsWith("private/shared_prefs/") || lower.endsWith(".save")
            || lower.contains("/profile") || lower.contains("/modded/")
            || lower.endsWith(".save.bak") || lower.endsWith(".save.backup")
            || lower.endsWith("/mod_selection.json") || lower.endsWith("/last_mod_launch.json")
            || lower.endsWith("/game_branch") || lower.endsWith("/current_runtime_slot.json")
            || lower.endsWith("/workshop_sync_manifest.json")
            || lower.matches("private/files/[^/]*(preferences|settings)[^/]*")) return "preserved";
        if (lower.startsWith("private/files/game/") || lower.startsWith("private/files/game_versions/")
            || lower.startsWith("private/files/download_state/")
            || lower.endsWith("/installation_state.json")) return "installed";
        return "other";
    }

    private static boolean backup(String path, String kind) {
        if (kind.equals("credential")) return false;
        return kind.equals("preserved") || path.endsWith("/installation_state.json")
            || path.endsWith(".manifest") || path.endsWith(".id") || path.endsWith("/steam_branch.txt");
    }

    private void walk(File root, File file, String prefix, ZipOutputStream archive) throws Exception {
        if (Files.isSymbolicLink(file.toPath())) throw new IOException("Snapshot encountered a symbolic link");
        String relative = root.toPath().relativize(file.toPath()).toString().replace('\\', '/');
        String path = prefix + (relative.isEmpty() ? "" : "/" + relative);
        if (path.equals("private/files/download-validation") || path.startsWith("external/download-validation-evidence")) return;
        if (file.isDirectory()) {
            // Runtime payloads and caches are outside saves, preferences, credentials and installed Steam versions.
            if (Arrays.asList("cache", "code_cache", "dotnet_bcl", "runtime_packs", ".generations").contains(file.getName())) return;
            File[] children = file.listFiles();
            if (children == null) throw new IOException("Cannot inventory a protected directory");
            Arrays.sort(children, new Comparator<File>() {
                @Override public int compare(File left, File right) { return left.getName().compareTo(right.getName()); }
            });
            for (File child : children) walk(root, child, prefix, archive);
            return;
        }
        String category = kind(path);
        JSONObject item = new JSONObject().put("path", path).put("size", file.length())
            .put("modified", file.lastModified()).put("kind", category);
        if (!category.equals("other")) { item.put("sha256", hash(file)); hashedCount++; }
        inventory.put(item);
        if (backup(path, category)) {
            archive.putNextEntry(new ZipEntry(path));
            try (InputStream input = new BufferedInputStream(new FileInputStream(file))) {
                byte[] block = new byte[65536];
                for (int count; (count = input.read(block)) != -1;) archive.write(block, 0, count);
            }
            archive.closeEntry();
            backupCount++;
        }
    }

    private void snapshot(Bundle result, String name) throws Exception {
        File output = evidenceRoot(name);
        File data = getTargetContext().getDataDir();
        try (ZipOutputStream archive = new ZipOutputStream(new BufferedOutputStream(new FileOutputStream(new File(output, "snapshot.zip"))))) {
            for (String child : Arrays.asList("files", "shared_prefs")) {
                File root = new File(data, child);
                if (root.exists()) walk(root, root, "private/" + child, archive);
            }
            File external = getTargetContext().getExternalFilesDir(null);
            if (external != null && external.exists()) walk(external, external, "external", archive);
            File publicRoot = new File("/storage/emulated/0/StS2Launcher");
            if (publicRoot.exists()) walk(publicRoot, publicRoot, "public", archive);
        }
        JSONObject summary = new JSONObject().put("schema", 1).put("inventory", inventory)
            .put("backedUpFiles", backupCount).put("hashedFiles", hashedCount)
            .put("credentialBytesExported", false).put("cloudOperations", 0);
        writeJson(new File(output, "inventory.json"), summary);
        result.putString("evidence", output.toString());
        result.putInt("inventoryFiles", inventory.length());
        result.putInt("backedUpFiles", backupCount);
        result.putInt("hashedFiles", hashedCount);
    }

    private void request(Bundle result) throws Exception {
        String runId = identifier(arguments.getString("runId"));
        String operation = arguments.getString("operation", "download");
        if (!Arrays.asList("download", "copied-update", "fixture", "local-fixture", "cdn-diagnostics", "sandbox-ui", "hold").contains(operation))
            throw new IllegalArgumentException("Unsupported validation operation");
        String branch = arguments.getString("branch", "public");
        if (!branch.matches("[a-zA-Z0-9_-]+")) throw new IllegalArgumentException("Invalid branch");
        JSONObject request = new JSONObject().put("RunId", runId).put("Operation", operation).put("Branch", branch)
            .put("Provider", arguments.getString("provider", "default"))
            .put("Scenario", arguments.getString("scenario", "fresh"))
            .put("SourceRunId", arguments.getString("sourceRunId", "none"))
            .put("CancelAfterBytes", Long.parseLong(arguments.getString("cancelAfterBytes", "0")));
        writeJson(new File(validationRoot(), "request.json"), request);
        result.putString("request", "prepared");
        result.putString("runId", runId);
    }

    private static void removeOwnedTree(File target, String bound) throws Exception {
        String path = target.getCanonicalPath();
        if (!(path.equals(bound) || path.startsWith(bound + "/")) || Files.isSymbolicLink(target.toPath()))
            throw new IOException("Validation cleanup escaped its test-owned directory");
        if (!target.exists()) return;
        if (target.isDirectory()) {
            File[] children = target.listFiles();
            if (children == null) throw new IOException("Cannot enumerate validation cleanup directory");
            for (File child : children) removeOwnedTree(child, bound);
        }
        Files.delete(target.toPath());
    }

    private void cleanup(Bundle result) throws Exception {
        File privateRoot = new File(getTargetContext().getFilesDir(), "download-validation");
        File externalRoot = new File(getTargetContext().getExternalFilesDir(null), "download-validation-evidence");
        String privateBound = getTargetContext().getFilesDir().getCanonicalPath() + "/download-validation";
        String externalBound = getTargetContext().getExternalFilesDir(null).getCanonicalPath() + "/download-validation-evidence";
        if (!privateRoot.getCanonicalPath().equals(privateBound) || !externalRoot.getCanonicalPath().equals(externalBound))
            throw new IOException("Validation cleanup roots are not their explicitly named directories");
        int responseFiles = 0;
        File[] runs = privateRoot.listFiles();
        if (runs != null) for (File run : runs) {
            File list = new File(run, "tracked-http-files.txt");
            if (!list.isFile()) continue;
            try (BufferedReader reader = new BufferedReader(new FileReader(list))) {
                for (String entry; (entry = reader.readLine()) != null;) {
                    File file = new File(entry);
                    String parent = file.getCanonicalFile().getParent();
                    if ((!parent.equals(getTargetContext().getCacheDir().getCanonicalPath())
                        && !parent.equals(new File(getTargetContext().getFilesDir(), "tmp").getCanonicalPath()))
                        || !file.getName().matches("sts2_cdn_[A-Za-z0-9_-]+\\.bin") || Files.isSymbolicLink(file.toPath()))
                        throw new IOException("Recorded response file escaped its diagnostic ownership");
                    if (Files.deleteIfExists(file.toPath())) responseFiles++;
                }
            }
        }
        removeOwnedTree(privateRoot, privateBound);
        removeOwnedTree(externalRoot, externalBound);
        result.putString("cleanup", "Only private download-validation and external download-validation-evidence removed");
        result.putInt("trackedDiagnosticResponseFilesRemoved", responseFiles);
    }

    private JSONObject probeUrl(String host, String path, boolean directory, List<String> hosts) throws Exception {
        return probeUrl("https", host, path, directory, hosts, 3000);
    }

    private JSONObject probeUrl(String scheme, String host, String path, boolean directory, List<String> hosts, int timeout) throws Exception {
        long started = android.os.SystemClock.elapsedRealtime();
        JSONObject output = new JSONObject().put("scheme", scheme).put("host", host).put("port", scheme.equals("https") ? 443 : 80);
        HttpURLConnection connection = null;
        try {
            try {
                JSONArray addresses = new JSONArray();
                for (InetAddress address : InetAddress.getAllByName(host)) addresses.put(address.getHostAddress());
                output.put("dnsAddresses", addresses);
            } catch (Throwable dns) {
                output.put("dnsErrorType", dns.getClass().getName()).put("dnsErrorMessage", dns.getMessage());
                if (dns.getCause() != null) output.put("dnsErrorCause", dns.getCause().toString());
            }
            output.put("dnsFinishedMs", android.os.SystemClock.elapsedRealtime() - started);
            JSONArray proxyTypes = new JSONArray();
            for (Proxy proxy : ProxySelector.getDefault().select(new URI(scheme + "://" + host + path))) proxyTypes.put(proxy.type().toString());
            output.put("proxyTypes", proxyTypes);
            connection = (HttpURLConnection)new URL(scheme, host, path).openConnection();
            connection.setConnectTimeout(timeout);
            connection.setReadTimeout(timeout);
            connection.setInstanceFollowRedirects(false);
            connection.setRequestProperty("User-Agent", "StS2DownloadValidation/1.0");
            connection.connect();
            output.put("connectedMs", android.os.SystemClock.elapsedRealtime() - started);
            int status = connection.getResponseCode();
            output.put("status", status).put("headersMs", android.os.SystemClock.elapsedRealtime() - started)
                .put("declaredContentLength", connection.getContentLengthLong())
                .put("transferEncoding", connection.getHeaderField("Transfer-Encoding"))
                .put("connectionHeader", connection.getHeaderField("Connection"));
            InputStream body = status >= 400 ? connection.getErrorStream() : connection.getInputStream();
            ByteArrayOutputStream bytes = new ByteArrayOutputStream();
            if (body != null) try (InputStream input = body) {
                byte[] block = new byte[16384];
                for (int count; bytes.size() < 1024 * 1024 && (count = input.read(block)) != -1;) {
                    if (bytes.size() == 0) output.put("firstBodyByteMs", android.os.SystemClock.elapsedRealtime() - started);
                    bytes.write(block, 0, count);
                    output.put("lastBodyByteMs", android.os.SystemClock.elapsedRealtime() - started);
                }
                output.put("bodyEofMs", android.os.SystemClock.elapsedRealtime() - started);
            }
            output.put("bodyClosedMs", android.os.SystemClock.elapsedRealtime() - started);
            output.put("responseBytes", bytes.size());
            if (directory && status == 200) {
                JSONArray servers = new JSONObject(new String(bytes.toByteArray(), "UTF-8")).getJSONObject("response").getJSONArray("servers");
                for (int index = 0; index < servers.length() && hosts.size() < 5; index++) {
                    JSONObject server = servers.getJSONObject(index);
                    String candidate = server.optString("vhost", server.optString("host"));
                    if (candidate.matches("[a-zA-Z0-9.-]+") && !hosts.contains(candidate)) hosts.add(candidate);
                }
            }
        } catch (Throwable error) { output.put("errorType", error.getClass().getName()); }
        finally {
            if (connection != null) {
                output.put("disconnectStartedMs", android.os.SystemClock.elapsedRealtime() - started);
                connection.disconnect();
                output.put("disconnectFinishedMs", android.os.SystemClock.elapsedRealtime() - started);
            }
        }
        return output.put("elapsedMs", android.os.SystemClock.elapsedRealtime() - started);
    }

    private void networkProbe(Bundle result, String name) throws Exception {
        List<String> hosts = new ArrayList<String>();
        JSONArray probes = new JSONArray();
        probes.put(probeUrl("api.steampowered.com", "/ISteamWebAPIUtil/GetServerInfo/v1/", false, hosts));
        probes.put(probeUrl("api.steampowered.com", "/IContentServerDirectoryService/GetServersForSteamPipe/v1/?cell_id=0&max_servers=5", true, hosts));
        if (hosts.isEmpty()) hosts.add("steamcdn-a.akamaihd.net");
        for (String host : hosts) probes.put(probeUrl(host, "/", false, hosts));
        JSONObject output = new JSONObject().put("transport", "Direct Java HttpsURLConnection, no Godot bridge")
            .put("authenticatedRequests", 0).put("probes", probes);
        File path = new File(evidenceRoot(name), "network-probe.json");
        writeJson(path, output);
        result.putString("networkProbe", output.toString());
        result.putString("evidence", path.toString());
    }

    private void chunkProbe(Bundle result, String name) throws Exception {
        String host = arguments.getString("host", "cache10-lhr1.steamcontent.com");
        String scheme = arguments.getString("scheme", "https");
        String depot = arguments.getString("depot");
        String chunk = arguments.getString("chunk");
        if (!(host.matches("[a-zA-Z0-9.-]+\\.steamcontent\\.com") || host.equals("steamcdn-a.akamaihd.net")) || !Arrays.asList("http", "https").contains(scheme)
            || depot == null || !depot.matches("[0-9]{1,10}") || chunk == null || !chunk.matches("[A-Fa-f0-9]{40}"))
            throw new IllegalArgumentException("Invalid public CDN chunk probe parameters");
        JSONObject probe = probeUrl(scheme, host, "/depot/" + depot + "/chunk/" + chunk.toLowerCase(Locale.ROOT), false, new ArrayList<String>(), 30000);
        probe.put("transport", "Direct Java HttpURLConnection, no Godot bridge").put("authenticatedRequests", 0);
        probe.put("processUid", android.os.Process.myUid()).put("targetUid", getTargetContext().getApplicationInfo().uid)
            .put("targetInternetPermission", getTargetContext().checkSelfPermission("android.permission.INTERNET"))
            .put("helperInternetPermission", getContext().checkSelfPermission("android.permission.INTERNET"));
        writeJson(new File(evidenceRoot(name), "chunk-probe.json"), probe);
        result.putString("chunkProbe", probe.toString());
    }

    @Override public void onStart() {
        Bundle result = new Bundle();
        try {
            String action = arguments.getString("action", "snapshot");
            if (action.equals("snapshot")) snapshot(result, identifier(arguments.getString("name", "before")));
            else if (action.equals("request")) request(result);
            else if (action.equals("cleanup")) cleanup(result);
            else if (action.equals("network-probe")) networkProbe(result, identifier(arguments.getString("name", "network-probe")));
            else if (action.equals("chunk-probe")) chunkProbe(result, identifier(arguments.getString("name", "chunk-probe")));
            else throw new IllegalArgumentException("Unsupported helper action");
            finish(0, result);
        } catch (Throwable error) {
            // Deliberately omit exception messages and file contents from the instrumentation channel.
            result.putString("errorType", error.getClass().getName());
            finish(1, result);
        }
    }
}
