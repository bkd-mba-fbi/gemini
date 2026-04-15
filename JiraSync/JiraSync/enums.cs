using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraSync.Enums
{
    /// <summary>
    /// Mapping properties that can be used to map values from Jira to Gemini. 
    /// These properties correspond to the fields in Jira issues that can be mapped to fields in Gemini issues. 
    /// The mapping process will use these properties to identify which fields in Jira should be mapped to which fields 
    /// in Gemini based on the configuration provided in the AppConfig class.
    /// </summary>
    enum MappingProperty
    {
        issuetype,
        components,
        status,
        resolution,
        priority,
    }


}
