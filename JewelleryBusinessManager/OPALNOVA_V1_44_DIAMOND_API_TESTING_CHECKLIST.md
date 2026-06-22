# OPALNOVA V1.44.1 Diamond API Testing Checklist

1. Open **Tools → Diamond Supplier Studio**.
2. Open **Nivoda Diamond Search**.
3. Enter user-supplied Nivoda username and password.
4. Press **Save Settings**.
5. Press **Test Connection**.
6. If test succeeds, run a broad search such as ROUND, 1.0–1.5ct, IGI/GIA, lab-grown.
7. Confirm results appear or record the GraphQL error shown.
8. Select a result and press **Save Selected External Diamond**.
9. Press **Open Saved External Diamond Records**.
10. Confirm the record appears under **External Diamonds**.
11. Close and reopen OPALNOVA to confirm settings and saved records persist.
12. Publish standalone and confirm OPALNOVA.exe still opens.

If the API returns a schema error, open the GraphiQL explorer and compare the field names for diamond search filters and result fields.


## Current credential test

1. Open Diamond Supplier Studio.
2. Enter user-supplied Nivoda credentials or use already-saved credentials.
3. Click **Test Connection**.
4. If successful, run a broad search: ROUND, 1.0 to 1.5 ct, IGI/GIA, lab-grown.
5. Save one external diamond result.
6. Open saved external diamond records and confirm it appears.
7. Open GraphiQL in the browser if the schema returns a field/filter error.
