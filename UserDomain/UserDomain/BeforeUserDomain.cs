using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Contracts;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Extensibility.Events;
using Countersoft.Gemini.Infrastructure.Managers;
using System;
using System.Collections.Generic;


namespace UserDomain
{
    [AppType(AppTypeEnum.Timer),
        AppGuid("D8C62E01-7F6A-474B-B891-695AE9872CC6"),
        AppName("User Domain"),
        AppDescription("Specify the task creator's domain in a custom field and add each user from the same domain as watcher except blacklisted domains. Configure the domain blacklist and the custom field in the App.config file.")]

    public class BeforeUserDomain : AbstractIssueListener
    {
        public override IssueDto BeforeCreateFull(IssueDtoEventArgs args)
        {
            try
            {
                IssueDto issue = AddDomain(args, false);
                AddSuperuser(args.Context, issue, args.User, false);
                return issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return args.Issue;
            }
        }

        public override IssueDto BeforeUpdateFull(IssueDtoEventArgs args)
        {
            try
            {
                IssueDto issue = AddDomain(args, true);
                AddSuperuser(args.Context, issue, args.User, true);
                return issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return args.Issue;
            }
        }

        /// <summary>
        /// Add the domain to the custom field, and create an auditlog if it hasn't just been created.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="createAudit"></param>
        /// <returns></returns>
        private IssueDto AddDomain(IssueDtoEventArgs args, bool createAudit)
        {
            try
            {
                IssueDto issue = args.Issue;
                Helper helper = new Helper();
                if ((issue.CustomFields.Count > 0) && (!issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain"))).ToString().Equals(null)))
                {
                    CustomFieldDataDto erstellerOEField = issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain")));

                    // If there is no domain in the OE-Field yet
                    // Depending on wether you want users to manually change the OE-Field or not, .FormattedData or .Entity.Data could be chosen.
                    if (string.IsNullOrEmpty(erstellerOEField.Entity.Data))
                    {
                        string maildomain = helper.FindDomain(issue.OriginatorData);

                        // If no email address in OriginatorData present, take email address from creator user
                        if (string.IsNullOrEmpty(maildomain))
                        {
                            UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                            int userId;

                            // If created via another user
                            if (issue.Entity.ReportedBy > 0)
                            {
                                userId = issue.Entity.ReportedBy;
                            }
                            // If not
                            else
                            {
                                userId = args.User.Id;
                            }
                            UserDto creatorUser = userManager.Get(userId);
                            maildomain = helper.FindDomain(creatorUser.Entity.Email);
                        }
                        // OriginatorData has email address, no more actions required

                        string beforeValue = erstellerOEField.FormattedData;
                        erstellerOEField.Entity.Data = maildomain;
                        erstellerOEField.FormattedData = maildomain;
                        // Keep in mind that args.Issue / issue / erstellerOEField are reference types, not value types

                        // Create auditlog if being called from BeforeUpdateFull with the auditlog flag
                        if (createAudit)
                        {
                            // beforeValue -> previous value (args.Issue FormattedData -> previous value)
                            // issue FormattedData -> new value (alternatively erstellerOEField.FormattedData)
                            if (!beforeValue.Equals(helper.GetFormattedDataErstellerOE(issue)))
                            {
                                IssueManager issueManager = new IssueManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                                helper.CreateAuditlog(args.Context, args.Issue.Entity.Id, args.Issue.Entity.ProjectId, erstellerOEField, beforeValue, erstellerOEField.FormattedData, args.User.Id, args.User.Fullname);
                            }
                        }

                    }
                    return issue;
                }
                return args.Issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return args.Issue;
            }
        }

        /// <summary>
        /// Add user with the same domain (superuser) as watcher.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issueParam"></param>
        /// <param name="user"></param>
        /// <param name="createAudit"></param>
        private void AddSuperuser(GeminiContext context, IssueDto issueParam, User user, bool createAudit)
        {
            try
            {
                IssueDto issue = issueParam;
                Helper helper = new Helper();
                if ((issue.CustomFields.Count > 0) && (!issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain"))).ToString().Equals(null)))
                {
                    // Alternatively .Entity.Data could be chosen.
                    string domain = helper.GetFormattedDataErstellerOE(issue);

                    // If there is something to consider
                    if (helper.GetAppConfigValue("blacklist") != null)
                    {
                        // It has to be considered that we already are the superusers of erz.be.ch (and potentially other domains)
                        string forbiddenDomains = helper.GetAppConfigValue("blacklist");
                        string[] domains = forbiddenDomains.Split(',');

                        if (!Array.Exists(domains, element => element == domain))
                        {
                            AddWatcherFromDomain(context, domain, issue, user.Id, user.Fullname, createAudit);
                        }
                    }
                    else
                    {
                        AddWatcherFromDomain(context, domain, issue, user.Id, user.Fullname, createAudit);
                    }
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
        }

        /// <summary>
        /// Adds a watcher if it has the same domain as the email address. Only if user is not watcher as on this task.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="domain"></param>
        /// <param name="issueParam"></param>
        /// <param name="userId"></param>
        /// <param name="username"></param>
        /// <param name="createAudit"></param>
        private void AddWatcherFromDomain(GeminiContext context, string domain, IssueDto issueParam, int userId, string username, bool createAudit)
        {
            try
            {
                IssueDto issue = issueParam;
                Helper helper = new Helper();
                UserManager usermanager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
                List<UserDto> users = usermanager.GetActiveUsers();

                foreach (UserDto user in users)
                {
                    // Only if not already watcher on this task
                    if (!issue.Entity.Watchers.Contains(user.Entity.Id.ToString()))
                    {
                        string activeUserDomain = helper.FindDomain(user.Entity.Email);
                        if (domain == activeUserDomain)
                        {
                            issue.Entity.AddWatcher(user.Entity.Id);

                            if (createAudit)
                            {
                                string watcher = user.Entity.Fullname;
                                helper.CreateAuditlog(context, issue.Id, issue.Project.Id, null, "", watcher, userId, username);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
        }
    }
}