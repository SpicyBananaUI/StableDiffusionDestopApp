using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using Xunit;

namespace myApp.Tests
{
    /// <summary>
    /// Base class for Avalonia headless tests
    /// </summary>
    public abstract class TestBase : IClassFixture<HeadlessAppFixture>
    {
        protected HeadlessAppFixture App { get; }

        protected TestBase(HeadlessAppFixture app)
        {
            App = app;
        }

        /// <summary>
        /// Creates a test container for UI components without requiring a window
        /// </summary>
        protected Control CreateTestContainer(Control content)
        {
            // For headless testing, we'll use a simple container instead of a window
            // This avoids the IWindowingPlatform requirement
            var container = new Avalonia.Controls.ContentControl
            {
                Content = content,
                Width = 800,
                Height = 600
            };
            return container;
        }

        /// <summary>
        /// Runs an action on the UI thread and waits for completion
        /// </summary>
        protected async Task RunOnUIThread(Action action)
        {
            await Dispatcher.UIThread.InvokeAsync(action);
        }

        /// <summary>
        /// Runs a function on the UI thread and waits for completion
        /// </summary>
        protected async Task<T> RunOnUIThread<T>(Func<T> function)
        {
            return await Dispatcher.UIThread.InvokeAsync(function);
        }

        /// <summary>
        /// Waits for a condition to be true with a timeout
        /// </summary>
        protected async Task WaitForCondition(Func<bool> condition, int timeoutMs = 1000) // Reduced from 5000ms to 1000ms
        {
            var startTime = DateTime.Now;
            while (!condition() && (DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                await Task.Delay(25); // Reduced from 50ms to 25ms
            }
            
            if (!condition())
            {
                throw new TimeoutException($"Condition not met within {timeoutMs}ms");
            }
        }

        /// <summary>
        /// Executes a test with timeout protection
        /// </summary>
        protected async Task<bool> ExecuteWithTimeout(Func<Task> testAction, string testName, int? customTimeoutSeconds = null)
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
        /// Measures execution time of a test
        /// </summary>
        protected async Task<(bool Success, TimeSpan Elapsed)> MeasureTestExecution(Func<Task> testAction, string testName, int? customTimeoutSeconds = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = await ExecuteWithTimeout(testAction, testName, customTimeoutSeconds);
            stopwatch.Stop();
            
            return (success, stopwatch.Elapsed);
        }

        /// <summary>
        /// Simulates a click on a control
        /// </summary>
        protected void Click(Control control)
        {
            // For headless testing, we'll just set focus and trigger events
            control.Focus();
            // In a real implementation, you would create proper pointer events
            // This is a simplified version for headless testing
        }

        /// <summary>
        /// Simulates text input on a control
        /// </summary>
        protected void SetText(TextBox textBox, string text)
        {
            textBox.Text = text;
            textBox.Focus();
            // In a real implementation, you would create proper text input events
            // This is a simplified version for headless testing
        }
    }
}
