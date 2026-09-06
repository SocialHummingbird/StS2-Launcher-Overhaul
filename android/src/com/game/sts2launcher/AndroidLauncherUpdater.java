package com.game.sts2launcher;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.pm.Signature;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import androidx.core.content.FileProvider;
import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** Owns one foreground, consent-based APK update; never uninstalls or touches game data. */
final class AndroidLauncherUpdater {
    private static final String API = "https://api.github.com/repos/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest";
    private final Activity activity;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    private boolean busy, checked, foreground, launcherActive;
    private Runnable pending;
    private volatile boolean cancelled;
    private AlertDialog dialog;
    private ProgressBar progress;
    private TextView progressText;

    AndroidLauncherUpdater(Activity activity) { this.activity = activity; }

    void setLauncherActive(boolean active) {
        launcherActive = active;
        if (active) resume();
    }

    void check(boolean manual) {
        if (busy || (!manual && checked)) return;
        checked = true;
        busy = true;
        cancelled = false;
        if (manual) showProgress("Checking for app updates", false);
        worker.execute(() -> {
            try {
                HttpURLConnection connection = open(API);
                String json;
                try (InputStream in = connection.getInputStream(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
                    byte[] buffer = new byte[8192]; int read;
                    while ((read = in.read(buffer)) != -1) {
                        if (out.size() + read > 2 * 1024 * 1024) throw new IOException("Release response was too large.");
                        out.write(buffer, 0, read);
                    }
                    json = new String(out.toByteArray(), StandardCharsets.UTF_8);
                } finally { connection.disconnect(); }
                String installed = activity.getPackageManager().getPackageInfo(activity.getPackageName(), 0).versionName;
                LauncherAppRelease release = LauncherAppRelease.parse(json, installed, Build.SUPPORTED_ABIS);
                deliver(() -> {
                    dismiss();
                    if (cancelled) { busy = false; return; }
                    if (release == null) {
                        busy = false;
                        if (manual) message("You're up to date", "Installed launcher: " + installed);
                    } else {
                        dialog = new AlertDialog.Builder(activity)
                            .setTitle("Launcher update available")
                            .setMessage("Installed: " + installed + "\nAvailable: " + release.version
                                + String.format(java.util.Locale.ROOT, "\nDownload: %.1f MB", release.size / 1048576.0)
                                + "\n\nDownload and install this update? Your saves, settings, and downloaded game files stay in place.")
                            .setPositiveButton("Update", (d, which) -> download(release))
                            .setNegativeButton("Later", (d, which) -> busy = false)
                            .setOnCancelListener(d -> busy = false).show();
                    }
                });
            } catch (Exception ex) {
                deliver(() -> {
                    dismiss(); busy = false;
                    if (manual && !cancelled) message("Could not check for updates", readable(ex));
                    android.util.Log.w("STS2Mobile", "Launcher update check failed", ex);
                });
            }
        });
    }

    private File apkFile() { return new File(activity.getCacheDir(), "launcher-update.apk"); }

    private void download(LauncherAppRelease release) {
        cancelled = false;
        showProgress("Downloading " + release.version, true);
        worker.execute(() -> {
            File partial = new File(activity.getCacheDir(), "launcher-update-" + java.util.UUID.randomUUID() + ".apk.part");
            try {
                if (activity.getCacheDir().getUsableSpace() < release.size * 2 + 32L * 1024 * 1024)
                    throw new IOException("Not enough free space to download and install this update.");
                MessageDigest hash = MessageDigest.getInstance("SHA-256");
                HttpURLConnection connection = open(release.url);
                long total = 0, lastProgress = 0;
                try (InputStream in = connection.getInputStream(); FileOutputStream out = new FileOutputStream(partial)) {
                    byte[] buffer = new byte[65536]; int read;
                    while ((read = in.read(buffer)) != -1) {
                        if (cancelled) throw new InterruptedIOException("Update cancelled.");
                        total += read;
                        if (total > release.size) throw new IOException("APK size does not match the release.");
                        out.write(buffer, 0, read); hash.update(buffer, 0, read);
                        long now = System.currentTimeMillis();
                        if (now - lastProgress >= 250) {
                            lastProgress = now; final long downloaded = total;
                            activity.runOnUiThread(() -> {
                                if (progress != null) {
                                    progress.setProgress((int)(downloaded * 100 / release.size));
                                    progressText.setText(String.format(java.util.Locale.ROOT, "%.1f / %.1f MB", downloaded / 1048576.0, release.size / 1048576.0));
                                }
                            });
                        }
                    }
                    out.getFD().sync();
                } finally { connection.disconnect(); }
                if (cancelled) throw new InterruptedIOException("Update cancelled.");
                if (total != release.size || !hex(hash.digest()).equalsIgnoreCase(release.sha256))
                    throw new IOException("Update verification failed. Please download it again.");
                validateApk(partial);
                if (cancelled) throw new InterruptedIOException("Update cancelled.");
                if (apkFile().exists() && !apkFile().delete()) throw new IOException("Could not replace the cached update.");
                if (!partial.renameTo(apkFile())) throw new IOException("Could not save the verified update.");
                deliver(() -> {
                    dismiss(); busy = false;
                    if (!cancelled) install();
                });
            } catch (Exception ex) {
                partial.delete();
                deliver(() -> {
                    dismiss(); busy = false;
                    if (!cancelled) message("Update not installed", readable(ex));
                });
            }
        });
    }

    private void validateApk(File apk) throws Exception {
        PackageManager pm = activity.getPackageManager();
        int flags = Build.VERSION.SDK_INT >= 28 ? PackageManager.GET_SIGNING_CERTIFICATES : PackageManager.GET_SIGNATURES;
        PackageInfo current = pm.getPackageInfo(activity.getPackageName(), flags);
        PackageInfo target = pm.getPackageArchiveInfo(apk.getAbsolutePath(), flags);
        if (target == null || !activity.getPackageName().equals(target.packageName))
            throw new IOException("This APK is for a different app package. Your installed app has not been changed.");
        if (versionCode(target) <= versionCode(current))
            throw new IOException("This APK is not newer than the installed app.");
        if (target.applicationInfo == null || target.applicationInfo.minSdkVersion > Build.VERSION.SDK_INT)
            throw new IOException("This release requires a newer Android version.");
        if (!signers(signatures(current)).equals(signers(signatures(target))))
            throw new IOException("This APK's signing certificate does not match your installed app. Do not uninstall or clear app data to update.");
        try (java.util.zip.ZipFile zip = new java.util.zip.ZipFile(apk)) {
            boolean compatible = zip.stream().anyMatch(entry -> Arrays.stream(Build.SUPPORTED_ABIS)
                .anyMatch(abi -> entry.getName().startsWith("lib/" + abi + "/") && entry.getName().endsWith(".so")));
            if (!compatible) throw new IOException("This APK does not support your device's architecture.");
        }
    }

    private void install() {
        try {
            validateApk(apkFile());
            if (Build.VERSION.SDK_INT >= 26 && !activity.getPackageManager().canRequestPackageInstalls()) {
                dialog = new AlertDialog.Builder(activity).setTitle("Allow launcher updates")
                    .setMessage("Android needs permission to install updates from StS2 Launcher. Enable ‘Allow from this source’, then return here to continue.")
                    .setPositiveButton("Open settings", (d, which) -> {
                        try {
                            activity.getPreferences(0).edit().putBoolean("launcher_update_permission_pending", true).apply();
                            activity.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + activity.getPackageName())));
                        } catch (Exception ex) {
                            activity.getPreferences(0).edit().remove("launcher_update_permission_pending").apply();
                            message("Could not open settings", readable(ex));
                        }
                    }).setNegativeButton("Later", null).show();
                return;
            }
            Uri uri = FileProvider.getUriForFile(activity, activity.getPackageName() + ".fileprovider", apkFile());
            Intent intent = new Intent(Intent.ACTION_VIEW).setDataAndType(uri, "application/vnd.android.package-archive")
                .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            activity.startActivity(intent);
        } catch (Exception ex) { message("Update not installed", readable(ex)); }
    }

    void resume() {
        foreground = true;
        if (!launcherActive) return;
        if (pending != null) { Runnable action = pending; pending = null; action.run(); }
        if (activity.getPreferences(0).getBoolean("launcher_update_permission_pending", false)) {
            activity.getPreferences(0).edit().remove("launcher_update_permission_pending").apply();
            checked = true;
            if (Build.VERSION.SDK_INT < 26 || activity.getPackageManager().canRequestPackageInstalls()) install();
            else message("Update not installed", "Installation permission was not enabled. You can try again from Help → Check for app updates.");
        }
    }

    void pause() { foreground = false; }
    void destroy() { cancelled = true; pending = null; dismiss(); worker.shutdownNow(); }

    private void deliver(Runnable action) {
        activity.runOnUiThread(() -> {
            if (activity.isDestroyed() || activity.isFinishing()) return;
            if (foreground && launcherActive) action.run(); else pending = action;
        });
    }

    private void showProgress(String title, boolean downloading) {
        dismiss();
        LinearLayout content = new LinearLayout(activity); content.setOrientation(LinearLayout.VERTICAL);
        int padding = (int)(24 * activity.getResources().getDisplayMetrics().density);
        content.setPadding(padding, padding, padding, padding);
        progressText = new TextView(activity); progressText.setText(downloading ? "Starting download…" : "Contacting the release server…");
        content.addView(progressText);
        progress = new ProgressBar(activity, null, android.R.attr.progressBarStyleHorizontal);
        progress.setIndeterminate(!downloading); progress.setMax(100); content.addView(progress);
        dialog = new AlertDialog.Builder(activity).setTitle(title).setView(content)
            .setNegativeButton("Cancel", (d, which) -> cancelled = true)
            .setOnCancelListener(d -> cancelled = true).show();
        dialog.setCanceledOnTouchOutside(false);
    }

    private void dismiss() { if (dialog != null) dialog.dismiss(); dialog = null; progress = null; progressText = null; }
    private void message(String title, String message) {
        if (!activity.isDestroyed() && !activity.isFinishing())
            new AlertDialog.Builder(activity).setTitle(title).setMessage(message).setPositiveButton("OK", null).show();
    }
    private static String readable(Exception ex) { return ex.getMessage() == null ? "Please try again when you have an internet connection." : ex.getMessage(); }
    private static long versionCode(PackageInfo info) {
        return Build.VERSION.SDK_INT >= 28 ? info.getLongVersionCode() : info.versionCode;
    }
    private static Signature[] signatures(PackageInfo info) {
        return Build.VERSION.SDK_INT >= 28
            ? info.signingInfo == null ? null : info.signingInfo.getApkContentsSigners()
            : info.signatures;
    }
    private static Set<String> signers(Signature[] signatures) throws IOException {
        if (signatures == null || signatures.length == 0) throw new IOException("APK signing information is missing.");
        Set<String> result = new HashSet<>();
        for (Signature signature : signatures) result.add(signature.toCharsString());
        return result;
    }
    private static String hex(byte[] bytes) {
        StringBuilder result = new StringBuilder();
        for (byte value : bytes) result.append(String.format(java.util.Locale.ROOT, "%02x", value & 255));
        return result.toString();
    }
    private static HttpURLConnection open(String address) throws Exception {
        for (int redirects = 0; redirects < 6; redirects++) {
            if (!LauncherAppRelease.isTrustedUrl(address)) throw new IOException("Unexpected update download host.");
            HttpURLConnection connection = (HttpURLConnection)new URL(address).openConnection();
            connection.setInstanceFollowRedirects(false); connection.setConnectTimeout(15000); connection.setReadTimeout(30000);
            connection.setRequestProperty("User-Agent", "StS2-Launcher");
            int status = connection.getResponseCode();
            if (status >= 300 && status < 400) {
                String location = connection.getHeaderField("Location"); connection.disconnect();
                if (location == null) throw new IOException("Update redirect has no destination.");
                address = new URL(new URL(address), location).toString(); continue;
            }
            if (status != 200) { connection.disconnect(); throw new IOException("Update server returned HTTP " + status + ". Please try again later."); }
            return connection;
        }
        throw new IOException("Too many update download redirects.");
    }
}
