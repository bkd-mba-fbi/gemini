using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResetWorkspaceBadges
{
    [AppType(AppTypeEnum.Timer),
    AppGuid("01FDD186-C386-4B78-8D2F-0704510E9C98"),
    AppName("Reset or decrement Workspace Badges"),
    AppDescription("Reset or decrement badge count if the issue not in the workspace filter.")]
    public class ResetWorkspaceBadges : TimerJob
    {
        public override bool Run(IssueManager issueManager)
        {
            LogDebugMessage(string.Concat("START: ",DateTime.Now.ToString()));
            GetWorkspaceItems(issueManager);
            LogDebugMessage(string.Concat("END: ", DateTime.Now.ToString()));
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
            try
            {
                NavigationCardsManager navigationCardsManager = new NavigationCardsManager(issueManager);
                List<NavigationCard> workspaces = navigationCardsManager.GetAll();
                foreach (NavigationCard workspace in workspaces.ToList())
                {

                    if (workspace.Url == "items" && workspace.BadgeCount > 0)
                    {

                        IssuesFilter filter = ChangeSystemFilterTypesMe(workspace.Filter, (int)workspace.UserId, false);
                        List<IssueDto> workspaceItems = issueManager.GetFiltered(filter, true);

                        if (workspaceItems.Count() > 0)
                        {
                            UpdateBadgeCount(workspace, workspaceItems, navigationCardsManager, false);
                        }
                        else
                        {
                            UpdateBadgeCount(workspace, workspaceItems, navigationCardsManager, true);
                        }
                        
                    }
                    
                }

            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, string.Concat("Run Method GetWorkspaceItems: ",exception.Message));
            }
                           
        }


        /// <summary>
        /// This Method change SystemFilterType" > "ME". When filters in Worksapce to be executed with Systemuser(-2). 
        /// You need to change SystemFilterTypes to Workspace user. Returnvalue "IssuesFilter".
        /// param returnOriginal set back to Original
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="userId"></param>
        /// <param name="returnOriginal"></param>
        /// <returns></returns>
        public IssuesFilter ChangeSystemFilterTypesMe(IssuesFilter filter, int userId, bool returnOriginal)
        {

            if (!returnOriginal)
            {

                switch (filter.SystemFilter)
                {
                    case IssuesFilter.SystemFilterTypes.AssignedToMeIssues:
                        filter.Resources = userId.ToString();
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext14DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext30DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext7DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext7DaysIssuesAfterTomorrow:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueTodayIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueTomorrowIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueYesterdayIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.FollowedByMeIssues:
                        List<int> users = filter.Watchers;
                        users = new List<int> { userId };
                        filter.Watchers = users;
                        break;
                    case IssuesFilter.SystemFilterTypes.LateIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.NoType:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyClosedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyCreatedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyUpdatedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.SubmittedByMeIssues:
                        filter.ReportedByUserId = userId;
                        break;
                    case IssuesFilter.SystemFilterTypes.TimeLoggedByMeIssues:
                        filter.TimeLoggedBy = userId.ToString();
                        break;
                    case IssuesFilter.SystemFilterTypes.ToSynch:
                        break;
                    default:
                        break;
                }

 
            }
            else
            {
                switch (filter.SystemFilter)
                {
                    case IssuesFilter.SystemFilterTypes.AssignedToMeIssues:
                        filter.Resources.Replace(userId.ToString(),"");
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext14DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext30DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext7DaysIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueNext7DaysIssuesAfterTomorrow:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueTodayIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueTomorrowIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.DueYesterdayIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.FollowedByMeIssues:
                        filter.Watchers.Remove(userId);
                        break;
                    case IssuesFilter.SystemFilterTypes.LateIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.NoType:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyClosedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyCreatedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.RecentlyUpdatedIssues:
                        break;
                    case IssuesFilter.SystemFilterTypes.SubmittedByMeIssues:
                        filter.ReportedByUserId = 0;
                        break;
                    case IssuesFilter.SystemFilterTypes.TimeLoggedByMeIssues:
                        filter.TimeLoggedBy.Replace(userId.ToString(),"");
                        break;
                    case IssuesFilter.SystemFilterTypes.ToSynch:
                        break;
                    default:
                        break;
                }


            }
       

            return filter;
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
        /// <param name="reset"></param>
        public void UpdateBadgeCount(NavigationCard workspace, List<IssueDto> workspaceItems, NavigationCardsManager navigationCardsManager, bool reset)
        {
            try
            {
                bool change = false;
                
                workspace = navigationCardsManager.Get(workspace.Id);
                if (reset)
                {
                    change = true;
                    workspace.CardData.Badges.RemoveAll(item => item > 0);
                    LogDebugMessage("Reset Badge Count in Workspace: " + workspace.Id + " " + workspace.Title);
                }
                else
                {

                    foreach (int badge in workspace.CardData.Badges.ToList())
                    {
                        if (!workspaceItems.Exists(item => item.Entity.Id == badge))
                        {
                            change = true;
                            workspace.CardData.Badges.RemoveAll(id => id == badge);
                            LogDebugMessage(string.Concat("Update Badge Count in Workspace: ", workspace.Key," (" ,workspace.Id , ") " , workspace.Title, " >> Issue: ",badge));
                            
                        }
                    }
                    
                }

                
                if (change)
                {
                    //it's possible workspace change at time of runing timerapp get new data and don't overwrite user changes.
                    List<int> newBadges = workspace.CardData.Badges;
                    workspace.Filter = ChangeSystemFilterTypesMe(workspace.Filter, (int)workspace.UserId, true);
                    workspace = navigationCardsManager.Get(workspace.Id);
                    workspace.CardData.Badges = newBadges;
                    navigationCardsManager.Update(workspace, true, false);
                }
                

            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, string.Concat("Run Method UpdateBadgeCount: ",exception.Message));
            }
   
        }

    }
}