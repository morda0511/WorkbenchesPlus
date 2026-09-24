# Change protocol

Mandatory for every future code task. Same as `AGENTS.md`.

```text
REQUEST
→ UNDERSTAND
→ SEARCH EXISTING IMPLEMENTATION
→ CHECK VANILLA IMPLEMENTATION
→ TRACE DEPENDENCIES
→ CHECK GIT HISTORY
→ IDENTIFY ROOT CAUSE
→ IDENTIFY AFFECTED SYSTEMS
→ PLAN MINIMAL CHANGE
→ IMPLEMENT
→ BUILD (`dotnet build -c Release`; no zip unless asked)
→ TEST (in-game station UI)
→ REGRESSION CHECK
→ REVIEW DIFF
→ REPORT
```

## Report template

### Geändert / Changed

- File
- Method
- Concrete change

### Ursache / Cause

- Root cause

### Betroffene Systeme / Affected systems

- …

### Tests

- Build
- Function test

### Regression

- Existing behaviours still checked

### Zusätzlich gefunden / Additionally found

- Issues **not** changed
