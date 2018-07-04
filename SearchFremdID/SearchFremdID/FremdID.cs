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

namespace SearchFremdID
{
    public class FremdID : IIssueBeforeListener
    {
        /// <summary>
        /// Search in "text" after ID with a Regex Pattern
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string FindID(string text)
        {
            string id = null;
            try
            {
                string pattern = GetAppConfigValue("regex");
                Regex regex = new Regex(pattern);
                Match match = regex.Match(text);
                id = match.Value;
            }
            catch (Exception e)
            {
                GeminiApp.LogException(e, false, e.Message);
            }
            return id;
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
        /// This creates an auditlog for the custom field manipulations
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issueId"></param>
        /// <param name="issueProjectId"></param>
        /// <param name="customField"></param>
        /// <param name="beforeValue"></param>
        /// <param name="afterValue"></param>
        public void CreateAuditlog(GeminiContext context, int issueId, int issueProjectId, CustomFieldDataDto customField, string beforeValue, string afterValue)
        {
            IssueAuditManager issueAuditManager = new IssueAuditManager(GeminiApp.Cache(), GeminiApp.UserContext(), context);
            IssueAudit audit = issueAuditManager.GetIssueAuditObject(issueId, issueProjectId);
            issueAuditManager.LogChange(audit, ItemAttributeVisibility.AssociatedCustomFields, customField.Entity.CustomFieldId.ToString(),
            string.Empty, string.Empty, beforeValue, afterValue);
        }

        /// <summary>
        /// Check if the "Fremd-ID"-field is ""
        /// Then it checks if the ID is in the description
        /// if there is no ID, then it checks in the comment section
        /// </summary>
        /// <param name="args">Properties of the task</param>
        /// <returns>args.Issue</returns>
        public IssueDto BeforeUpdateFull(IssueDtoEventArgs args)
        {
            try
            {
                List <IssueCommentDto> comments = args.Issue.Comments;
                string description = args.Issue.Description;
                string fieldName = GetAppConfigValue("customFieldName");
                CustomFieldDataDto fremdIDField = args.Issue.CustomFields.Find(field => field.Name.Equals(fieldName));

                if (fremdIDField.Entity.Data == "")
                {
                    if (FindID(description) != "")
                    {
                        string beforeValue = fremdIDField.Entity.Data;
                        string valueIdDescription = FindID(description);
                        string fieldValue = fremdIDField.Entity.Data = valueIdDescription;
                        CreateAuditlog(args.Context, args.Issue.Entity.Id, args.Issue.Entity.ProjectId, fremdIDField, beforeValue, fieldValue);
                    }
                    else
                    {
                        foreach (IssueCommentDto comment in comments)
                        {
                            string valueIdComment = FindID(comment.Entity.Comment);
                            if (valueIdComment != "")
                            {
                                string fieldValue = fremdIDField.Entity.Data = valueIdComment;
                                IssueManager issueManager = new IssueManager(GeminiApp.Cache(), GeminiApp.UserContext(), args.Context);
                                issueManager.Update(args.Issue);
                                CreateAuditlog(args.Context, args.Issue.Entity.Id, args.Issue.Entity.ProjectId, fremdIDField, "", fieldValue);
                                break;
                            }
                        }
                    }
                }
            }

            catch (Exception e)
            {
                int issueID = args.Issue.Id;
                string message = string.Format("IssueID: {0}", issueID);
                GeminiApp.LogException(e, false, message);
            }
            return args.Issue;
        }

        public Issue BeforeAssign(IssueEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeClose(IssueEventArgs args)
        {
            return args.Entity;
        }

        public IssueComment BeforeComment(IssueCommentEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeCreate(IssueEventArgs args)
        {
            return args.Entity;
        }

        public IssueDto BeforeCreateFull(IssueDtoEventArgs args)
        {
            return args.Issue;
        }

        public Issue BeforeDelete(IssueEventArgs args)
        {
            return args.Entity;
        }

        public IssueDto BeforeIssueCopy(IssueDtoEventArgs args)
        {
            return args.Issue;
        }

        public Issue BeforeResolutionChange(IssueEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeStatusChange(IssueEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeUpdate(IssueEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeWatcherAdd(IssueEventArgs args)
        {
            return args.Entity;
        }

        public IssueDto CopyIssueComplete(IssueDtoEventArgs args)
        {
            return args.Issue;
        }

        public string AppGuid
        {
            get
            {
                return "E56F3E06-FEA7-4BC5-BE14-D081A993AFA3";
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
<<<<<<< HEAD
                return "Searching after Fremd-ID in Task description or comments and set Fremd-ID";
=======
                return "Search for foreign Id with RegEx in task description or comments and set customfield. Configure the RegEx pattern and the name of the custom field in the App.config file.";
>>>>>>> e72a7049b512e04b4f70e7e1a57cbd2bd1eccd5d
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
                return "Search Fremd-ID";
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}