using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Commons.Permissions;
using Countersoft.Gemini.Contracts;
using Countersoft.Gemini.Extensibility.Events;
using Countersoft.Gemini.Infrastructure.Managers;
using System;
using Countersoft.Gemini.Extensibility.Apps;


namespace UserDomain
{
    [AppType(AppTypeEnum.Timer),
        AppGuid("D8C62E01-7F6A-474B-B891-695AE9872CC6"),
        AppName("User Domain"),
        AppDescription("Specify the task creator's domain in a custom field and add each user from the same domain as watcher except blacklisted domains. Configure the domain blacklist and the custom field in the App.config file.")]

    public class AfterUserDomain : AbstractIssueListener
    {
        
    }
}