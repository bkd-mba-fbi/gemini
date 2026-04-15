# UserDomain

## Overview
UserDomain is a Gemini app that runs before issue creation and update to assign a creator domain to a custom field and add same-domain users as watchers.

It is implemented as an issue listener (`AbstractIssueListener`) and is designed to work with a Gemini custom field that stores the issue creator's email domain.

## What it does
- On issue create or update, it checks a configured custom field for the creator domain.
- If the domain field is empty, it extracts the domain from the issue originator email or the creator user's email.
- It writes the domain value into the configured custom field.
- It adds all active Gemini users from the same domain as watchers on the issue.
- It skips users whose domain is blacklisted in configuration.
- It records audit log entries when the domain or watcher list changes.

## Key files
- `UserDomain/BeforeUserDomain.cs` - main issue listener logic.
- `UserDomain/Helper.cs` - config loading, domain extraction, and audit logging helper methods.
- `UserDomain/App.config` - app settings for custom field name and blacklist.
- `UserDomain/UserDomain.csproj` - project build settings and Gemini dependencies.

## Configuration
The app reads runtime settings from `App.config` using `appSettings`.

Example `App.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="blacklist" value="erz.be.ch"/>
    <add key="customFieldNameDomain" value="DomainCreator"/>
  </appSettings>
</configuration>
```

### Supported settings
- `customFieldNameDomain` - the Gemini custom field name used to store the issue creator domain.
- `blacklist` - comma-separated list of domains that should not automatically receive watcher assignments.

## Behavior details
- `BeforeCreateFull` and `BeforeUpdateFull` both call the same logic.
- The application only updates the domain field if the field exists and is currently empty.
- Domain extraction uses a regular expression to parse the domain portion of an email address.
- If `OriginatorData` does not contain an email, the app falls back to the creator user's email.
- Watchers are added only for active users whose email domain matches the creator domain and who are not already watchers.
- When called during update, the app creates audit log entries for domain and watcher changes.

## Build and deployment
- Target framework: .NET Framework 4.5.2.
- Output type: `Library` (.dll).
- The project depends on Gemini SDK assemblies: `Countersoft.Gemini`, `Countersoft.Gemini.Commons`, `Countersoft.Gemini.Contracts`, `Countersoft.Gemini.Extensibility`.

### Deployment notes
- The project contains a post-build event that packages the app and copies it into a local Gemini app folder.
- Adjust the `PostBuildEvent` paths in `UserDomain.csproj` if your Gemini installation path differs.
- The app requires the configured custom field to exist on the target project.

## Known behavior and limitations
- If the configured custom field is missing or has no value, the app does nothing.
- Blacklist matching is exact, and domains must be separated by commas.
- The app only considers active users returned by `UserManager.GetActiveUsers()`.
- The app does not validate email format beyond regex matching for the domain part.

## Recommended setup
1. Create the custom field in Gemini (for example `DomainCreator`).
2. Update `App.config` with the custom field name and any blacklist domains.
3. Build and deploy the app to Gemini.
4. Test with a sample issue create/update to confirm the domain field is populated and watchers are added.
