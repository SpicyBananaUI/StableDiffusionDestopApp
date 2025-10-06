using System;
using Avalonia;
using Xunit;

namespace myApp.Tests
{
    /// <summary>
    /// Fixture for setting up basic testing environment
    /// </summary>
    public class HeadlessAppFixture : IDisposable
    {
        public Application Application { get; private set; }

        public HeadlessAppFixture()
        {
            // For basic testing, we don't need to initialize the full Avalonia platform
            // This avoids the headless platform issues
            Application = new App();
        }

        public void Dispose()
        {
            // Application doesn't have Dispose, but we can clean up if needed
        }
    }
}
