using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace myApp.Tests
{
    /// <summary>
    /// Custom test runner that handles timeouts
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// Runs a test with timeout protection
        /// </summary>
        public static async Task<bool> RunTestWithTimeout(Func<Task> testAction, string testName, int? customTimeoutSeconds = null)
        {
            var timeout = customTimeoutSeconds ?? TestConfiguration.GetTimeoutSeconds();
            var timeoutSpan = TimeSpan.FromSeconds(timeout);
            
            using var cts = new System.Threading.CancellationTokenSource(timeoutSpan);
            
            try
            {
                await testAction();
                return true;
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"Test '{testName}' timed out after {timeout} seconds");
                return false;
            }
        }

        /// <summary>
        /// Runs a test method with timeout protection
        /// </summary>
        public static async Task<bool> RunTestMethodWithTimeout(MethodInfo method, object instance, object[] parameters)
        {
            var testName = $"{method.DeclaringType?.Name}.{method.Name}";
            var customTimeout = GetTimeoutFromAttribute(method);
            
            var testAction = async () =>
            {
                if (method.ReturnType == typeof(Task))
                {
                    var task = (Task)method.Invoke(instance, parameters);
                    if (task != null)
                    {
                        await task;
                    }
                }
                else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var task = (Task)method.Invoke(instance, parameters);
                    if (task != null)
                    {
                        await task;
                    }
                }
                else
                {
                    method.Invoke(instance, parameters);
                }
            };

            return await RunTestWithTimeout(testAction, testName, customTimeout);
        }

        /// <summary>
        /// Gets timeout from TimeoutAttribute if present
        /// </summary>
        private static int? GetTimeoutFromAttribute(MethodInfo method)
        {
            var timeoutAttr = method.GetCustomAttribute<Attributes.TimeoutAttribute>();
            return timeoutAttr?.TimeoutSeconds;
        }
    }
}
