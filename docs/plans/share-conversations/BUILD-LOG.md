# Build Log

- **SC-01** — Domain model + repository interface (Core). Commit `eac90601`. Three review rounds:
  (1) FAIL — 3 Important findings, the key one a real gap in `ARCHITECTURE.md` itself (the
  recipient-uniqueness constraint didn't say whether it applied across revoked rows, which as
  written would have permanently blocked re-sharing with anyone ever revoked); resolved by filtering
  the uniqueness to active rows only and documenting it. (2) FAIL — one stale doc comment left after
  the first fix round. (3) PASS. `dotnet build` clean throughout; no runtime entry point exists yet
  for this foundation task (nothing calls these types until SC-02), so the build itself is the smoke
  gate here, per the reviewer's own conclusion.
