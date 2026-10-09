# Build Log

- **09-10-2026 T1** `42a9cca7`: Metadata keys, readers, writer, AIModelPricing. 1491/1491 unit tests
  (reviewer rerun). One fix round: ReadDecimal allowed thousands separators ("3,5" read as 35).
  Smoke: no entry point yet (readers first used by T4/T5); covered by the T7 wire check.
- **09-10-2026 T2** `d3b43999`: Public contract, ordered collection, builder extension, options.
  1492/1492 (reviewer rerun). Passed first review. Smoke: collection resolves from a real
  UmbracoBuilder in the spec; full DI path covered by T7.
