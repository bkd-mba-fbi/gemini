using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnlockUser;

namespace UnitTestUnlockUser
{
    [TestClass]
    public class UnlockU
    {
        [TestMethod]
        public void TestGetUnlockTime()
        {
            UnlockUserClass unlockUserClass = new UnlockUserClass();
            TimeSpan time = DateTime.Now.TimeOfDay;
            TimeSpan minutes = TimeSpan.FromMinutes(15);
            TimeSpan unlockTime = time.Add(minutes);
            string result = unlockTime.ToString(@"hh\:mm");
            string expectedTime = unlockUserClass.GetUnlockTime();
            Assert.AreEqual(expectedTime, result);
        }
    }
}
