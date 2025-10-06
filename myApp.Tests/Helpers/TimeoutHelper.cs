using System;
using System.Diagnostics;
using System.Threading.Tasks;
using myApp.Tests.Attributes;

namespace myApp.Tests.Helpers
{
    /// <summary>
    /// Helper class for managing test timeouts
    /// </summary>
    public static class TimeoutHelper
    {
        /// <summary>
        /// Executes a test with timeout protection
        /// </summary>
        public static async Task<bool> ExecuteWithTimeout(Func<Task> testAction, string testName, int? customTimeoutSeconds = null)
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
        /// Executes a test with timeout protection and returns result
        /// </summary>
        public static async Task<T> ExecuteWithTimeout<T>(Func<Task<T>> testAction, string testName, int? customTimeoutSeconds = null)
        {
            var timeout = customTimeoutSeconds ?? TestConfiguration.GetTimeoutSeconds();
            var timeoutSpan = TimeSpan.FromSeconds(timeout);
            
            using var cts = new System.Threading.CancellationTokenSource(timeoutSpan);
            
            try
            {
                return await testAction();
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"Test '{testName}' timed out after {timeout} seconds");
                throw new TimeoutException($"Test '{testName}' exceeded timeout of {timeout} seconds");
            }
        }

        /// <summary>
        /// Measures execution time of a test
        /// </summary>
        public static async Task<(bool Success, TimeSpan Elapsed)> MeasureTestExecution(Func<Task> testAction, string testName, int? customTimeoutSeconds = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = await ExecuteWithTimeout(testAction, testName, customTimeoutSeconds);
            stopwatch.Stop();
            
            return (success, stopwatch.Elapsed);
        }

        /// <summary>
        /// Gets timeout for a test method, checking for TimeoutAttribute
        /// </summary>
        public static int GetTimeoutForTest(System.Reflection.MethodInfo method)
        {
            var timeoutAttr = method.GetCustomAttributes(typeof(TimeoutAttribute), false);
            if (timeoutAttr.Length > 0)
            {
                return ((TimeoutAttribute)timeoutAttr[0]).TimeoutSeconds;
            }
            
            return TestConfiguration.GetTimeoutSeconds();
        }
    }
}
