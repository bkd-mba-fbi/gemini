using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using JiraSync.Configuration;
using JiraSync.Enums;
using JiraSync.Models;
using Newtonsoft.Json;
using Issues = JiraSync.Models.Issues;

namespace JiraSync
{
    [AppType(AppTypeEnum.Timer),
        AppGuid("4a348174-a464-4c09-aa62-c30eca557224"),
        AppName("Jira sync"),
        AppDescription("This application synchronizes data between Jira and Gemini. Configuration options and usage instructions are provided in the documentation.")]
    public class JiraGeminiSyncJob : TimerJob
    {

        private static readonly HttpClient _client = new HttpClient();
        private static AppConfig _appConfig = new AppConfig();
        private static IssueManager _issueManager;
        private static List<IssueDto> _geminiIssues;
        private static List<IssueDto> _geminiIssuesClosed;

        /// <summary>
        /// Main method that will be called by the Gemini timer job, it will read the appconfig file, 
        /// get the jira services and then call the JiraSynch method for each jira service.
        /// </summary>
        /// <param name="issueManager"></param>
        /// <returns></returns>
        public override bool Run(IssueManager issueManager)
        {
            _issueManager = issueManager;
            string appconfigFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AppConfig.json";
            AppConfig appconfig = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(appconfigFile));
            _appConfig = appconfig;
            
            LogDebugMessage(string.Concat("START: ", DateTime.Now.ToString()));
            foreach (JiraService jiraService in appconfig.JiraServices)
            {
                _geminiIssues = GetGeminiIssues(jiraService, false);
                _geminiIssuesClosed = GetGeminiIssues(jiraService, true);
                GetMappings(jiraService);
                JiraSynch(jiraService);

            }
            LogDebugMessage(string.Concat("END: ", DateTime.Now.ToString()));
            return true;
        }

        /// <summary>
        /// Get the interval for the timer job, it will get the interval from the data store, if not found it will return 5 minutes as default interval.
        /// </summary>
        /// <param name="dataStore"></param>
        /// <returns></returns>
        public override TimerJobSchedule GetInterval(Countersoft.Gemini.Contracts.Business.IGlobalConfigurationWidgetStore dataStore)
        {
            var data = dataStore.Get<TimerJobSchedule>(AppGuid);

            if (data == null || data.Value == null)
            {
                return new TimerJobSchedule(5);
            }

            return data.Value;
        }
        
        /// <summary>
        /// Shutdown method for the timer job, it will be called when the job is stopped.
        /// </summary>
        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Main method to synchronize Jira issues with Gemini, it will get the jira data, check the versions and check the issues, 
        /// </summary>
        /// <param name="jiraService"></param>
        public void JiraSynch(JiraService jiraService)
        {
            try
            {

                Task<string> result = GetJiraData(jiraService);

                JiraResponse jiraData = JsonConvert.DeserializeObject<JiraResponse>(result.Result);
     
                Project project = _issueManager.GeminiContext.Projects.Get(jiraService.TargetGeminProject);
                jiraService.TargetGeminProjectId = project.Id;
                jiraService.GeminiCustomFieldJiraKeyId = _issueManager.GeminiContext.CustomFields.GetAll().First(c => c.Name == jiraService.GeminiCustomFieldJiraKey && c.TemplateId == project.TemplateId).Id;
                jiraService.DefaultComponentId = _issueManager.GeminiContext.Components.GetForProject(jiraService.TargetGeminProjectId).First(c => c.Name == jiraService.DefaultComponent).Id;

                LogDebugMessage($"Jira issues total: {jiraData.Issues.Count}");
                                

                foreach (Issues jiraissue in jiraData.Issues)
                {
                    CheckVersion(jiraissue, jiraService);
                    CheckIssue(jiraissue, jiraService);
                }

            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, true, string.Concat("JiraSynch ", exception.Message));
            }

        }

        /// <summary>
        /// Get the Jira data by calling the Jira API with the search post body, it will return the response as a string, 
        /// if the response is not 200 OK, it will log an exception.
        /// </summary>
        /// <param name="jiraService"></param>
        /// <returns></returns>
        public async Task<string> GetJiraData(JiraService jiraService)
        {

            string responsebody = null;

            string uriSerach = jiraService.JiraUrlSearch;
            try
            {

                var email = jiraService.JiraUsername;

                var authToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{email}:{jiraService.PersonalAccessToken}")
                );

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                //new AuthenticationHeaderValue("Bearer", jiraService.PersonalAccessToken);
                string body = JsonConvert.SerializeObject(jiraService.SearchPostBody).ToString();
                StringContent content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage result = _client.PostAsync(uriSerach, content).Result;

                responsebody = await result.Content.ReadAsStringAsync();
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    GeminiApp.LogException($"Response from {uriSerach} is not 200 OK.", result.StatusCode.ToString(), true);
                }
                LogDebugMessage($"Jira Response {result.StatusCode}");
                return responsebody;
            }
            catch (Exception ex)
            {
                GeminiApp.LogException(ex, false, ex.Message);
            }
            return responsebody;

        }

        /// <summary>
        /// Check the versions in the Jira issue, if the version is not exist in Gemini, 
        /// it will create the version in Gemini, if the version is exist but the released value is different, it will update the version in Gemini.
        /// </summary>
        /// <param name="jiraissue"></param>
        /// <param name="jiraService"></param>
        public void CheckVersion(Issues jiraissue, JiraService jiraService)
        {
            int projectId = jiraService.TargetGeminProjectId;
            try
            {
                List<Countersoft.Gemini.Commons.Entity.Version> versions = _issueManager.GeminiContext.Versions.FindWhere(v => v.ProjectId == projectId);
                foreach (FixVersions fixversion in jiraissue.Fields.FixVersions)
                {
                    Countersoft.Gemini.Commons.Entity.Version exists = versions.Find(v => v.ProjectId == projectId && v.Name == fixversion.Name);
                    /*
                    if (exists != null)
                    {

                        if (exists.Released != fixversion.Released)
                        {

                            exists.Released = fixversion.Released;
                            _issueManager.GeminiContext.Versions.Update(exists);
                            LogDebugMessage($"Update Version {exists.Name}");
                        } 
                

                    }*/
                    if (exists == null)
                    {

                        Countersoft.Gemini.Commons.Entity.Version createVersion = new Countersoft.Gemini.Commons.Entity.Version
                        {
                            ProjectId = projectId,
                            Active = true,
                            Name = fixversion.Name,
                            Label = fixversion.Name,
                            Released = fixversion.Released,
                        };

                        createVersion = _issueManager.GeminiContext.Versions.Create(createVersion);
                        Mapping mapping = new Mapping
                        {
                            Property = "version",
                            Source = createVersion.Name,
                            Target = createVersion.Name,
                            TargetId = createVersion.Id
                        };

                        jiraService.Mapping.Add(mapping);
                        LogDebugMessage($"Create Version {createVersion.Name}");

                    }
                }
            }
            catch (Exception ex)
            {
                GeminiApp.LogException(ex, true, ex.Message);
            }
        }

        /// <summary>
        /// Get the description from the Jira issue, it will concatenate all the text from the content and children of the description field.
        /// For every text it will add a <br> tag to separate the lines, and also replace the \n and \r with <br> to make sure the line breaks are preserved in Gemini.
        /// </summary>
        /// <param name="jiraissue"></param>
        /// <returns></returns>
        public string GetDescription(Issues jiraissue)
        {
           string description = string.Empty;
            foreach (Content item in jiraissue.Fields.Description.Content)
            {
                if (item.Children != null)
                {
                    string typeParent = item.Type;
                    foreach (Children content in item.Children)
                    {
                        if (content.Text != null)
                        {   
                            description += GetTextFormat(typeParent, content.Text);
                        }
                    }
                }
            }

            return description.Replace("\n", "<br>").Replace("\r", "<br>");
        }

        /// <summary>
        /// Format the text based on its type.
        /// </summary>
        /// <param name="type">The type of the text (e.g., paragraph, codeBlock).</param>
        /// <param name="text">The text to format.</param>
        /// <returns>The formatted text.    </returns>
        public string GetTextFormat(string type, string text)
        {
            switch (type)   
            {   case "paragraph":
                    return $"<p>{text}</p>";
                case "codeBlock":
                    return $"<code>{text}</code>";    
    
                default:
                    return text;
            }
        }   

        /// <summary>
        /// FinalTargetStatus is used to check if the issue is in the final target status, if it is in the final target status, it will not update the status of the issue in Gemini,
        /// </summary>
        /// <param name="jiraService"></param>
        /// <param name="issue"></param>
        /// <returns></returns>
        public bool FinalTargetStatus(JiraService jiraService, IssueDto issue) {
            return jiraService.FinalTargetStatus == issue.Status;
            }

        /// <summary>
        /// Check the issue in Gemini, if the issue is exist, it will update the issue in Gemini based on the Jira issue, 
        /// if the issue is not exist, it will create the issue in Gemini based on the Jira issue.
        /// Add the link to the Jira issue in the description of the Gemini issue, and also add the custom field value for the Jira key in Gemini to link the issues together.
        /// </summary>
        /// <param name="jiraissue"></param>
        /// <param name="jiraService"></param>
        public void CheckIssue(Issues jiraissue, JiraService jiraService)
        {
            try
            {
                bool updated = false;
                string fremdId = jiraissue.Key;

                IssueDto exists = GetIssue(fremdId, jiraService);


                if (exists != null)
                {
                    int existHash = GetIssueHash(exists);
                    IssueDto updateIssue = exists;

                    updateIssue.Project = _issueManager.GeminiContext.Projects.Get(jiraService.TargetGeminProjectId);
                    updateIssue.Entity.ProjectId = jiraService.TargetGeminProjectId;
                    updateIssue.Entity.TypeId = int.Parse(GetTarget(jiraService, jiraissue.Fields.Issuetype.Name, updateIssue, MappingProperty.issuetype));
                    updateIssue.Entity.PriorityId = int.Parse(GetTarget(jiraService, jiraissue.Fields.Priority.Name, updateIssue, MappingProperty.priority));
                    if(!FinalTargetStatus(jiraService,updateIssue))
                    {
                        updateIssue.Entity.StatusId = int.Parse(GetTarget(jiraService, jiraissue.Fields.Status.Name, updateIssue, MappingProperty.status));
                    }
                   
                    if (jiraissue.Fields.FixVersions.Count() > 0)
                    {
                        updateIssue.Entity.FixedInVersionId = _issueManager.GeminiContext.Versions.GetAll().First(v => v.Name == jiraissue.Fields.FixVersions[0].Name && v.ProjectId == jiraService.TargetGeminProjectId).Id;
                    }

                    if (jiraissue.Fields.Resolution != null)
                    {
                        updateIssue.Entity.ResolutionId = int.Parse(GetTarget(jiraService, jiraissue.Fields.Resolution.Name, updateIssue, MappingProperty.resolution));
                    }

                    updateIssue.CustomFields.First(c => c.Entity.Data == fremdId).Entity.CustomFieldId = jiraService.GeminiCustomFieldJiraKeyId;

                    if (!updateIssue.Description.Contains(fremdId))
                    {
                        updateIssue.Entity.Description = GetJiraBrowseUrl(jiraService, fremdId, updateIssue.Description);
                    }
    ;
                    updateIssue.Entity.Components = GetComponents(jiraService, jiraissue.Fields.Components, updateIssue);


                    updated = true;

                    if (existHash != GetIssueHash(updateIssue))
                    {
                        updateIssue = _issueManager.Update(updateIssue);
                        LogDebugMessage($"Updated issue: {updateIssue.ProjectCode}-{updateIssue.Id}");
                    }

                }

                if (!updated && jiraService.Mapping.Exists(t => t.Property == "issuetype" && t.Source == jiraissue.Fields.Issuetype.Name))
                {
                    Countersoft.Gemini.Commons.Entity.Issue task = new Countersoft.Gemini.Commons.Entity.Issue
                    {
                        ProjectId = jiraService.TargetGeminProjectId,
                        Title = jiraissue.Fields.Summary,
                        Description = GetJiraBrowseUrl(jiraService, fremdId, GetDescription(jiraissue)),
                        Components = jiraService.DefaultComponentId.ToString()


                    };
                    IssueDto issueDtoCreated = _issueManager.Create(task);

                    issueDtoCreated.CustomFields.First(c => c.Entity.CustomFieldId == jiraService.GeminiCustomFieldJiraKeyId).Entity.Data = fremdId;
                    _issueManager.Update(issueDtoCreated);

                    LogDebugMessage($"Created issue: {issueDtoCreated.ProjectCode}-{issueDtoCreated.Id}");
                    updated = true;

                }
                
            }
            catch (Exception ex)
            {
                LogDebugMessage($"{jiraissue.Key}  {ex.Message}");
                GeminiApp.LogException(ex, true, ex.Message);
            }
        }

        /// <summary>
        /// since we can not do a direct query to get the issue with the custom field value, 
        /// we need to get all issues with custom fields and then find the issue with the matching custom field value. 
        /// This method is used to get all issues with custom fields, if includeClosed is true, it will include closed issues as well and also filter to the appconfig.TargetGeminProject.
        /// </summary>
        /// <param name="jiraService"></param>
        /// <param name="includeClosed"></param>
        /// <returns></returns>
        List<IssueDto> GetGeminiIssues(JiraService jiraService, bool includeClosed)
        {

            IssuesFilter filter = new IssuesFilter
            {
                IncludeClosed = false,
                RevisedAfter = DateTime.Now.AddMonths(-12).ToShortDateString()
            };
            
            if (includeClosed)
            {
                filter.IncludeClosed = true;
                filter.RevisedAfter = DateTime.Now.AddMonths(-6).ToShortDateString();
                return _issueManager.GetIssues(filter, 3000).Where(s => s.ClosedDate.HasValue && s.CustomFields.Count > 0 && s.ProjectCode == jiraService.TargetGeminProject).ToList();
            }
           
            return _issueManager.GetIssues(filter, 1000).Where(c => c.CustomFields.Count() > 0).ToList();

        }

        /// <summary>
        /// Get issue with the matching custom field value, if not found in the already loaded _geminiIssues, 
        /// it will call GetGeminiIssues with includeClosed = true to include closed issues as well and also filter to the appconfig.TargetGeminProject. 
        /// If still not found, it will return null.
        /// </summary>
        /// <param name="fremdId"></param>
        /// <param name="jiraService"></param>
        /// <returns></returns>
        IssueDto GetIssue(string fremdId, JiraService jiraService)
        {
            bool exists = false;
            foreach (IssueDto item in _geminiIssues)
            {
                exists = item.CustomFields.Exists(f => f.Name == jiraService.GeminiCustomFieldJiraKey && f.Entity.Data.Contains(fremdId));
                if (exists)
                {
                    return item;
                }
            }
      
             foreach (IssueDto item in _geminiIssuesClosed)
            {
                exists = item.CustomFields.Exists(f => f.Name == jiraService.GeminiCustomFieldJiraKey && f.Entity.Data.Contains(fremdId));
                if (exists)
                {
                    return item;
                }
            }
            return null;

        }

        /// <summary>
        /// Get the target id for each mapping based on the source value and the property type, 
        /// if the mapping is not found, it will log a debug message and return 0 as target id.
        /// </summary>
        /// <param name="jiraService"></param>
        /// <returns></returns>
        public JiraService GetMappings(JiraService jiraService)
        {
            try
            {

                Project project = _issueManager.GeminiContext.Projects.Get(jiraService.TargetGeminProject);
                foreach (Mapping item in jiraService.Mapping)
                {
                    switch (item.Property)
                    {
                        case "issuetype":
                            item.TargetId = _issueManager.GeminiContext.Meta.TypeGet().First(t => t.Label == item.Target && t.TemplateId == project.TemplateId).Id;
                            break;
                        case "components":
                            item.TargetId = _issueManager.GeminiContext.Components.GetAll().First(c => c.Name == item.Target && c.ProjectId == project.Id).Id;
                            break;
                        case "status":
                            item.TargetId = _issueManager.GeminiContext.Meta.StatusGet().FirstOrDefault(s => s.Label == item.Target && s.TemplateId == project.TemplateId).Id;
                            break;
                        case "resolution":
                            item.TargetId = _issueManager.GeminiContext.Meta.ResolutionGet().First(r => r.Label == item.Target && r.TemplateId == project.TemplateId).Id;
                            break;
                        case "priority":
                            item.TargetId = _issueManager.GeminiContext.Meta.PriorityGet().First(p => p.Label == item.Target && p.TemplateId == project.TemplateId).Id;
                            break;


                        default:
                            break;
                    }
                }

                return jiraService;
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, true, string.Concat("JiraSynch ", exception.Message));
            }
            return jiraService;
        }

        /// <summary>
        /// Get the components for the issue, it will check if the component is already exist in the issue, if not it will add the component to the issue.
        /// </summary>
        /// <param name="jiraService"></param>
        /// <param name="components"></param>
        /// <param name="issueDto"></param>
        /// <returns></returns>
        internal string GetComponents(JiraService jiraService, List<Components> components, IssueDto issueDto)
        {
            string issueComponent = issueDto.Entity.Components;
            foreach (Components component in components)
            {
                string value = GetTarget(jiraService, component.Name, issueDto, MappingProperty.components).ToString();
                string newValue = issueComponent.PadRight(1) == "|" ? value : "|" + value;
                if (!issueComponent.Contains(value) && int.Parse(value) > 0)
                {
                    issueComponent = issueComponent + newValue;
                }

                issueComponent = issueComponent.Replace("||", "|");

            }
            return issueComponent;

        }

        /// <summary>
        /// Get the hash code for the issue based on the properties that we want to compare, 
        /// in this case we are comparing StatusId, ResolutionId, Components, ProjectId, FixedInVersionId, TypeId and Description length.
        /// </summary>
        /// <param name="issueDto"></param>
        /// <returns></returns>
        internal int GetIssueHash(IssueDto issueDto)
        {
            string issueProp = issueDto.Entity.StatusId + issueDto.Entity.ResolutionId + issueDto.Entity.Components + issueDto.Entity.ProjectId + issueDto.Entity.FixedInVersionId + issueDto.Entity.TypeId + issueDto.Entity.Description.Length;
            return issueProp.GetHashCode();
        }

        /// <summary>
        /// Get the target id for the mapping based on the source value and the property type, 
        /// if the mapping is not found, it will log a debug message and return 0 as target id.
        /// </summary>
        /// <param name="jiraService"></param>
        /// <param name="source"></param>
        /// <param name="issueDto"></param>
        /// <param name="mappingProperty"></param>
        /// <returns></returns>
        internal string GetTarget(JiraService jiraService, string source, IssueDto issueDto, MappingProperty mappingProperty)
        {
            int id = 0;
            try
            {
                id = jiraService.Mapping.First(s => s.Source == source).TargetId;
                if (id == 0)
                {
                    LogDebugMessage($"Mapping Target {mappingProperty} not found: {source}");
                }
                return id.ToString();
            }
            catch (Exception ex)
            {
                LogDebugMessage($"Mapping Source {mappingProperty} not found: {source}");
                GeminiApp.LogException(ex, true, ex.Message);
            }
            if (id == 0)
            {
                switch (mappingProperty)
                {
                    case MappingProperty.issuetype:
                        return issueDto.Entity.TypeId.ToString();
                    case MappingProperty.components:
                        return issueDto.Entity.Components;
                    case MappingProperty.status:
                        return issueDto.Entity.StatusId.ToString();
                    case MappingProperty.resolution:
                        return issueDto.Entity.ResolutionId.ToString();
                    case MappingProperty.priority:
                        return issueDto.Entity.PriorityId.ToString();
                    default:
                        break;
                }
            }
            return id.ToString();


        }

        /// <summary>
        /// Get the Jira browse url for the issue, it will return a string with the link to the Jira issue and the description of the Gemini issue,
        /// </summary>
        /// <param name="jiraService"></param>
        /// <param name="issuekeyJira"></param>
        /// <param name="descriptionGemini"></param>
        /// <returns></returns>
        internal string GetJiraBrowseUrl(JiraService jiraService, string issuekeyJira, string descriptionGemini)
        {
            Uri jiraServiceUrl = new Uri(jiraService.JiraUrlSearch);
            return $"<p><a href=\"{ jiraServiceUrl.Scheme}://{jiraServiceUrl.Host}/browse/{issuekeyJira}\" target=\"_blank\">{issuekeyJira}</a></p><br>{descriptionGemini}";
        }

    }

}