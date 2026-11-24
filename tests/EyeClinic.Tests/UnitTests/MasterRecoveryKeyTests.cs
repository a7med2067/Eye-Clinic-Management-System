using System;
using EyeClinic.Business.Services;
using EyeClinic.Data.Security;
using EyeClinic.Tests.Framework;

namespace EyeClinic.Tests.UnitTests
{
    public class MasterRecoveryKeyTests
    {
        public void Test_GenerateRecoveryKey_LengthAndUnambiguousCharset()
        {
            string key = AuthService.GenerateRecoveryKey();

            Assert.IsNotNull(key, "Generated key must not be null");
            Assert.AreEqual(29, key.Length, "Key with dashes must be 29 characters (6 groups of 4 separated by 5 dashes)");

            string[] groups = key.Split('-');
            Assert.AreEqual(6, groups.Length, "Key must have exactly 6 groups");
            foreach (var g in groups)
            {
                Assert.AreEqual(4, g.Length, "Each group must have 4 characters");
            }

            // Must NOT contain ambiguous characters '0', '1', 'O', 'I'
            Assert.IsFalse(key.Contains("0"), "Key should not contain '0'");
            Assert.IsFalse(key.Contains("1"), "Key should not contain '1'");
            Assert.IsFalse(key.Contains("O"), "Key should not contain 'O'");
            Assert.IsFalse(key.Contains("I"), "Key should not contain 'I'");
        }

        public void Test_NormalizeRecoveryKey_HandlesDashesSpacesAndCase()
        {
            string rawKey = "9f4k-7m2p-x8a3-b5w9-q1r6-t4h7";
            string normalized = AuthService.NormalizeRecoveryKey(rawKey);
            Assert.AreEqual("9F4K7M2PX8A3B5W9Q1R6T4H7", normalized, "Should convert to uppercase and strip dashes");

            string withSpaces = " 9F4K 7M2P X8A3 B5W9 Q1R6 T4H7 ";
            string normalizedSpaces = AuthService.NormalizeRecoveryKey(withSpaces);
            Assert.AreEqual("9F4K7M2PX8A3B5W9Q1R6T4H7", normalizedSpaces, "Should strip spaces");
        }

        public void Test_MasterRecoveryKey_CryptographicHashing()
        {
            string rawKey = "9F4K-7M2P-X8A3-B5W9-Q1R6-T4H7";
            string normalized = AuthService.NormalizeRecoveryKey(rawKey);

            var auth = PasswordHasher.HashPassword(normalized);
            Assert.IsNotNull(auth.hash, "Hash should be generated");
            Assert.IsNotNull(auth.salt, "Salt should be generated");

            // Verify with exact normalized key
            bool valid = PasswordHasher.VerifyPassword(normalized, auth.hash, auth.salt);
            Assert.IsTrue(valid, "PasswordHasher should successfully verify normalized recovery key");

            // Verify that wrong key fails
            bool invalid = PasswordHasher.VerifyPassword("WRONGKEY1234567890123456", auth.hash, auth.salt);
            Assert.IsFalse(invalid, "Wrong key must fail verification");
        }

        public void Test_RecoverDoctorPassword_ValidationRules()
        {
            var authService = new AuthService();

            // Empty key
            var res1 = authService.RecoverDoctorPassword("", "newpass123");
            Assert.IsFalse(res1.IsSuccess, "Should fail on empty key");

            // Short password
            var res2 = authService.RecoverDoctorPassword("9F4K-7M2P-X8A3-B5W9-Q1R6-T4H7", "123");
            Assert.IsFalse(res2.IsSuccess, "Should fail on short password < 6 chars");
        }
    }
}
