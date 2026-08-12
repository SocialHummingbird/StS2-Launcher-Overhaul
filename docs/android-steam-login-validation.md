# Android Steam login validation

Updated: 2026-08-13

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

No device is currently connected. The recent limited mod-loading run did not exercise or validate authentication, ownership, or download, so those results remain unclaimed.

Never publish passwords, Steam Guard codes, refresh tokens, account identifiers, private save data, or unreviewed full logs.
