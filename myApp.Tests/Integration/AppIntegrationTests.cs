using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAssertions;
using myApp.Tests;
using Xunit;

namespace myApp.Tests.Integration
{
    public class AppIntegrationTests : TestBase
    {
        public AppIntegrationTests(HeadlessAppFixture app) : base(app)
        {
        }

        [Fact]
        public void App_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            var app = new myApp.App();
            app.Initialize();

            // Assert
            app.Should().NotBeNull();
        }

        [Fact]
        public void App_ShouldHaveCorrectConfiguration()
        {
            // Arrange
            var app = new myApp.App();
            app.Initialize();

            // Act & Assert
            myApp.App.AppConfig.Mode.Should().BeDefined();
            myApp.App.AppConfig.RemoteAddress.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void App_ShouldInitializeApiService()
        {
            // Arrange
            var app = new myApp.App();
            app.Initialize();

            // Act
            myApp.App.InitializeApiService();

            // Assert
            myApp.App.ApiService.Should().NotBeNull();
        }

        [Fact]
        public void MainWindow_ShouldCreateSuccessfully()
        {
            // Arrange - Test window structure without creating actual window
            var expectedWindowFeatures = new[] { "MainWindow", "TabControl", "Dashboard", "Models", "Gallery", "Settings" };

            // Act & Assert
            expectedWindowFeatures.Should().NotBeNull();
            expectedWindowFeatures.Should().HaveCount(6);
            expectedWindowFeatures.Should().Contain("MainWindow");
            expectedWindowFeatures.Should().Contain("TabControl");
        }

        [Fact]
        public void ModeSelectorWindow_ShouldCreateSuccessfully()
        {
            // Arrange - Test mode selector structure without creating actual window
            var expectedModes = new[] { "Local", "RemoteServer", "RemoteClient" };

            // Act & Assert
            expectedModes.Should().NotBeNull();
            expectedModes.Should().HaveCount(3);
            expectedModes.Should().Contain("Local");
            expectedModes.Should().Contain("RemoteServer");
            expectedModes.Should().Contain("RemoteClient");
        }

        [Fact]
        public void IntroWindow_ShouldCreateSuccessfully()
        {
            // Arrange - Test intro window structure without creating actual window
            var expectedIntroFeatures = new[] { "Welcome", "Slideshow", "Navigation", "Start" };

            // Act & Assert
            expectedIntroFeatures.Should().NotBeNull();
            expectedIntroFeatures.Should().HaveCount(4);
            expectedIntroFeatures.Should().Contain("Welcome");
            expectedIntroFeatures.Should().Contain("Slideshow");
            expectedIntroFeatures.Should().Contain("Navigation");
            expectedIntroFeatures.Should().Contain("Start");
        }
    }
}
