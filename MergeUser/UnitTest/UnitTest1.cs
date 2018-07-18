using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MergeUser;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRegexPattern()
        {
            Merge merge = new Merge();
            string input = "test@erz.be.ch";
            string actual = merge.FindDomain(input);
            string expected = "erz.be.ch";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestEmptyEmailInput()
        {
            Merge merge = new Merge();
            string input = "";
            string result = merge.FindDomain(input);
            string expected = string.Empty;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestInvalidEmailInput()
        {
            Merge merge = new Merge();
            string input = "test.erz.be.ch";
            string result = merge.FindDomain(input);
            string expected = string.Empty;
            Assert.AreEqual(expected, result);
        }
    }
}
