# Updating the launcher in the app

The launcher checks GitHub for a new release after it opens. If one is available, it offers **Update** or **Later**. You can check yourself with **Help → Check for app updates**.

## Install an update

1. Choose Update to download the APK. You can cancel the download.
2. Wait for the download and file checks to finish.
3. If Android asks, allow the launcher to install apps from this source, then return to the app.
4. Confirm the update in Android's installer.

The updater does not uninstall the app or change saves, login details, mods, or downloaded game versions. Game updates are separate and live on the **Versions** page.

You need to install the first APK normally before you can use this feature. GitHub login is not required to download an app update.

## If the update is unavailable

An automatic check failing in the background does not interrupt play. A manual check shows the error. You can also download the APK from the [release page](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest).

The updater rejects APKs with a missing checksum, the wrong architecture or package, a different signing certificate, or a version code that is not newer. Don't uninstall the app to bypass an incompatible-update error; that removes local saves.

## For release maintainers

Publish a non-draft, non-prerelease GitHub release and mark it latest. Keep the package name and signing certificate, increase the Android version code, and use a comparable version tag. A higher numeric version such as `v0.2.432` avoids ambiguity between differently named release candidates.

Name APK assets with their ABI suffix, such as `-arm64-v8a.apk`. The updater uses GitHub's `sha256:` asset digest and checks file size, package, signature, minimum Android version, and native architecture before handing the APK to Android. Signing-key rotation is not supported by this implementation.

Unit tests cover release selection and rejection cases. The full download, permission, installation, and relaunch cycle still needs a device test with a newer compatible APK.
