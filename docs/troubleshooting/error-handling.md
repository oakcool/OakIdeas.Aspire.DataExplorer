# Troubleshooting Common Data Explorer Errors

## Connection failed

If Data Explorer reports that the selected database is unavailable or the connection was rejected:

- Confirm the Aspire-hosted database is running.
- Refresh Object Explorer metadata.
- Re-select the database resource if the resource was recreated.

## Query timeout

If metadata loading times out:

- Wait for the database workload to settle.
- Retry the operation.
- Refresh metadata instead of repeatedly reloading the page.

## Permission denied

If metadata access is denied:

- Use a development database identity with schema and metadata access.
- Try a different development database if the current one is intentionally locked down.

## Resource not found

If a selected resource or object cannot be found:

- Refresh discovered resources.
- Re-select the target database or object from Object Explorer.

## Provider error

If the provider reports an unsupported or unexpected operation:

- Retry once after refreshing metadata.
- Review the diagnostic code shown in the UI details before debugging provider behavior.
