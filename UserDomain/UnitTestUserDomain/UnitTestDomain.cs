using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UserDomain;

namespace UnitTestUserDomain
{
    [TestClass]
    public class UnitTestDomain
    {
        [TestMethod]
        public void TestFindDomain()
        {
            AfterUserDomain userDomainClass = new AfterUserDomain();
            string email = "vorname.nachnaem@bff.ch";
            string expectedDomain = "bff.ch";
            string result = userDomainClass.FindDomain(email);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain2()
        {
            AfterUserDomain userDomainClass = new AfterUserDomain();
            string email = "";
            string expectedDomain = "";
            string result = userDomainClass.FindDomain(email);
            Assert.AreEqual(expectedDomain, result);
        }

        [TestMethod]
        public void TestFindDomain3()
        {
            AfterUserDomain userDomainClass = new AfterUserDomain();
            string testString = "Teststring";
            string expectedDomain = "";
            string result = userDomainClass.FindDomain(testString);
            Assert.AreEqual(expectedDomain, result);
        }
    }
}
