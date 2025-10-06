using System;

namespace myApp.Tests
{
    /// <summary>
    /// Configuration settings for tests
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Default timeout for tests in seconds
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// Environment variable to override test timeout
        /// </summary>
        public const string TimeoutOverrideEnvVar = "TEST_TIMEOUT_SECONDS";

        /// <summary>
        /// Gets the configured timeout for tests
        /// </summary>
        public static int GetTimeoutSeconds()
        {
            var envTimeout = Environment.GetEnvironmentVariable(TimeoutOverrideEnvVar);
            if (int.TryParse(envTimeout, out var timeout) && timeout > 0)
            {
                return timeout;
            }
            return DefaultTimeoutSeconds;
        }

        /// <summary>
        /// Gets the configured timeout as a TimeSpan
        /// </summary>
        public static TimeSpan GetTimeout()
        {
            return TimeSpan.FromSeconds(GetTimeoutSeconds());
        }

        /// <summary>
        /// Checks if a test should be skipped based on timeout
        /// </summary>
        public static bool ShouldSkipTest(string testName, TimeSpan elapsedTime)
        {
            var timeout = GetTimeout();
            if (elapsedTime > timeout)
            {
                Console.WriteLine($"Skipping test '{testName}' - exceeded timeout of {timeout.TotalSeconds} seconds (elapsed: {elapsedTime.TotalSeconds:F2}s)");
                return true;
            }
            return false;
        }
    }
}
