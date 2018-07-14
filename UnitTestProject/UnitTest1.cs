using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VoidRewardParser.Logic;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async void TestPlatinum()
        {
            var minSell = await PlatinumPrices.GetPrimePlatSellOrders("Oberon Prime Blueprint");
            Assert.IsTrue(minSell > 10);
        }

        [TestMethod]
        public async void TestDucat()
        {
            var ducat = await DucatPrices.GetPrimePlatDucats("akbronco_prime_blueprint");
            Assert.IsTrue(ducat == 15);
        }
    }
}