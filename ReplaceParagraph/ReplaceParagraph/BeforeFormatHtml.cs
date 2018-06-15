using Countersoft.Gemini;
using Countersoft.Gemini.Commons;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Extensibility.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReplaceParagraph
{
    /// <summary>
    /// BeforeFormatHtml class with an interface IIssueBeforeListener.
    /// </summary>
    public class BeforeFormatHtml: IIssueBeforeListener
    {
        /// <summary>
        /// This method checks if the html-string is from originator Type "Breeze".
        /// Every Breeze Type html-string will be formatted. So every paragraphs will be replaced by string.empty.
        /// </summary>
        /// <param name="htmlText"></param>
        /// <param name="originatorType"></param>
        /// <returns></returns>
        public string FormatHtmlString(string htmlText, IssueOriginatorType originatorType)
        {
            string htmlString = htmlText;
            try
            {
                var stringOriginatorType = originatorType;

                if (IssueOriginatorType.Breeze == stringOriginatorType)
                {
                    string pattern = "(<p class=\"MsoNormal\">(.*)>&nbsp;<(.*)</p>|(?s)<table class=\"MsoNormalTable\"(.*)</table>)";
                    Regex regex = new Regex(pattern);
                    htmlString = regex.Replace(htmlString, String.Empty);
                }
            }
            catch (Exception e)
            {
                GeminiApp.LogException(e, false, e.Message);
            }
            return htmlString;
        }

        /// <summary>
        /// Before a comment will be created, the comment will be formatted and overwritten.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public IssueComment BeforeComment(IssueCommentEventArgs args)
        {
            try
            {
                args.Entity.Comment = FormatHtmlString(args.Entity.Comment, args.Entity.OriginatorType);
            }
            catch (Exception e)
            {
                int issueID = args.Entity.Id;
                string message = string.Format("IssueID: {0}", issueID);
                GeminiApp.LogException(e, false, message);
            }
            return args.Entity;
        }

        /// <summary>
        /// Before a task will be created, the description will be formatted and overwritten.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Issue BeforeCreate(IssueEventArgs args)
        {
            try
            {
                args.Entity.Description = FormatHtmlString(args.Entity.Description, args.Entity.OriginatorType);
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
                return "Format HTML String from E-Mail comments and task description";
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
