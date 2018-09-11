using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Commons.Permissions;
using Countersoft.Gemini.Contracts;
using Countersoft.Gemini.Infrastructure.Managers;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;


namespace UserDomain
{
    public class Helper
    {
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
        /// This method filters an email address with a regex pattern to get its domain
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string FindDomain(string email)
        {
            string domain = null;
            try
            {
                // RFC 5322 Official Standard
                string pattern = "(?<=@)(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)])";
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
        /// This creates an auditlog (history) for the watcher and custom field manipulations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issueId"></param>
        /// <param name="issueProjectId"></param>
        /// <param name="customField"></param>
        /// <param name="beforeValue"></param>
        /// <param name="afterValue"></param>
        /// <param name="userId"></param>
        /// <param name="username"></param>
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

        public string GetFormattedDataErstellerOE(IssueDto issue)
        {
            string formattedData = null;
            try
            {
                formattedData = issue.CustomFields.Find(field => field.Name.Equals(GetAppConfigValue("customFieldNameDomain"))).FormattedData;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
            return formattedData;
        }
    }
}
