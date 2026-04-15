# ResetWorkspaceBadges

## Overview
ResetWorkspaceBadges is a Gemini timer-job app that keeps workspace badge counts in sync with the current items visible in the workspace filter.

The app checks each workspace with `Url == "items"` and a positive badge count, removes badges for missing items, and clears badges entirely when the workspace has no matching items.

## What it does
- Runs as a Gemini `TimerJob`.
- Retrieves all navigation cards (workspace entries) via `NavigationCardsManager`.
- Filters workspaces to those with `Url == "items"` and `BadgeCount > 0`.
- Converts workspace filters from the workspace user context to runtime context.
- Loads current filtered issue items for each workspace.
- Removes badge IDs that are no longer present in the workspace items.
- Clears all badges when the workspace item list is empty.

## Key files
- `ResetWorkspaceBadges/ResetWorkspaceBadges.cs` - main timer job logic and badge cleanup implementation.
- `ResetWorkspaceBadges/ResetWorkspaceBadges.csproj` - project build settings and Gemini SDK references.

## How it works
1. `Run()` logs start/end timestamps and calls `GetWorkspaceItems()`.
2. `GetWorkspaceItems()` enumerates all workspaces and checks each `NavigationCard`.
3. It replaces workspace filter system-user references via `ChangeSystemFilterTypesMe()` and loads filtered issues using `issueManager.GetFiltered()`.
4. If the workspace has zero items, it resets all badges.
5. If the workspace has items, it removes badge entries for any issue IDs not present in the current item list.
6. Changes are persisted using `NavigationCardsManager.Update()`.

## Implementation details
- `ChangeSystemFilterTypesMe()` adapts specific `IssuesFilter.SystemFilterTypes` values when the workspace filter needs to run for the workspace user.
- `UpdateBadgeCount()` performs badge cleanup in-place and only updates the workspace when badges have changed.
- To avoid overwriting concurrent workspace updates, the app reloads the workspace before saving changes.

## Build and deployment
- Target framework: .NET Framework 4.5.2.
- Output type: `Library` (.dll).
- Dependencies: Gemini SDK assemblies referenced in `ResetWorkspaceBadges.csproj`.

### Deployment notes
- The project includes a `PostBuildEvent` that packages the app and copies it to a local Gemini app folder.
- Adjust the `PostBuildEvent` path in `ResetWorkspaceBadges.csproj` to match your Gemini installation.

## Known behavior and limitations
- Only workspaces with `Url == "items"` are processed.
- The app only touches workspaces where `BadgeCount` was already greater than zero.
- `Shutdown()` is not implemented.
- Badge cleanup is based on the current item IDs returned by `issueManager.GetFiltered()`.
- If a workspace filter is user-specific, the project rewrites filter parameters for runtime evaluation and then restores them before saving.

## Recommended setup
1. Deploy the app as a Gemini timer job.
2. Verify the app can access the workspace navigation cards and issue filters in your environment.
3. Monitor workspace badge behavior after deployment to confirm stale badges are removed correctly.
