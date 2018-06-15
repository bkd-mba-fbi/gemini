using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReplaceParagraph;
using Countersoft.Gemini.Commons;

namespace TestReplaceParagraph
{
    [TestClass]
    public class ReplaceParagraphTest
    {
        [TestMethod]
        public void RegexTest() // Einzeiliger Teststring
        {
            BeforeFormatHtml replace = new BeforeFormatHtml();
            string htmlString = "<p class=\"MsoNormal\"><span style=\"font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D\"><o:p>&nbsp;</o:p></span></p>";
            string formated = string.Empty;
            string result = replace.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(formated, result); // Der Kommentar sollte korrekt formatiert sein
        }

        [TestMethod]
        public void RegexMultipleLineTest() // Mehrzeiliger Teststring
        {
            BeforeFormatHtml replace = new BeforeFormatHtml();
            string htmlString = @"<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>&nbsp;</o:p></span></p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>Should not delete</o:p></span></p>
<p class=""MsoNormal"">Should not delete</p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>&nbsp;</o:p></span></p>";
            string formated = @"
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>Should not delete</o:p></span></p>
<p class=""MsoNormal"">Should not delete</p>
";
            string result = replace.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(formated, result); // Der Kommentar sollte korrekt formatiert sein
        }

        [TestMethod]
        public void RegexNegativTest() // Negativtest: Der Teststring sollte nicht formatiert werden
        {
            BeforeFormatHtml replace = new BeforeFormatHtml();
            string htmlString = @"<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D;mso-fareast-language:EN-US""><o:p></o:p></span></p>
<p class=""MsoNormal""><b><span lang=""FR"" style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif"">De&nbsp;:</span></b><span lang=""FR"" style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif"">";
            string result = replace.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(htmlString, result); // Das Resultat muss dem htmlString entsprechen, da es keine Änderungen geben sollte
        }
    }
}