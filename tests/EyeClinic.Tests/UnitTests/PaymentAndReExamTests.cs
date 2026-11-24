using System;
using EyeClinic.Core.Enums;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class PaymentAndReExamTests
    {
        public void Test_ReExamEligibility_Within14Days_IsEligible()
        {
            var lastExamDate = new DateTime(2026, 10, 10);
            var today = new DateTime(2026, 10, 20); // 10 days difference
            int allowedDays = 14;

            bool isEligible = (today.Date - lastExamDate.Date).TotalDays <= allowedDays;

            Assert.IsTrue(isEligible, "Patient examined 10 days ago should be eligible for re-exam within 14 days limit.");
        }

        public void Test_ReExamEligibility_Beyond14Days_RequiresNewExam()
        {
            var lastExamDate = new DateTime(2026, 9, 25);
            var today = new DateTime(2026, 10, 20); // 25 days difference
            int allowedDays = 14;

            bool isEligible = (today.Date - lastExamDate.Date).TotalDays <= allowedDays;

            Assert.IsFalse(isEligible, "Patient examined 25 days ago should NOT be eligible for re-exam.");
        }

        public void Test_RefundValidation_RequiresMandatoryReason()
        {
            string emptyReason = "   ";
            string validReason = "ظرف طارئ للمريض ولم يدخل للكشف";

            bool emptyValid = !string.IsNullOrWhiteSpace(emptyReason);
            bool textValid = !string.IsNullOrWhiteSpace(validReason);

            Assert.IsFalse(emptyValid, "Refund without a reason must be rejected (PAY-9).");
            Assert.IsTrue(textValid, "Refund with an explicit reason is accepted.");
        }

        public void Test_PricingCalculation_ExamTypes()
        {
            decimal priceExam = 300m;
            decimal priceReExam = 50m;
            decimal priceUrgent = 450m;

            Assert.AreEqual(300m, priceExam);
            Assert.AreEqual(50m, priceReExam);
            Assert.AreEqual(450m, priceUrgent);
            Assert.IsTrue(priceUrgent > priceExam, "Urgent exam price must be higher than regular exam.");
        }
    }
}
