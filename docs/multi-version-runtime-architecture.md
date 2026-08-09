# Multi-version runtime architecture

## Objective

Keep downloaded Steam branches in separate local runtime slots and start the selected slot without mixing game assets.

## Runtime slot

A slot contains:

- Downloaded game files.
- Selected PCK identity.
- Runtime-pack metadata and patched managed assemblies.
- Branch/build provenance.
- Patch-validation result.

The slot identifier is stable, filesystem-safe, and derived from the selected branch. Public/default keeps the existing primary slot for compatibility.

## Activation

Before launch, the launcher verifies that the selected slot is complete and that its PCK, runtime pack, active assembly cache, and branch marker agree. It either activates that slot or reports one actionable readiness failure.

Activation must not move gameplay saves or block launch on save state.

## Cleanup

Cleanup targets one explicit inactive slot. It must not remove the active slot, application package data, credentials, mods, or gameplay saves.

## Validation boundary

Compilation, fixture checks, and metadata inspection can validate slot isolation and readiness rules. Android launch compatibility still requires an appropriate runtime test when hardware is available.
