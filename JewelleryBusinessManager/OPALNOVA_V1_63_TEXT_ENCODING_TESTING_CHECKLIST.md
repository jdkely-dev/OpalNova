# OPALNOVA V1.63.0 Text Encoding Testing Checklist

Use this checklist after installing or launching the V1.63.0 build.

- [ ] Confirm the main header shows `Version 1.63.0 - text encoding and copy cleanup`.
- [ ] Open About and confirm it shows `Version 1.63.0 - Text Encoding and Copy Cleanup`.
- [ ] Open Settings / Admin / Data Safety and generate the User Guide.
- [ ] Confirm the guide title is `OPALNOVA User Guide` and the metadata says manual version V1.63.0.
- [ ] Generate Release Notes and confirm V1.63.0 appears at the top.
- [ ] Run Database Health Check and confirm the first line reads `OPALNOVA - Database Health Check`.
- [ ] Open Hardware & POS Studio, generate Device Setup Notes, and confirm the heading and menu paths use plain readable text.
- [ ] Generate a Production Batch Report and confirm selected-batch titles use `Batch Report - ...`.
- [ ] Generate Inventory Audit, Opal Parcel Yield, and Stone Workflow reports if sample records exist.
- [ ] Confirm those generated report headings do not show mojibake or replacement symbols.
- [ ] Open Traceability View on a record and confirm the text heading reads `OPALNOVA - TRACEABILITY VIEW`.
- [ ] Close the app normally.
