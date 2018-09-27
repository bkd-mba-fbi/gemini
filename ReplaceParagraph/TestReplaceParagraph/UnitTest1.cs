using Countersoft.Gemini.Commons;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReplaceParagraph;

namespace TestReplaceParagraph
{
    [TestClass]
    public class ReplaceParagraphTest
    {
        [TestMethod]
        public void RegexTest() // Einzeiliger Teststring
        {
            BeforeFormatHtml beforeFormatHtml = new BeforeFormatHtml();
            string htmlString = "<p class=\"MsoNormal\"><span style=\"font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D\"><o:p>&nbsp;</o:p></span></p>";
            string formatted = "<p class=\"MsoNormal\"><span style=\"font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D\"></span></p>";
            string result = beforeFormatHtml.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(formatted, result); // Der Kommentar sollte korrekt formatiert sein
        }

        [TestMethod]
        public void RegexMultipleLineTest() // Mehrzeiliger Teststring
        {
            BeforeFormatHtml beforeFormatHtml = new BeforeFormatHtml();
            string htmlString = @"<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>&nbsp;</o:p></span></p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>TEST</o:p></span></p>
<p class=""MsoNormal"">Should not delete</p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""><o:p>&nbsp;</o:p></span></p>";
            string formatted = @"<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""></span></p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D"">TEST</span></p>
<p class=""MsoNormal"">Should not delete</p>
<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D""></span></p>";
            string result = beforeFormatHtml.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(formatted, result); // Der Kommentar sollte korrekt formatiert sein
        }

        [TestMethod]
        public void RegexNegativTest() // Negativtest: Der Teststring sollte nicht formatiert werden
        {
            BeforeFormatHtml beforeFormatHtml = new BeforeFormatHtml();
            string htmlString = @"<p class=""MsoNormal""><span style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif;color:#1F497D;mso-fareast-language:EN-US""></span></p>
<p class=""MsoNormal""><b><span lang=""FR"" style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif"">De&nbsp;:</span></b><span lang=""FR"" style=""font-size:11.0pt;font-family:&quot;Calibri&quot;,sans-serif"">";
            string result = beforeFormatHtml.FormatHtmlString(htmlString, IssueOriginatorType.Breeze);
            Assert.AreEqual(htmlString, result); // Das Resultat muss dem htmlString entsprechen, da es keine Änderungen geben sollte
        }
    }
}