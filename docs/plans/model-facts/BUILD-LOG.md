# Build Log

- **09-10-2026 T1** `42a9cca7`: Metadata keys, readers, writer, AIModelPricing. 1491/1491 unit tests
  (reviewer rerun). One fix round: ReadDecimal allowed thousands separators ("3,5" read as 35).
  Smoke: no entry point yet (readers first used by T4/T5); covered by the T7 wire check.
