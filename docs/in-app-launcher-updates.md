# In-app launcher updates

The Android launcher checks GitHub's latest published, non-prerelease release once per activity, after its first UI frame. Checks happen in the background. A newer compatible APK produces an **Update / Later** prompt with the installed version, available version, and download size. Network failures during automatic checks do not interrupt play.

**Help → Check for app updates** performs an explicit check and reports both up-to-date and failure outcomes. This updates the launcher APK; the Versions page continues to manage downloaded game files.

After Update is selected, a cancellable download writes only to the app cache. Before installation, the launcher verifies the release asset size and SHA-256 digest, package name, increased Android version code, signing certificates, minimum Android version, and native architecture. APKs with missing checksums or ambiguous architecture matches are rejected. Signing-key rotation is intentionally not accepted by this first implementation.

Android may require **Allow from this source** permission. The launcher opens that setting and continues with the verified cached APK when the user returns. Android then presents its normal install confirmation. No GitHub login or browser visit is needed, and the updater never uninstalls the existing app or modifies saves, credentials, mods, or downloaded game files.

The first APK containing this feature still needs to be installed normally. Subsequent releases must keep the same package/signing identity, increase `versionCode`, use comparable version tags (including numbered RC revisions within the same release family), publish an ABI-suffixed APK such as `-arm64-v8a.apk`, and include GitHub's `sha256:` asset digest.

Validation: Android release Java compilation; 10 JVM tests for version comparison, release/ABI selection, URL restrictions, checksums, and invalid metadata; managed builds and phone/desktop Help-button interaction tests. A complete download → permission → package-install → relaunch cycle still requires a device test with a newer, correctly signed release. The tests do not claim that system installation has been exercised.
