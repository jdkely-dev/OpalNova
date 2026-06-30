# OPALNOVA V1.82.0 Inventory Media Batch Workflow Testing Checklist

Use this checklist after launching the V1.82.0 build.

- [ ] Confirm the main header shows `Version 1.82.0 - inventory media batch workflow`.
- [ ] Open About and confirm it shows `Version 1.82.0 - Inventory Media Batch Workflow`.
- [ ] Open Release Notes and confirm V1.82.0 appears above V1.81.0.
- [ ] Open a jewellery stock, stone, material, customer, job or other saved record in the main register/detail workflow.
- [ ] Confirm the detail action button reads `+ Photos`.
- [ ] Click `+ Photos` and select one supported image file.
- [ ] Confirm OPALNOVA copies the photo to app storage and creates a linked photo record.
- [ ] Select the same record again and confirm the detail preview can still show a linked photo.
- [ ] Click `+ Photos` again and select multiple supported image files.
- [ ] Confirm OPALNOVA reports the correct number of imported photos.
- [ ] Open the Photos section and confirm batch-imported records exist with captions linking them to the selected record type/id.
- [ ] Run Health Check and confirm the imported photo links are not reported missing.
- [ ] Confirm existing reports or record previews still open without photo-related errors.
- [ ] Close OPALNOVA cleanly.
