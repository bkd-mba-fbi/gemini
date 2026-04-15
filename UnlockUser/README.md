# UnlockUser

## Overview
UnlockUser is a Gemini timer job app that detects locked Gemini accounts, sends a notification email, and automatically unlocks the user after a configured delay.

The app is implemented as a Gemini `TimerJob` and checks active users on each run.

## What it does
- Scans active Gemini users for `Locked == true`.
- Reads unlock timing and localized email text from `App.config`.
- Sends a notification email to the locked user in their configured language.
- Unlocks the user automatically once the configured unlock time has passed.

## Key files
- `UnlockUser/UnlockUserClass.cs` - main timer job and business logic.
- `UnlockUser/App.config` - email body, subject, and unlock delay settings.
- `UnlockUser/UnlockUser.csproj` - project build settings and Gemini SDK references.
- `UnlockUser/UnitTestUnlockUser` - optional unit test project for the app.

## Configuration
The app uses `App.config` appSettings keys.

Example settings:

```xml
<appSettings>
  <add key="mailbody_de-DE" value="..."/>
  <add key="mailbody_fr-FR" value="..."/>
  <add key="mailSubject_de-DE" value="Ihr Gemini-Konto wurde gesperrt!"/>
  <add key="mailSubject_fr-FR" value="Votre compte Gemini a été suspendu!"/>
  <add key="unlockTime" value="15"/>
</appSettings>
```

### Supported keys
- `mailbody_<language>` - localized HTML body for the notification email. The body can use `{0}` for unlock time in minutes and `{1}` for the unlock clock time.
- `mailSubject_<language>` - localized subject line for the email.
- `unlockTime` - number of minutes after which the user will be automatically unlocked.

## How it works
1. `Run()` retrieves all active users from Gemini.
2. For each locked user, it calls `SendMail()` and `UnlockUser()`.
3. `SendMail()` checks whether the lock timestamp is newer than the last run interval and sends email only once per lock event.
4. If localized email settings are missing, it notifies global admins of the configuration error.
5. `UnlockUser()` calculates the scheduled unlock time and unlocks the account when the next timer run is after that time.

## Build and deployment
- Target framework: .NET Framework 4.5.2.
- Output type: `Library` (.dll).
- Dependencies:
  - `Countersoft.Gemini` assemblies
  - `System.configuration`
  - `System.Net.Http` and other standard .NET libraries as referenced in the project.

### Deployment notes
- The project includes a `PostBuildEvent` that packages the app and copies it to a local Gemini app folder under `C:\inetpub\wwwroot\GeminiPNA\App_Data\apps`.
- Adjust the `PostBuildEvent` paths in `UnlockUser.csproj` to match your local Gemini installation.
- The app must be deployed as a Gemini timer job.

## Known behavior and limitations
- The app uses `entity.Revised.ToLocalTime()` as the locked timestamp.
- It only sends email if the lock timestamp is newer than the previous run interval.
- If the localized email keys are not found, global admins receive a fallback error notification.
- Unlock occurs on the next timer run after the configured unlock time.
- `Shutdown()` is not implemented.

## Testing
- A test project exists at `UnlockUser/UnitTestUnlockUser`, with a project reference to the main `UnlockUser` app.
- You can add unit tests for `GetUnlockTime`, `SendMail`, and `UnlockUser` behavior in that project.
