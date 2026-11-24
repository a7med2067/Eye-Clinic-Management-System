using System;
using System.Reflection;
using EyeClinic.Tests.Framework;
using EyeClinic.Tests.UnitTests;

namespace EyeClinic.Tests
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=================================================");
            Console.WriteLine(" Eye Clinic Management System - Unit Test Runner ");
            Console.WriteLine(" Target: .NET Framework 4.8 | C# 7.3             ");
            Console.WriteLine("=================================================");
            Console.WriteLine();
            Type[] testClasses = new Type[]
            {
                typeof(PatientLogicTests),
                typeof(PaymentAndReExamTests),
                typeof(DailyClosingCalculationTests),
                typeof(BackupServiceTests),
                typeof(ExcelExportServiceTests),
                typeof(PatientImportServiceTests),
                typeof(UserAccountValidationTests),
                typeof(MasterRecoveryKeyTests)
            };

            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;

            foreach (var testClass in testClasses)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Running: " + testClass.Name);
                Console.ResetColor();

                object instance = Activator.CreateInstance(testClass);
                var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    if (method.Name.StartsWith("Test_"))
                    {
                        totalTests++;
                        try
                        {
                            method.Invoke(instance, null);
                            passedTests++;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("  [PASS] ");
                            Console.ResetColor();
                            Console.WriteLine(method.Name);
                        }
                        catch (TargetInvocationException ex)
                        {
                            failedTests++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("  [FAIL] ");
                            Console.ResetColor();
                            Console.WriteLine(method.Name);

                            var inner = ex.InnerException ?? ex;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("         Error: " + inner.Message);
                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine(string.Format("Total Tests: {0} | Passed: {1} | Failed: {2}", totalTests, passedTests, failedTests));

            if (failedTests == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALL UNIT TESTS PASSED SUCCESSFULLY! (100% OK)");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SOME UNIT TESTS FAILED!");
                Console.ResetColor();
                return 1;
            }
        }
    }
}
