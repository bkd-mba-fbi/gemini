using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Contracts.Business;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Infrastructure.Managers;
using Countersoft.Gemini.Infrastructure.TimerJobs;
using Countersoft.Gemini.Mailer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnlockUser
{
    [AppType(AppTypeEnum.Timer),
    AppGuid("09B081E3-D07D-4E8B-9F78-7C0D27C54092"),
    AppName("Unlock User"),
    AppDescription("Sending E-Mail to locked User and unlock after configured unlocktime (app.config) in  minutes. Also configure mail subject and body für all user languages (app.config).")]

    public class UnlockUserClass : TimerJob
    {
        public override TimerJobSchedule GetInterval(IGlobalConfigurationWidgetStore dataStore)
        {
            var data = dataStore.Get<TimerJobSchedule>(AppGuid);

            if (data == null || data.Value == null)
            {
                return new TimerJobSchedule(5);
            }
            return data.Value;
        }

        /// <summary>
        /// The complete procedure to find, notify and unlock the locked user.
        /// </summary>
        /// <param name="issueManager"></param>
        /// <returns></returns>
        public override bool Run(IssueManager issueManager)
        {
            UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), issueManager.GeminiContext);
            List<UserDto> users = userManager.GetActiveUsers();
            foreach (UserDto user in users)
            {
                if (user.Entity.Locked == true)
                {
                    DateTime currentTime = DateTime.Now;
                    SendMail(user.Entity, currentTime, GetUnlockTime(user.Entity), issueManager);
                    UnlockUser(issueManager, user.Entity, currentTime);
                }
            }
            return true;
        }

        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get value from different AppConfig settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public KeyValueConfigurationElement GetAppConfigSettings(string settings)
        {
            ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string appConfigFileName = Path.Combine(assemblyFolder, "App.config");
            configFile.ExeConfigFilename = appConfigFileName;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);
            AppSettingsSection appSettings =
                   (AppSettingsSection)config.GetSection("appSettings");
            return appSettings.Settings[settings];
        }

        /// <summary>
        /// This method sends the E-mail with the appropriate user language.
        /// First is to get the locked timestamp and the last run time from the app.
        /// To send an email the locked timestamp has to be greater than the last run time app.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="language"></param>
        public void SendMail(User entity, DateTime currentTime, DateTime unlockTime, IssueManager issueManager)
        {
            string log;
            IGlobalConfigurationWidgetStore dataStore = issueManager.GeminiContext.GlobalConfigurationWidgetStore;
            int intervalMinutes = Convert.ToInt32(GetInterval(dataStore).IntervalInMinutes);

            string language = entity.Language;
            DateTime lockedTimeStamp = entity.Revised.ToLocalTime();
            DateTime lastRunTime = currentTime - new TimeSpan(0, intervalMinutes, 0);

            if (lockedTimeStamp > lastRunTime)
            {
                LogDebugMessage("Benutzer: " + entity.Fullname + " gesperrt.");
                string timeToUnlock = unlockTime.ToString(@"HH\:mm");
                KeyValueConfigurationElement mailbodyLanguageSettings = GetAppConfigSettings(string.Concat("mailbody_", language));
                KeyValueConfigurationElement mailSubjectLanguageSettings = GetAppConfigSettings(string.Concat("mailSubject_", language));
                
                if (mailbodyLanguageSettings == null || mailSubjectLanguageSettings == null)
                {
                   
                    UserManager usermanager = new UserManager(issueManager);
                    List<UserDto> users = usermanager.GetActiveUsers();
                    foreach (UserDto user in users)
                    {
                        if (user.IsGlobalAdmin)
                        {

                            if (!EmailHelper.Send(GeminiApp.Config, "Unlock User App: failure in App.config file", string.Concat(@"<style>
div.container {
background-color: #ffffff;
}
div.container p {
font-family: Arial;
font-size: 14px;
font-style: normal;
font-weight: normal;
text-decoration: none;
text-transform: none;
color: #000000;
background-color: #ffffff;
}
</style>

<div class='container'>
<p>Please create a key for mailbody_ and mailSubject_ in app.config file for user language >>", language ,@"</p><p></p><p>Also Unlock user: ", entity.Username,"</p></div>"), user.Entity.Email, string.Empty, true, out log))
                            {
                                    GeminiApp.LogException(new Exception(log) { Source = "Notification" }, false);
                            }
                            LogDebugMessage("E-Mail Benachrichtigung an " + entity.Fullname + " versendet.");
		                }  
                    }

                }
                else
                {
                    string mailBody = string.Format(mailbodyLanguageSettings.Value, GetAppConfigSettings("unlockTime").Value, "(" + timeToUnlock + ")");

                    if (!EmailHelper.Send(GeminiApp.Config, string.Concat(mailSubjectLanguageSettings.Value),
                    mailBody,
                    entity.Email, string.Empty, true, out log))
                    {
                        GeminiApp.LogException(new Exception(log) { Source = "Notification" }, false);
                    }
                    LogDebugMessage("E-Mail Benachrichtigung an " + entity.Fullname + " versendet.");
                }
            }
        }

        /// <summary>
        /// This method unlocks the user
        /// It sets the user.entity.locked status to false, when the next run time is greater than the unlock time.
        /// </summary>
        /// <param name="issueManager"></param>
        /// <param name="entity"></param>
        public void UnlockUser(IssueManager issueManager, User entity, DateTime currentTime)
        {
            try
            {
                IGlobalConfigurationWidgetStore dataStore = issueManager.GeminiContext.GlobalConfigurationWidgetStore;
                int intervalMinutes = Convert.ToInt32(GetInterval(dataStore).IntervalInMinutes);
                DateTime timeToUnlock = GetUnlockTime(entity);
                DateTime nextRunTime = currentTime + new TimeSpan(0, intervalMinutes, 0);

                if (timeToUnlock < nextRunTime)
                {
                    UserManager userManager = new UserManager(GeminiApp.Cache(), GeminiApp.UserContext(), issueManager.GeminiContext);
                    entity.Locked = false;
                    userManager.Update(entity);
                    LogDebugMessage("Benutzer: " + entity.Fullname + " entsperrt.");
                }
            }
            catch (Exception exception)
            {
                GeminiApp.LogException(exception, false, exception.Message);
            }
        }

        /// <summary>
        /// Calculates the unlock time
        /// The method adds waiting minutes to the current time
        /// The return value is a string (HH:mm)
        /// </summary>
        /// <returns></returns>
        public DateTime GetUnlockTime(User entity)
        {
            DateTime lockedTime = entity.Revised.ToLocalTime();
            int waitTime = Int32.Parse(GetAppConfigSettings("unlockTime").Value);
            DateTime timeToUnlock = lockedTime.AddMinutes(waitTime);
            return timeToUnlock;
        }
    }
}