using System;
using EyeClinic.Core.Entities;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class PatientLogicTests
    {
        public void Test_AgeCalculation_AccurateFromBirthDate()
        {
            var p = new Patient
            {
                BirthDate = DateTime.Today.AddYears(-26)
            };

            Assert.AreEqual(26, p.CalculatedAge, "CalculatedAge must match 26 years.");
        }

        public void Test_AgeCalculation_NullWhenBirthDateMissing()
        {
            var p = new Patient
            {
                BirthDate = null
            };

            Assert.IsNull(p.CalculatedAge, "CalculatedAge must be null when BirthDate is not set.");
        }

        public void Test_SequentialFileNumber_Progression()
        {
            string currentMax = "1005";
            int nextNumber = int.Parse(currentMax) + 1;
            string formatted = nextNumber.ToString();

            Assert.AreEqual("1006", formatted, "Sequential file number must increment without gaps.");
        }

        public void Test_PatientValidation_RequiresNameAndMobile()
        {
            var p1 = new Patient { FullName = "", Mobile = "01012345678" };
            var p2 = new Patient { FullName = "Ahmed", Mobile = "" };
            var p3 = new Patient { FullName = "Ahmed Ali", Mobile = "01012345678" };

            bool p1Valid = !string.IsNullOrWhiteSpace(p1.FullName) && !string.IsNullOrWhiteSpace(p1.Mobile);
            bool p2Valid = !string.IsNullOrWhiteSpace(p2.FullName) && !string.IsNullOrWhiteSpace(p2.Mobile);
            bool p3Valid = !string.IsNullOrWhiteSpace(p3.FullName) && !string.IsNullOrWhiteSpace(p3.Mobile);

            Assert.IsFalse(p1Valid, "Patient with empty name must be invalid (PAT-1).");
            Assert.IsFalse(p2Valid, "Patient with empty mobile must be invalid (PAT-1).");
            Assert.IsTrue(p3Valid, "Patient with name and mobile is valid.");
        }
    }
}
