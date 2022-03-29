using Countersoft.Gemini;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Events;
using System;
using System.Text.RegularExpressions;

namespace ReplaceParagraph
{
    /// <summary>
    /// BeforeFormatHtml class with an interface IIssueBeforeListener.
    /// </summary>
    public class BeforeFormatHtml : IIssueBeforeListener
    {
        /// <summary>
        /// Replace html empty paragraph "<p> </p>" with ""
        /// or Empty eMail to Ticketline
        /// </summary>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public string FormatHtmlString(string htmlText)
        {
            string formatedHtml = htmlText;
            
            formatedHtml = htmlText.Replace("<p> </p>", "");
            formatedHtml = htmlText.Replace("<p class=\"MsoNormal\"> </p>", "");           
            formatedHtml = formatedHtml.Replace("<p class=\"MsoNormal\"><span lang=\"DE-CH\"><o:p>&nbsp;</o:p></span></p>", "");
            formatedHtml = formatedHtml.Replace("<p class=\"MsoNormal\"><o:p>&nbsp;</o:p></p>\r\n", "");
            return formatedHtml;
        }

        /// <summary>
        /// Before a comment will be created, the comment will be reformatted.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public IssueComment BeforeComment(IssueCommentEventArgs args)
        {
            try
            {
                args.Entity.Comment = FormatHtmlString(args.Entity.Comment);
            }
            catch (Exception e)
            {
                int issueID = args.Entity.Id;
                string message = string.Format("Exception BeforeComment: {0}. IssueID: {1}", e.Message, issueID);
                GeminiApp.LogException(e, false, message);
            }
            return args.Entity;
        }

        /// <summary>
        /// Before a task will be created, the description will be reformatted.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Issue BeforeCreate(IssueEventArgs args)
        {
            try
            {
                args.Entity.Description = FormatHtmlString(args.Entity.Description);
            }
            catch (Exception e)
            {
                int issueID = args.Entity.Id;
                string message = string.Format("IssueID: {0}", issueID);
                GeminiApp.LogException(e, false, message);
            }
            return args.Entity;
        }

        public Issue BeforeAssign(IssueEventArgs args)
        {
            return args.Entity;
        }

        public Issue BeforeClose(IssueEventArgs args)
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
            try
            {
                args.Entity.Description = FormatHtmlString(args.Entity.Description);
            }
            catch (Exception e)
            {
                int issueID = args.Entity.Id;
                string message = string.Format("IssueID: {0}", issueID);
                GeminiApp.LogException(e, false, message);
            }
            return args.Entity;
        }

        public IssueDto BeforeUpdateFull(IssueDtoEventArgs args)
        {
            return args.Issue;
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
                return "44D3C69A-FFF1-4A3D-8756-55A30D84CBC0";
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
                return "Replace paragraph and format HTML string from incoming E-Mail over Breeze App (created comments and description)";
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
                return "Replace Paragraph";
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
