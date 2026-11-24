using System;

namespace EyeClinic.Tests.Framework
{
    public class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }

    public static class Assert
    {
        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!object.Equals(expected, actual))
            {
                throw new AssertException(string.Format("Assert.AreEqual Failed. Expected: <{0}>, Actual: <{1}>. {2}", expected, actual, message));
            }
        }

        public static void AreNotEqual<T>(T notExpected, T actual, string message = "")
        {
            if (object.Equals(notExpected, actual))
            {
                throw new AssertException(string.Format("Assert.AreNotEqual Failed. Value: <{0}>. {1}", actual, message));
            }
        }

        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                throw new AssertException(string.Format("Assert.IsTrue Failed. {0}", message));
            }
        }

        public static void IsFalse(bool condition, string message = "")
        {
            if (condition)
            {
                throw new AssertException(string.Format("Assert.IsFalse Failed. {0}", message));
            }
        }

        public static void IsNotNull(object value, string message = "")
        {
            if (value == null)
            {
                throw new AssertException(string.Format("Assert.IsNotNull Failed. Value is null. {0}", message));
            }
        }

        public static void IsNull(object value, string message = "")
        {
            if (value != null)
            {
                throw new AssertException(string.Format("Assert.IsNull Failed. Value is not null: <{0}>. {1}", value, message));
            }
        }
    }
}
