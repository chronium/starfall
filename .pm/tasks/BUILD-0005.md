---
id: BUILD-0005
title: Partition local and family project-reference architecture checks
track: BUILD
milestone: M0
dependsOn:
- BUILD-0002
createdAt: 2026-08-02T11:31:50.9499840Z
modifiedAt: 2026-08-02T11:33:30.2016010Z
---

Fix the Starfall architecture suite so the approved client-only coordinator source references can be added by CLIENT-0006 without contradicting the exact Starfall-local dependency graph.

Acceptance criteria:
- Product_project_references_match_approved_graph compares only Starfall-local project references with ExpectedProductReferences.
- Only references accepted by IsApprovedFamilySourceReference are excluded from the local product graph; unknown or unapproved references remain fail-closed.
- Coordinator-family references continue to be validated separately through the exact ApprovedClientFamilySourceReferences allowlist and repository/family policy test.
- A regression fixture covers Starfall.Client with Content, Protocol, and all three approved family references.
- Unapproved property roots, coordinator projects, Royale references, and family references from non-client projects continue to fail.
- Starfall restores, builds, formats, and passes its full test suite.
- No production project reference, coordinator source, generated content, runtime behavior, wiki contract, or coordinator gitlink is changed.

## Notes

- 2026-08-02 11:33 UTC - Implemented the reviewed architecture-test correction.

  - Product_project_references_match_approved_graph now excludes only references accepted by IsApprovedFamilySourceReference before comparing the exact Starfall-local graph.
  - Added an in-memory Starfall.Client regression fixture containing Content, Protocol, and all three approved ChronoFall family references.
  - The fixture also proves an unapproved coordinator project remains in the local-graph input and therefore fails closed rather than being silently filtered.
  - Existing repository/family policy tests continue to reject arbitrary property roots, direct SDL3-CS, Royale references, and family consumption outside Starfall.Client.
  - No production project reference, runtime source, generated content, wiki contract, coordinator source, or gitlink was changed.

  Validation:
  - dotnet restore Starfall.slnx -m:1 --disable-build-servers: passed; all projects up to date.
  - dotnet build Starfall.slnx -m:1 --no-restore --disable-build-servers: passed with 0 warnings and 0 errors.
  - dotnet test Starfall.slnx -m:1 --no-restore --no-build --disable-build-servers: passed 15/15.
  - dotnet format Starfall.slnx --verify-no-changes --no-restore: passed.
  - Starfall pm doctor and git diff --check passed.
  - Family inspection returned all three projects available, readable, and write-trusted with zero warnings.
  - The coordinator remains clean at its existing unpushed SHARED-0017 commit; Royale remains clean. No visual validation was required.