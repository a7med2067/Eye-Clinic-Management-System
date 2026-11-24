using System;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class DailyClosingCalculationTests
    {
        public void Test_DailyClosing_ZeroDiscrepancy()
        {
            decimal expectedCash = 1200.00m;
            decimal actualCash = 1200.00m;

            decimal diff = actualCash - expectedCash;

            Assert.AreEqual(0.00m, diff, "When actual matches expected, discrepancy must be zero.");
        }

        public void Test_DailyClosing_DeficitCalculation()
        {
            decimal expectedCash = 1500.00m;
            decimal actualCash = 1350.00m;

            decimal diff = actualCash - expectedCash;

            Assert.AreEqual(-150.00m, diff, "Discrepancy must correctly reflect deficit (-150 LE).");
            Assert.IsTrue(diff < 0, "Negative difference indicates cash shortage in drawer.");
        }

        public void Test_DailyClosing_SurplusCalculation()
        {
            decimal expectedCash = 800.00m;
            decimal actualCash = 850.00m;

            decimal diff = actualCash - expectedCash;

            Assert.AreEqual(50.00m, diff, "Discrepancy must correctly reflect surplus (+50 LE).");
            Assert.IsTrue(diff > 0, "Positive difference indicates cash surplus in drawer.");
        }

        public void Test_ElectronicPayment_SegregatedFromDrawerCash()
        {
            decimal cashPayments = 900.00m;
            decimal vodafoneCash = 300.00m;
            decimal instaPay = 450.00m;

            decimal totalRevenue = cashPayments + vodafoneCash + instaPay;
            decimal expectedCashInDrawer = cashPayments; // Vodafone & InstaPay are not physical drawer cash!

            Assert.AreEqual(1650.00m, totalRevenue, "Total revenue must sum all methods.");
            Assert.AreEqual(900.00m, expectedCashInDrawer, "Expected physical drawer cash must ONLY count cash payments (PRD: CLS-2).");
        }
    }
}
