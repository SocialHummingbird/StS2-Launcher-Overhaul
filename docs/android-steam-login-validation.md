# Android Steam login validation

Updated: 2026-08-22

## Implementation boundary

The launcher keeps Steam authentication because it is required to verify ownership, download the game, and synchronize saves. Local save commits and game launch remain available when saved credentials are missing or rejected; automatic synchronization then fails open and leaves local changes queued.

The native Android credential panel must:

- Accept username and password input.
- Support Steam Guard challenges.
- Clear password fields after submit, cancel, or expiry.
- Avoid storing or injecting a Steam password.
- Keep refresh tokens encrypted through the existing Android Keystore path.
- Keep logs free of credentials and tokens.

## Checks available without a device

- Managed compilation.
- Basic launcher navigation and action wiring on desktop.

The focused save tests do not validate authentication. Their fake remote proves neither Steam nor Android transport.

## Checks requiring a device

- Keyboard and focus behavior.
- Password-manager suggestions.
- Steam Guard interaction.
- Real authentication, ownership, and download.
- Process death and resume behavior.

RC4's 10/10 device-launch matrix reused the installed Steam session and selected `public-beta` runtime. It did not exercise a fresh password handoff, Steam Guard, ownership discovery, a live N → N+1 depot update, or real save transfer. Those results therefore remain unclaimed even though runtime identity and launcher handoff passed on the exact RC4 device.

For diagnostics, prefer **Help → Diagnostics → Create support report** and a focused logcat window. Review both before sharing. Never publish passwords, Steam Guard codes, refresh/session tokens, QR/login payloads, account identifiers, private save data, or unreviewed full logs.
