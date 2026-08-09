# Steam version selection architecture

## Purpose

Version selection lets an authenticated Steam owner discover, download, cache, and launch an account-visible game branch without overwriting another downloaded branch.

## Model

Each option records a branch name, display label, availability, optional build metadata, and whether a password is required. Public/default remains the fallback selection.

## Storage

- Public/default uses the existing primary game slot.
- Non-public branches use stable side-by-side runtime slots.
- Slot metadata binds the branch, manifest/build identity, selected PCK, runtime pack, and patch-validation result.
- Cache cleanup removes only the explicitly selected inactive slot.

## Download and launch

1. Steam authentication establishes ownership and account-visible branch metadata.
2. Refresh reads branch metadata without downloading game files.
3. Download/update writes only the selected slot.
4. Launch readiness checks the selected slot, branch marker, runtime pack, and patch-validation evidence.
5. A missing or mismatched selected runtime blocks that runtime launch with a direct repair action.

Gameplay launch and version activation do not move gameplay saves.

## Diagnostics

Diagnostics should report the selected branch, availability, slot directory, PCK identity, runtime-pack identity, branch marker, and the first readiness failure. They should not recreate a separate evidence framework.

## Limitations

Private/password branch entry is not implemented. Static and desktop checks cannot prove Android runtime compatibility.
