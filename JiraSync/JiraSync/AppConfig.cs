using System.Collections.Generic;

namespace JiraSync.Configuration
{
    public class AppConfig
    {
        public List<JiraService> JiraServices { get; set; }
    }

    public class JiraService
        {
            public string JiraUrlSearch { get; set; }
            public string PersonalAccessToken { get; set; }
            public SearchPostBody SearchPostBody { get; set; }
            public string TargetGeminProject { get; set; }
            public int TargetGeminProjectId { get; set; }
            public string GeminiCustomFieldJiraKey { get; set; }
            public int GeminiCustomFieldJiraKeyId { get; set; }
            public string DefaultComponent { get; set; }
            public int DefaultComponentId { get; set; }
            public List<Mapping> Mapping { get; set; }
        }

        public class Mapping
        {
            public string Property { get; set; }
            public string Source { get; set; }
            public string Target { get; set; }
            public int TargetId { get; set; }
    }



    /// <summary>
    /// Naming rule violation: These words must begin with upper case characters: jql JiraSync.
    /// Its important begin with lower characters because on post body you will receive a 400 Bad Request.
    /// </summary>
    public class SearchPostBody
        {
            public string jql { get; set; }
            public int maxResults { get; set; }
            public List<string> fields { get; set; }
        }


 }
