using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraSync
{
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    
    public class JiraResponse
    {
        public string Expand { get; set; }
        public int StartAt { get; set; }
        public int MaxResults { get; set; }
        public int Total { get; set; }
        public List<Issue> Issues { get; set; }
    }

    public class Component
    {
        public string Self { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Fields
    {
        public string Summary { get; set; }
        public Issuetype Issuetype { get; set; }
        public DateTime? LastViewed { get; set; }
        public List<Component> Components { get; set; }
        public List<Version> Versions { get; set; }
        public string Description { get; set; }
        public List<FixVersion> FixVersions { get; set; }
        public Priority Priority { get; set; }
        public Resolution Resolution { get; set; }
        public DateTime Updated { get; set; }
        public Status Status { get; set; }
    }

    public class FixVersion
    {
        public string Self { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public bool Archived { get; set; }
        public bool Released { get; set; }
        public DateTime ReleaseDate { get; set; }
    }

    public class Issue
    {
        public string Expand { get; set; }
        public string Id { get; set; }
        public string Self { get; set; }
        public string Key { get; set; }
        public Fields Fields { get; set; }
    }

    public class Issuetype
    {
        public string Self { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string Name { get; set; }
        public bool Subtask { get; set; }
        public int AvatarId { get; set; }
    }

    public class Priority
    {
        public string Self { get; set; }
        public string IconUrl { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class Resolution
    {
        public string Self { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
    }


    public class Status
    {
        public string Self { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public StatusCategory StatusCategory { get; set; }
    }

    public class StatusCategory
    {
        public string Self { get; set; }
        public int Id { get; set; }
        public string Key { get; set; }
        public string ColorName { get; set; }
        public string Name { get; set; }
    }

    public class Version
    {
        public string Self { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public bool Archived { get; set; }
        public bool Released { get; set; }
        public string ReleaseDate { get; set; }
    }


}
