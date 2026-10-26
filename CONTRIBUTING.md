# Contributing

Keep each change focused on behavior the launcher currently supports.

## Pull requests

Include:

- What changed and why.
- Reproduction steps for a bug fix.
- The focused commands you ran and their results.
- Any device or real-Steam validation performed.
- Important limitations that remain untested.

Test the behavior you changed. A successful build, a patch count, or a source inspection does not show that a feature works. If a check cannot run, explain what prevented it. For documentation changes, check the instructions against the code and verify links and examples.

## Focused validation

Use the smallest relevant behavior checks. Operational commands, fixture arguments and APK requirements are maintained in [Building and testing](docs/development.md).

The save tests use a small fake remote and do not prove Steam or Android transport. Desktop launcher interaction does not prove Android rendering or input. Report device and live-service results separately and only for the exact build tested.

Use the existing component responsible for a behavior where it fits. When replacing an implementation, remove the old code and its unused callers, dependencies, tests and documentation together. Keep checks that serve a separate process or file-change boundary, and preserve distinct behavior coverage when consolidating tests. If a proposed deletion still needs evidence, record the missing evidence in the existing reduction inventory.
