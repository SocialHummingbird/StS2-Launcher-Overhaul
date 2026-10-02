# Contributing

Keep each change focused on behavior the launcher currently supports.

## Pull requests

Include:

- What changed and why.
- Reproduction steps for a bug fix.
- The focused commands you ran and their results.
- Any device or real-Steam validation performed.
- Important limitations that remain untested.

Do not substitute source-shape checks, broad matrices, or generated evidence bundles for a direct behavior test. If a required check cannot run, state the concrete blocker.

## Focused validation

Use the smallest relevant behavior checks. Operational commands, fixture arguments and APK requirements are maintained in [Building and testing](docs/development.md).

The save tests use a small fake remote and do not prove Steam or Android transport. Desktop launcher interaction does not prove Android rendering or input. Report device and live-service results separately and only for the exact build tested.

Keep one existing authority per decision; remove superseded callers and code together. Revalidation at a later process or mutation boundary remains intentional. Preserve unique behavior coverage when consolidating tests. Record unproven deletion candidates and their retention reason in the existing reduction inventory.
