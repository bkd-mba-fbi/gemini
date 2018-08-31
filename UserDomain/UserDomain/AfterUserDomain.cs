using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Commons.Permissions;
using Countersoft.Gemini.Contracts;
using Countersoft.Gemini.Extensibility.Events;
using Countersoft.Gemini.Infrastructure.Managers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UserDomain
{
    public class AfterUserDomain : IIssueAfterListener
    {
        /// <summary>
        /// This method filters an email address with a regex pattern to get domain
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string FindDomain(string email)
        {
            string domain = null;
            try
            {
                string pattern = "(?<=@)(.*)";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(email);
                domain = match.Value;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
            return domain;
        }

        /// <summary>
        /// Get value from different AppConfig settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public string GetAppConfigValue(string settings)
        {
            AppSettingsSection appSettings = null;
            try
            {
                ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string appConfigFileName = Path.Combine(assemblyFolder, "App.config");
                configFile.ExeConfigFilename = appConfigFileName;
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);
                appSettings = (AppSettingsSection)config.GetSection("appSettings");
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
            return appSettings.Settings[settings].Value;
        }

        /// <summary>
        /// This creates an auditlog for the watcher and custom field manipulations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issueId"></param>
        /// <param name="issueProjectId"></param>
        /// <param name="customField"></param>
        /// <param name="beforeValue"></param>
        /// <param name="afterValue"></param>
        public void CreateAuditlog(GeminiContext context, int issueId, int issueProjectId, CustomFieldDataDto customField, string beforeValue, string afterValue, int userId, string username)
        {
            try
            {
                IssueAuditManager issueAuditManager = new IssueAuditManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
                UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
                IssueAudit audit = issueAuditManager.GetIssueAuditObject(issueId, issueProjectId);
                audit.UserId = userId;
                audit.Fullname = username;

                if (customField == null)
                {
                    issueAuditManager.LogChange(audit, ItemAttributeVisibility.AssociatedWatchers,
                        string.Empty, string.Empty, beforeValue, afterValue);
                }
                else
                {
                    issueAuditManager.LogChange(audit, ItemAttributeVisibility.AssociatedCustomFields, customField.Entity.CustomFieldId.ToString(),
                        string.Empty, string.Empty, beforeValue, afterValue);
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }

        }

        /// <summary>
        /// This method adds a watcher if it has the same domain as from email-address. Only if user is not watcher as on this task.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="domainValue"></param>
        /// <param name="issue"></param>
        public void AddWatcherFromDomain(GeminiContext context, string domainValue, Issue issue, int userId, string username)
        {
            try
            {
                UserManager usermanager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
                List<UserDto> users = usermanager.GetActiveUsers();

                foreach (UserDto user in users)
                {
                    if (!issue.Watchers.Contains(user.Entity.Id.ToString()))
                    {
                        string activeUserDomain = FindDomain(user.Entity.Email);
                        if (domainValue == activeUserDomain)
                        {
                            issue.AddWatcher(user.Entity.Id);
                            string watcher = user.Entity.Fullname;
                            CreateAuditlog(context, issue.Id, issue.ProjectId, null, "", watcher, userId, username);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
        }

        /// <summary>
        /// In the first step it checks if the Ersteller OE field is empty.
        /// After that it adds the domain and creates an auditlog. The next section is to get the App.config value.
        /// The last step is to check if the domain is in the blacklist.
        /// </summary>
        /// <param name="args"></param>
        public void RunLogic(IssueDtoEventArgs args)
        {
            try
            {
                CustomFieldDataDto erstellerOEField = args.Issue.CustomFields.Find(field => field.Name.Equals(GetAppConfigValue("customFieldNameDomain")));

                if (string.IsNullOrEmpty(erstellerOEField.Entity.Data) || string.IsNullOrEmpty(erstellerOEField.FormattedData))
                {
                    string maildomain = FindDomain(args.Issue.OriginatorData);

                    if (string.IsNullOrEmpty(maildomain))
                    {
                        UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                        UserDto creatorUser = userManager.Get(args.Issue.Entity.Creator);
                        maildomain = FindDomain(creatorUser.Entity.Email);
                    }
                    else
                    {
                        string beforeDomainValue = erstellerOEField.Entity.Data;
                        string domainValue = erstellerOEField.Entity.Data = maildomain;

                        IssueManager issueManager = new IssueManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                        CreateAuditlog(args.Context, args.Issue.Entity.Id, args.Issue.Entity.ProjectId, erstellerOEField, beforeDomainValue, domainValue, args.User.Id, args.User.Fullname);
                        issueManager.Update(args.Issue);

                        if (GetAppConfigValue("blacklist") != null)
                        {
                            string forbiddenDomains = GetAppConfigValue("blacklist");
                            string[] domains = forbiddenDomains.Split(',');

                            if (!Array.Exists(domains, element => element == domainValue))
                            {
                                AddWatcherFromDomain(args.Context, domainValue, args.Issue.Entity, args.User.Id, args.User.Fullname);
                                issueManager.Update(args.Issue);
                            }
                        }
                        else
                        {
                            AddWatcherFromDomain(args.Context, domainValue, args.Issue.Entity, args.User.Id, args.User.Fullname);
                            issueManager.Update(args.Issue);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
        }

        /// <summary>
        /// The app procedures is in the BeforeCreateFull-Listener. 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public void AfterCreateFull(IssueDtoEventArgs args)
        {
            RunLogic(args);
        }

        public void AfterAssign(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterClose(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterComment(IssueCommentEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterCreate(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterDelete(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterIssueCopy(IssueDtoEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterResolutionChange(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterStatusChange(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterUpdate(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void AfterUpdateFull(IssueDtoEventArgs args)
        {
            RunLogic(args);
        }

        public void AfterWatcherAdd(IssueEventArgs args)
        {
            throw new NotImplementedException();
        }

        public string AppGuid
        {
            get
            {
                return "D8C62E01-7F6A-474B-B891-695AE9872CC6";
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get
            {
                return "Specify the task creator's domain in a custom field and add each user from the same domain as watcher expects blacklist domains. Configure the blacklist domain and the custom field in the App.config file.";
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                return "User Domain";
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}