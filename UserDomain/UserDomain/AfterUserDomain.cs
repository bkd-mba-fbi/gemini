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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string appConfigFileName = Path.Combine(assemblyFolder, "App.config");
            configFile.ExeConfigFilename = appConfigFileName;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);
            AppSettingsSection appSettings =
                   (AppSettingsSection)config.GetSection("appSettings");
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

        /// <summary>
        /// This method adds a watcher if it has the same domain as from email-address
        /// </summary>
        /// <param name="context"></param>
        /// <param name="domainValue"></param>
        /// <param name="issue"></param>
        public void AddWatcherFromDomain(GeminiContext context, string domainValue, Issue issue, int userId, string username)
        {
            UserManager usermanager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
            List<UserDto> users = usermanager.GetActiveUsers();

            foreach (UserDto user in users)
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

        /// <summary>
        /// The app procedures is in the BeforeCreateFull-Listener. In the first step it checks if the Ersteller OE field is empty.
        /// After that it adds the domain and creates an auditlog. The next section is to get the App.config value.
        /// The last step is to check if the domain is in the blacklist.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public void AfterCreateFull(IssueDtoEventArgs args)
        {
            CustomFieldDataDto erstellerOEField = args.Issue.CustomFields.Find(field => field.Name.Equals(GetAppConfigValue("customFieldNameDomain")));
            if (erstellerOEField.Entity.Data == String.Empty)
            {
                string maildomain = FindDomain(args.Issue.OriginatorData);

                if (maildomain != "")
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
            throw new NotImplementedException();
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
                return "Searching after User Domain from Task and set User Domain";
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