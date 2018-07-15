using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using VoidRewardParser.Logic;

namespace UnitTestProject
{
    [TestClass]
    public class ApiTests
    {
        [TestMethod]
        public async Task TestPlatinum()
        {
            String search = "Oberon Prime Blueprint";

            var minSell = await PlatinumPrices.GetPrimePlatSellOrders(search);

            Assert.IsTrue(minSell.HasValue && minSell >= 1 && minSell < 100);
        }

        [TestMethod]
        public async Task TestDucat()
        {
            String search = "akbronco_prime_blueprint";

            var ducat = await DucatPrices.GetPrimePlatDucats(search);

            Assert.IsTrue(ducat.HasValue && ducat == 15);
        }
    }
}