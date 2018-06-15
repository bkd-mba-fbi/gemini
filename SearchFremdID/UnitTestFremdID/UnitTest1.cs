using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SearchFremdID;

namespace UnitTestFremdID
{
    [TestClass]
    public class TestFremdID
    {
        [TestMethod]
        public void FU1()
        {
            FremdID fremdId = new FremdID();
            string teststring = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, 
sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, 
sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. 
Stet clita kasd gubergren, no sea takimata sanctus est EVO-2134 Lorem ipsum dolor sit amet. 
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, 
sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. 
Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            string expectedId = "EVO-2134";
            string result = fremdId.FindID(teststring);
            Assert.AreEqual(expectedId, result);
        }

        [TestMethod]
        public void FU2()
        {
            FremdID fremdId = new FremdID();
            string teststring = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, 
sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, 
sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. 
Stet clita kasd gubergren, no sea takimata sanctus est A4O-2018-02-12-V01-Escada Lorem ipsum dolor sit amet. 
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, 
sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. 
Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            string expectedId = "A4O-2018-02-12-V01-Escada";
            string result = fremdId.FindID(teststring);
            Assert.AreEqual(expectedId, result);
        }

        [TestMethod]
        public void FU3()
        {
            FremdID fremdId = new FremdID();
            string teststring = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr.";
            string expected = "";
            string result = fremdId.FindID(teststring);
            Assert.AreEqual(expected, result);
        }
    }
}
