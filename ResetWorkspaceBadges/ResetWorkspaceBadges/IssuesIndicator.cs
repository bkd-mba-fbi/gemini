using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResetWorkspaceBadges
{
    [AppType(AppTypeEnum.Timer),
    AppGuid("01FDD186-C386-4B78-8D2F-0704510E9C98"),
    AppName("Reset Workspace Badges"),
    AppDescription("Reset the Badge Count in Workspace")]
    public class IssuesIndicator : TimerJob
    {
        public override bool Run(IssueManager issueManager)
        {
            GetWorkspaceItems(issueManager);
            return true;
        }

        public override TimerJobSchedule GetInterval(Countersoft.Gemini.Contracts.Business.IGlobalConfigurationWidgetStore dataStore)
        {
            var data = dataStore.Get<TimerJobSchedule>(AppGuid);

            if (data == null || data.Value == null)
            {
                return new TimerJobSchedule(5);
            }

            return data.Value;
        }

        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all Workspaces and it filters by "Url = items" and "BadgeCount > 0"
        /// If there are Items in the workspace, it updates the BadgeCount.
        /// If there are no Items in the workspace, it resets the BadgeCount.
        /// </summary>
        /// <param name="issueManager"></param>
        public void GetWorkspaceItems(IssueManager issueManager)
        {
            NavigationCardsManager navigationCardsManager = new NavigationCardsManager(issueManager);
            List<NavigationCard> workspaces = navigationCardsManager.GetAll();
            foreach (NavigationCard workspace in workspaces)
            {
                if (workspace.Url == "items" && workspace.BadgeCount > 0)
                {
                    List<IssueDto> workspaceItems = issueManager.GetItems(workspace);
                    
                    if (workspaceItems.Count() > 0)
                    {
                        UpdateBadgeCount(workspace, workspaceItems, navigationCardsManager);
                    }
                    else if (workspaceItems.Count() == 0)
                    {
                        ResetBadgeCount(workspace, navigationCardsManager);
                    }
                }
            }
        }

        /// <summary>
        /// This method updates the BadgeCount.
        /// In the first foreach, every items that should not be deleted, will be in the list "elementsNotToDelete".
        /// The second foreach, compares the elements in BadgeCount with the list "elementsNotToDelete" 
        /// and filters the items that should be deleted in the "elementsToDelete".
        /// In the last foreach, the items in the BadgeCount, that exist in "elementsToDelete", will be deleted.
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="workspaceItems"></param>
        /// <param name="navigationCardsManager"></param>
        public void UpdateBadgeCount(NavigationCard workspace, List<IssueDto> workspaceItems, NavigationCardsManager navigationCardsManager)
        {
            List<int> badges = workspace.CardData.Badges;
            List<int> elementsNotToDelete = new List<int>();
            List<int> elementsToDelete = new List<int>();

            foreach (IssueDto item in workspaceItems)
            {
                int isInList = badges.IndexOf(item.Id);
                if (isInList != -1)
                {
                    elementsNotToDelete.Add(item.Id);
                }
            }

            foreach (int badge in badges)
            {
                if (!elementsNotToDelete.Exists(element => element == badge))
                {
                    elementsToDelete.Add(badge);
                }
            }

            foreach (int badgetoDelete in elementsToDelete)
            {
                workspace.CardData.Badges.Remove(badgetoDelete);
                navigationCardsManager.Update(workspace);
                LogDebugMessage("Update Badge Count in Workspace: " + workspace.Id + " " + workspace.Title);
            }
        }

        /// <summary>
        /// Resets the BadgeCount from Workspace if "Badges > 0"
        /// </summary>
        /// <param name="workspace">Workspace properties</param>
        /// <param name="navigationCardsManager"></param>
        public void ResetBadgeCount(NavigationCard workspace, NavigationCardsManager navigationCardsManager)
        {
            workspace.CardData.Badges.RemoveAll(item => item > 0);
            navigationCardsManager.Update(workspace);
            LogDebugMessage("Reset Badge Count in Workspace: " + workspace.Id + " " + workspace.Title);
        }
    }
}