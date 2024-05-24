using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using JiraSync.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JiraSync
{
    [AppType(AppTypeEnum.Timer),
        AppGuid("4a348174-a464-4c09-aa62-c30eca557224"),
        AppName("Jira sync"),
        AppDescription("")]
    public class JiraGeminiSyncJob : TimerJob
    {

        private static readonly HttpClient _client = new HttpClient();
        private static AppConfig _appConfig = new AppConfig();
        private static IssueManager _issueManager;
        private static List<IssueDto> _geminiIssues;


        public override bool Run(IssueManager issueManager)
        {
            _issueManager = issueManager;
            string appconfigFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AppConfig.json";
            AppConfig appconfig = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(appconfigFile));
            _appConfig = appconfig;
            _geminiIssues = GetGeminiIssues();
            LogDebugMessage(string.Concat("START: ", DateTime.Now.ToString()));
            foreach (JiraService jiraService in appconfig.JiraServices)
            {
                GetMappings(jiraService);
                JiraSynch(jiraService);

            }
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

                LogDebugMessage($"Jira issues total: {jiraData.Total}");

                foreach (Issue issue in jiraData.Issues)
                {
                    CheckVersion(issue, jiraService);
                    CheckIssue(issue, jiraService);
                }

            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, true, string.Concat("JiraSynch ", exception.Message));
            }

        }

        public async Task<string> GetJiraData(JiraService jiraService)
        {

            string responsebody = null;

            string uriSerach = jiraService.JiraUrlSearch;
            try
            {
                _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jiraService.PersonalAccessToken);
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

        public void CheckVersion(Issue issue, JiraService jiraService)
        {
            int projectId = jiraService.TargetGeminProjectId;
            try
            {
                List<Countersoft.Gemini.Commons.Entity.Version> versions = _issueManager.GeminiContext.Versions.FindWhere(v => v.ProjectId == projectId);
                foreach (FixVersion fixversion in issue.Fields.FixVersions)
                {
                    Countersoft.Gemini.Commons.Entity.Version exists = versions.Find(v => v.ProjectId == projectId && v.Name == fixversion.Name);
                    if (exists != null)
                    {
                        if (exists.Released != fixversion.Released || exists.ReleaseDate != fixversion.ReleaseDate)
                        {
                            exists.Released = fixversion.Released;
                            exists.ReleaseDate = fixversion.ReleaseDate;

                            _issueManager.GeminiContext.Versions.Update(exists);
                            LogDebugMessage($"Update Version {exists.Name}");
                        } 
        
                    }
                    else
                    {

                        Countersoft.Gemini.Commons.Entity.Version createVersion = new Countersoft.Gemini.Commons.Entity.Version
                        {
                            ProjectId = projectId,
                            Active = true,
                            Name = fixversion.Name,
                            Label = fixversion.Name,
                            Released = fixversion.Released,
                            ReleaseDate = fixversion.ReleaseDate
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

        public void CheckIssue(Issue issue, JiraService jiraService)
        {
            try
            {
                bool updated = false;
                string fremdId = issue.Key;

                IssueDto exists = GetIssue(fremdId, jiraService);

                if (exists != null)
                {
                    int existHash = GetIssueHash(exists);
                    IssueDto updateIssue = exists;

                    updateIssue.Project = _issueManager.GeminiContext.Projects.Get(jiraService.TargetGeminProjectId);
                    updateIssue.Entity.ProjectId = jiraService.TargetGeminProjectId;
                    updateIssue.Entity.TypeId = GetTarget(jiraService, issue.Fields.Issuetype.Name);
                    updateIssue.Entity.PriorityId = GetTarget(jiraService, issue.Fields.Priority.Name);
                    updateIssue.Entity.StatusId = GetTarget(jiraService, issue.Fields.Status.Name);
                    if (issue.Fields.FixVersions.Count() > 0)
                    {
                        updateIssue.Entity.FixedInVersionId = _issueManager.GeminiContext.Versions.GetAll().First(v => v.Name == issue.Fields.FixVersions[0].Name && v.ProjectId == jiraService.TargetGeminProjectId).Id;
                    }

                    if (issue.Fields.Resolution != null)
                    {
                        updateIssue.Entity.ResolutionId = GetTarget(jiraService, issue.Fields.Resolution.Name);
                    }

                    updateIssue.CustomFields.First(c => c.Entity.Data == fremdId).Entity.CustomFieldId = jiraService.GeminiCustomFieldJiraKeyId;

                    if (!updateIssue.Description.Contains(fremdId))
                    {
                        updateIssue.Entity.Description = GetJiraBrowseUrl(jiraService, fremdId, updateIssue.Description);
                    }

                    updateIssue.Entity.Description = updateIssue.Description.Replace("\r\n", "<br>")
    ;
                    updateIssue.Entity.Components = GetComponents(jiraService, issue.Fields.Components, updateIssue.Entity.Components);


                    updated = true;

                    if (existHash != GetIssueHash(updateIssue))
                    {
                        updateIssue = _issueManager.Update(updateIssue);
                        LogDebugMessage($"Updated issue: {updateIssue.ProjectCode}-{updateIssue.Id}");
                    }

                }

                if (!updated && jiraService.Mapping.Exists(t => t.Property == "issuetype" && t.Source == issue.Fields.Issuetype.Name))
                {
                    Countersoft.Gemini.Commons.Entity.Issue task = new Countersoft.Gemini.Commons.Entity.Issue
                    {
                        ProjectId = jiraService.TargetGeminProjectId,
                        Title = issue.Fields.Summary,
                        Description = GetJiraBrowseUrl(jiraService, fremdId, issue.Fields.Description.Replace("\r\n", "<br>")),
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
                LogDebugMessage(issue.Key + ex.Message);
                GeminiApp.LogException(ex, true, ex.Message);
            }
        }

        List<IssueDto> GetGeminiIssues()
        {

            IssuesFilter filter = new IssuesFilter
            {
                IncludeClosed = false,
            };
            return _issueManager.GetIssues(filter, 1000).Where(c => c.CustomFields.Count() > 0).ToList();

        }

        IssueDto GetIssue(string fremdId, JiraService jiraService)
        {
            bool exists = false;
            foreach (IssueDto item in _geminiIssues)
            {
                exists = item.CustomFields.Exists(f => f.Name == jiraService.GeminiCustomFieldJiraKey && f.Entity.Data == fremdId);
                if (exists)
                {
                    return item;
                }
            }

            return null;

        }

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


        internal string GetComponents(JiraService jiraService, List<Component> components, string issueComponent)
        {
            foreach (Component component in components)
            {

                string value = GetTarget(jiraService, component.Name).ToString();
                string newValue = issueComponent.PadRight(1) == "|" ? value : "|" + value;
                if (!issueComponent.Contains(value) && int.Parse(value) > 0)
                {
                    issueComponent = issueComponent + newValue;
                }

                issueComponent = issueComponent.Replace("||", "|");

            }
            return issueComponent;

        }


        internal int GetIssueHash(IssueDto issueDto)
        {
            string issueProp = issueDto.Entity.StatusId + issueDto.Entity.ResolutionId + issueDto.Entity.Components + issueDto.Entity.ProjectId + issueDto.Entity.FixedInVersionId + issueDto.Entity.TypeId + issueDto.Entity.Description.Length;
            return issueProp.GetHashCode();
        }

        internal int GetTarget(JiraService jiraService, string source)
        {
            int id = 0;
            try
            {
                id = jiraService.Mapping.First(s => s.Source == source).TargetId;
                return id;
            }
            catch (Exception ex)
            {
                LogDebugMessage($"Mapping Source not found:  {source}");
                GeminiApp.LogException(ex, true, ex.Message);
            }
            return id;

        }


        internal string GetJiraBrowseUrl(JiraService jiraService, string issuekeyJira, string descriptionGemini)
        {
            Uri jiraServiceUrl = new Uri(jiraService.JiraUrlSearch);
            return $"<p><a href=\"{ jiraServiceUrl.Scheme}://{jiraServiceUrl.Host}/browse/{issuekeyJira}\" target=\"_blank\">{issuekeyJira}</a></p><br>{descriptionGemini}";
        }

    }

}