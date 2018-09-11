using Microsoft.VisualStudio.TestTools.UnitTesting;
using UserDomain;

namespace UnitTestUserDomain
{
    /// <summary>
    /// Unit-Tests, Stub-Tests für genauere Tests zusätzlich definierbar
    /// </summary>
    [TestClass]
    public class UnitTestDomain
    {
        [TestMethod]
        public void TestFindDomain()
        {
            Helper helperFunctions = new Helper();
            string email = "vorname.nachnaem@bff.ch";
            string expectedDomain = "bff.ch";
            string result = helperFunctions.FindDomain(email);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain2()
        {
            Helper helperFunctions = new Helper();
            string email = "";
            string expectedDomain = "";
            string result = helperFunctions.FindDomain(email);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain3()
        {
            Helper helperFunctions = new Helper();
            string testString = "Teststring";
            string expectedDomain = "";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain4()
        {
            Helper helperFunctions = new Helper();
            string testString = "manuela.zulliger@bzemme.ch; helene.moser@bzemme.ch";
            string expectedDomain = "bzemme.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain5()
        {
            Helper helperFunctions = new Helper();
            string testString = "ssd.gdfgdfgdf@xn--stackoverflow.com";
            string expectedDomain = "xn--stackoverflow.com";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain6()
        {
            Helper helperFunctions = new Helper();
            string testString = "t@stackoverflow.xn--com";
            string expectedDomain = "stackoverflow.xn--com";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain7()
        {
            Helper helperFunctions = new Helper();
            string testString = "t@stackoverflow.co.uk";
            string expectedDomain = "stackoverflow.co.uk";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain8()
        {
            Helper helperFunctions = new Helper();
            string testString = "Anita.Wehrli@bfb-bielbienne.ch";
            string expectedDomain = "bfb-bielbienne.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain9()
        {
            Helper helperFunctions = new Helper();
            string testString = "daniela.kuenzi@bsd-bern.ch";
            string expectedDomain = "bsd-bern.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain10()
        {
            Helper helperFunctions = new Helper();
            string testString = "Gabriela.Wuethrich@be-med.ch";
            string expectedDomain = "be-med.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain11()
        {
            Helper helperFunctions = new Helper();
            string testString = "k.berliat@sfgb-b.ch";
            string expectedDomain = "sfgb-b.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain12()
        {
            Helper helperFunctions = new Helper();
            string testString = "marc.rentschler@bbz-biel.ch";
            string expectedDomain = "bbz-biel.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain13()
        {
            Helper helperFunctions = new Helper();
            string testString = "marc.rentschler@bbz-biel.ch; marc.rentschler@bbz-biel.ch";
            string expectedDomain = "bbz-biel.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain14()
        {
            Helper helperFunctions = new Helper();
            string testString = "marc.rentschler@bbz-biel.ch; Gabriela.Wuethrich@be-med.ch";
            string expectedDomain = "bbz-biel.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain15()
        {
            Helper helperFunctions = new Helper();
            string testString = "test@erz.be.ch";
            string expectedDomain = "erz.be.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain16()
        {
            Helper helperFunctions = new Helper();
            string testString = "manuela.zulliger@bzemme.ch";
            string expectedDomain = "bzemme.ch";
            string result = helperFunctions.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }
    }
}
