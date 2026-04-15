# JiraSync

## Overview
JiraSync is a Gemini timer job application that synchronizes Jira issues into a Gemini project.

It reads one or more Jira service configurations from `AppConfig.json`, searches Jira via the Jira REST search API, and then creates or updates Gemini issues.

The app is built as a Gemini extension (`TimerJob`) and runs regularly according to its timer schedule.

## What it does
- Reads `AppConfig.json` from the same directory as the executing assembly.
- Connects to Jira using username + personal access token (Basic auth).
- Executes a Jira JQL search against `jiraUrlSearch`.
- Maps Jira fields into Gemini fields using configured mappings.
- Ensures Gemini versions exist for Jira fixVersions and updates their released state.
- Finds existing Gemini issues by a configured custom field value (e.g. `FremdId`).
- Updates existing Gemini issues if mapped fields change.
- Creates new Gemini issues when a Jira issue has no matching Gemini issue.
- Appends a Jira browse link into the Gemini issue description.

## Key files
- `JiraSync/JiraSync.cs` - main timer job implementation and synchronization logic.
- `JiraSync/AppConfig.cs` - configuration model used by the app.
- `JiraSync/JiraResponse.cs` - Jira API response model definitions.
- `JiraSync/Appconfig.json` - example runtime configuration.
- `JiraSync/JiraSync.csproj` - project build settings and Gemini dependencies.

## Configuration
The app uses `AppConfig.json` with this structure:

```json
{
  "jiraServices": [
    {
      "jiraUrlSearch": "https://your-jira-instance/rest/api/3/search/jql",
      "jiraUsername": "user@example.com", //Jira Username
      "personalAccessToken": "YOUR_TOKEN", // Jira Access Token
      "searchPostBody": {
        "jql": "project = ABC", //jql search query string for Jira issues
        "maxResults": 200, //How many issues will be returnd
        "fields": ["issuetype", "components", "description", "fixVersions", "resolution", "summary", "priority", "status"] // Issue columns in jira Response
      },
      "targetGeminProject": "PROJECT_CODE", 
      "geminiCustomFieldJiraKey": "FremdId", 
      "defaultComponent": "DefaultComponentName", 
      "finalTargetStatus": "Closed", 
      "mapping": [ 
        { "property": "issuetype", "source": "Bug", "target": "Bug" },
        { "property": "priority", "source": "Blocker", "target": "High" },
        { "property": "status", "source": "In Progress", "target": "In Work" },
        { "property": "components", "source": "Web", "target": "Web" }
      ]
    }
  ]
}
```

### Important configuration details
- `jiraUrlSearch` should target Jira's search API endpoint.
- `searchPostBody.jql` must be lowercase `jql`.
- `geminiCustomFieldJiraKey` must match a Gemini custom field name that exists on the target project, and that field is used to store the Jira issue key.
- `targetGeminProject` must be a Gemini project code.
- `defaultComponent` must be a valid Gemini component name for the target project.
- `finalTargetStatus` is a Gemini status name. If an issue already has this status, JiraSync will not change its status again.
- `mapping` entries are used to convert Jira values into Gemini IDs for the following properties:
  - `issuetype`
  - `priority`
  - `resolution`
  - `status`
  - `components`

## How synchronization works
1. The timer job starts and reads `AppConfig.json`.
2. For each configured Jira service, it loads existing Gemini issues for the target project.
3. It queries Jira for issues with the configured JQL.
4. It checks Jira fixVersions and creates or updates matching Gemini versions.
5. It tries to find an existing Gemini issue using the configured custom field value.
6. If a Gemini issue exists, it updates type, priority, status, resolution, version, components, and description.
7. If no Gemini issue exists and an issue type mapping exists, it creates a new Gemini issue.

## Build and deployment
- Target framework: .NET Framework 4.8.
- Dependencies:
  - `Countersoft.Gemini` assemblies (Gemini SDK)
  - `Newtonsoft.Json`
- Output type: `Library` (.dll).

### Notes on project deployment
- The project currently contains a post-build event that packages the app and copies it to `...\Gemini\App_Data\apps`.
- Adjust the post-build copy path in `JiraSync.csproj` to match your local Gemini installation.
- The app should be deployed as a Gemini app and registered as a timer job.

## Runtime notes
- `AppConfig.json` is loaded by path from the assembly location.
- The actual config file name used by code is `AppConfig.json`; on Windows this is case-insensitive.
- The `Shutdown()` method is not implemented in the current code.
- `GetGeminiIssues()` loads up to 1000 issues at a time and filters issues with custom fields.

## Known behavior
- If a Jira issue has multiple fixVersions, only the first fixVersion is used for `FixedInVersionId`.
- Component values are appended to existing Gemini issue component strings, avoiding duplicates.
- If a mapping entry is missing, the app falls back to the current Gemini issue value for that property.
- The Jira issue browser link is inserted into the Gemini issue description.

## Recommended next steps
- Verify the Gemini custom field used for Jira keys exists in the target project.
- Confirm all target Gemini values used in `mapping` exist in the project's template.
- Update the post-build deployment paths if you want to deploy automatically after build.
- Add any additional Jira fields to `searchPostBody.fields` if you need them in the future.
