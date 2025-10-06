using System;

namespace myApp.Tests.Attributes
{
    /// <summary>
    /// Attribute to specify timeout for individual tests
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TimeoutAttribute : Attribute
    {
        public int TimeoutSeconds { get; }

        public TimeoutAttribute(int timeoutSeconds)
        {
            TimeoutSeconds = timeoutSeconds;
        }
    }
}
