using Countersoft.Gemini;
using Countersoft.Gemini.Commons;
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
        /// Reformats the Gemini HTML text from comments and ticket description to remove unnecessary and annoying paragraphs.
        /// Whether the ticket or comment has been created by Breeze or manually by a user does not matter.
        /// Since the HTML pattern is a bit more complex, not every paragraph gets replaced by string.empty, but instead
        /// different steps adjust the necessary changes in the HTML.
        /// </summary>
        /// <param name="htmlText"></param>
        /// <param name="originatorType"></param>
        /// <returns></returns>
        public string FormatHtmlString(string htmlText, IssueOriginatorType originatorType)
        {
            string htmlString = htmlText;
            try
            {
                string unescapedCLRFPattern = @"(\r\n)";
                string copiedContentDivPattern = @"(<\/{0,1}div[^>]*>)";
                string copiedContentBrPattern = @"(<\/{0,1}br \w[^>]*>)";
                string copiedContentParagraphPattern = @"(<\/{0,1}p \w[^>]*>)";

                string emptyParagraphsPattern = @"(<p[^>]*>\s+<\/p>)";
                string paragraphPattern = @"(<\/p><p[^>]*>)";
                string emptyBreakPattern = @"(<br \/>\s+<br \/>)";
                string enclosingParagraphPattern = @"(<\/{0,1}p[^>]*>)";
                string reduceTooManyBreaksPattern = @"(<\/{0,1}br[^>]*>){3,}";

                Regex unescapedCLRFRegex = new Regex(unescapedCLRFPattern);
                htmlString = unescapedCLRFRegex.Replace(htmlString, String.Empty);
                Regex copiedContentDivRegex = new Regex(copiedContentDivPattern);
                htmlString = copiedContentDivRegex.Replace(htmlString, String.Empty);

                Regex emptyParagraphsRegex = new Regex(emptyParagraphsPattern);
                htmlString = emptyParagraphsRegex.Replace(htmlString, String.Empty);
                Regex paragraphRegex = new Regex(paragraphPattern);
                htmlString = paragraphRegex.Replace(htmlString, "<br /><br />");
                Regex emptyBreakRegex = new Regex(emptyBreakPattern);
                htmlString = emptyBreakRegex.Replace(htmlString, String.Empty);
                Regex enclosingParagrapRegex = new Regex(enclosingParagraphPattern);
                htmlString = enclosingParagrapRegex.Replace(htmlString, String.Empty);

                // optional, since multiple break elements can be legitimate, but also annyoing
                // use as preferred
                Regex reduceBreaksRegex = new Regex(enclosingParagraphPattern);
                htmlString = reduceTooManyBreaksPattern.Replace(htmlString, "<br /><br />");

            }
            catch (Exception e)
            {
                GeminiApp.LogException(e, false, e.Message);
            }
            return htmlString;
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
                args.Entity.Comment = FormatHtmlString(args.Entity.Comment, args.Entity.OriginatorType);
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
