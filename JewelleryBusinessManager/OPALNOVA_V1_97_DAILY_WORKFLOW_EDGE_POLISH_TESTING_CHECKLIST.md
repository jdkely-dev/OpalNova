# OPALNOVA V1.97.0 Daily Workflow Edge Polish Testing Checklist

Use this checklist against the published V1.97 build.

- Launch OPALNOVA and confirm the header shows `Version 1.97.0 - daily workflow edge polish`.
- Open About and confirm it shows `Version 1.97.0 - Daily Workflow Edge Polish`.
- Open Production Board and select a job card.
- Click `Open Payments` and confirm Payment & Collection opens in a workspace tab focused on the selected job.
- Confirm the focused payment tab title includes the selected job code when available.
- Select a production job that is not visible under the default payment filter and confirm Payment & Collection switches to `All jobs` so the job is still selected.
- Open Payment & Collection from the dashboard/menu and confirm the normal all-workflow entry point still opens.
- Confirm Payment & Collection rows use plain ASCII separators in status and money text.
- Confirm opening the focused payment workflow does not create a payment, sale, task, stock movement or job status change.
- Confirm Debug build, Release publish and published launch smoke have passed before treating this as release-ready.
