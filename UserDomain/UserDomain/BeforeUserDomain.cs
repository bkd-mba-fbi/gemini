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
                return AddDomainAndSuperuser(args, false);
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
                return AddDomainAndSuperuser(args, true);
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return args.Issue;
            }
        }

        /// <summary>
        /// To run AddSuperUser() even if an error in AddDomain() occurs. Also CleanCode: clearer and easier to read.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private IssueDto AddDomainAndSuperuser(IssueDtoEventArgs args, bool createAudit)
        {
            try
            {
                IssueDto issue = AddDomain(args, createAudit);
                issue = AddSuperuser(args.Context, issue, args.User, createAudit);

                /*
                IssueAuditManager issueAuditManager = new IssueAuditManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                issueAuditManager.GenerateAudit(args.Issue, issue); // WAS MACHT DAS?
                // LogChange wo im Code ausgeführt ? Keine Implementierung ? WO MACHT ES DAS?
                //issueAuditManager.LogChange()
                */

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
        /// <returns></returns>
        private IssueDto AddDomain(IssueDtoEventArgs args, bool createAudit)
        {
            try
            {
                IssueDto issue = new IssueDto();
                issue = args.Issue;
                Helper helper = new Helper();
                CustomFieldDataDto erstellerOEField = new CustomFieldDataDto();
                erstellerOEField = issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain")));

                // Falls noch keine Domain im OE-Feld
                // FormattedData genommen, um manuelle Änderungen möglich zu lassen. Allenfalls könnte .Entity.Data genommen werden.
                if (string.IsNullOrEmpty(erstellerOEField.FormattedData))
                {
                    string maildomain = helper.FindDomain(issue.OriginatorData);

                    // Falls keine Emailadresse in OriginatorData vorhanden ist, Emailadresse vom Erstelleruser nehmen
                    if (string.IsNullOrEmpty(maildomain))
                    {
                        // Falls via einen anderen User erfasst
                        if (issue.Entity.ReportedBy > 0)
                        {
                            UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                            UserDto creatorUser = userManager.Get(issue.Entity.ReportedBy);
                            maildomain = helper.FindDomain(creatorUser.Entity.Email);

                        }
                        // Falls nicht
                        else
                        {
                            UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                            UserDto creatorUser = userManager.Get(args.User.Id);
                            maildomain = helper.FindDomain(creatorUser.Entity.Email);
                        }
                    }

                    string beforeValue = erstellerOEField.FormattedData;
                    // OriginatorData enthält Emailadresse, keine zusätzliche Aktion nötig
                    erstellerOEField.Entity.Data = maildomain;
                    // Komischerweise wird dadurch auch erstellerOEField und args.Issue überschrieben...
                    erstellerOEField.FormattedData = maildomain;
                    int customFieldsNumber = issue.CustomFields.IndexOf(issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain"))));
                    issue.CustomFields[customFieldsNumber] = erstellerOEField;

                    // Auditlog erstellen, falls etwas geändert wird
                    // Nur beim Hinzufügen der ErstellerOE
                    // Also wenn unterschiedlich
                    if (createAudit)
                    {
                        // beforeValue -> vorheriger Wert (args.Issue FormattedData -> vorheriger Wert)
                        // issue FormattedData -> neuer Wert (alternativ erstellerOEField.FormattedData)
                        if (!beforeValue.Equals(helper.GetFormattedDataErstellerOE(issue)))
                        {
                            IssueManager issueManager = new IssueManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                            helper.CreateAuditlog(args.Context, args.Issue.Entity.Id, args.Issue.Entity.ProjectId, erstellerOEField, beforeValue, erstellerOEField.FormattedData, args.User.Id, args.User.Fullname);
                        }
                    }

                }
                return issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return args.Issue;
            }
        }

        /// <summary>
        /// Add user with the same domain (superuser) as watcher
        /// </summary>
        /// <param name="issue"></param>
        /// <param name="user"></param>
        private IssueDto AddSuperuser(GeminiContext context, IssueDto issueParam, User user, bool createAudit)
        {
            try
            {
                IssueDto issue = issueParam;
                Helper helper = new Helper();
                string domain = issue.CustomFields.Find(field => field.Name.Equals(helper.GetAppConfigValue("customFieldNameDomain"))).Entity.Data;
                string domain2 = helper.GetFormattedDataErstellerOE(issue);

                // Falls noch etwas zu beachten ist
                if (helper.GetAppConfigValue("blacklist") != null)
                {
                    // Man muss beachten, dass wir ja bereits die Superuser von erz.be.ch sind
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
                return issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return issueParam;
            }
        }

        /// <summary>
        /// Adds a watcher if it has the same domain as the email address. Only if user is not watcher as on this task.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="domainValue"></param>
        /// <param name="issue"></param>
        public IssueDto AddWatcherFromDomain(GeminiContext context, string domain, IssueDto issueParam, int userId, string username, bool createAudit)
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
                return issue;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
                return issueParam;
            }
        }
    }
}