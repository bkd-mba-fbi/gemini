# SearchFremdID

## Overview
SearchFremdID is a Gemini issue listener app that automatically finds and populates a configured custom field with a foreign ID extracted from task description or comments.

The app uses a regular expression defined in `App.config` to search for an ID pattern and writes the first match into the configured custom field when the field is empty.

## What it does
- Runs before issue updates (`BeforeUpdateFull`).
- If the configured custom field is empty, it searches the issue description for a matching ID.
- If no ID is found in the description, it searches existing comments.
- When an ID is found, it writes it to the custom field and logs the change with an audit entry.
- Other issue listener methods are implemented as pass-through no-ops.

## Key files
- `SearchFremdID/FremdID.cs` - main issue listener implementation.
- `SearchFremdID/App.config` - regex pattern and custom field name configuration.
- `SearchFremdID/SearchFremdID.csproj` - build settings and Gemini references.

## Configuration
The app reads the following settings from `App.config`:

```xml
<appSettings>
  <add key="regex" value="(EVO-[0-9]{1,10}(?i)|Webshop [0-9]{1,10}(?i)|A4O-\d\d\d\d-\d\d-\d\d-[\w-]*)"/>
  <add key="customFieldName" value="FremdId"/>
</appSettings>
```

### Supported keys
- `regex` — regular expression used to find the foreign ID in description or comments.
- `customFieldName` — name of the Gemini custom field to populate with the extracted ID.

## How it works
1. `BeforeUpdateFull` checks whether the configured custom field is currently empty.
2. If empty, it calls `FindID()` on the issue description.
3. If an ID is found, it stores the value in the custom field and creates an audit log.
4. If no ID is found in the description, it scans each issue comment.
5. When the first comment match is found, it updates the issue and logs the change.

## Build and deployment
- Target framework: .NET Framework 4.5.2.
- Output type: `Library` (.dll).
- Dependencies: Gemini SDK assemblies and `System.configuration`.

### Deployment notes
- The project includes a `PostBuildEvent` that packages the app and copies it to a local Gemini app folder.
- Update the `PostBuildEvent` paths in `SearchFremdID.csproj` to match your Gemini deployment environment.

## Known behavior and limitationsl
- The app only fills the custom field when it is empty.
- It uses the first regular expression match found in description or comments.
- It does not attempt to re-scan or overwrite a field that already contains a value.
- If the configured custom field is missing or the regex does not match, no value is written.
- The app does not validate whether the extracted value is already present elsewhere.

## Recommended setup
1. Add the configured custom field to your Gemini project.
2. Adjust the `regex` in `App.config` to match your foreign ID format.
3. Build and deploy the app.
4. Test on an update where description or comments contain the target ID.
