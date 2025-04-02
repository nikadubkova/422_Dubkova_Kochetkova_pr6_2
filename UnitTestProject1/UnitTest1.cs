using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;



namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            int a = 10;
            int b = 20;

            int sum = a + b;

            Assert.AreEqual(30, sum, "Сумма 10 и 20 должна быть 30.");
        }
    }
}