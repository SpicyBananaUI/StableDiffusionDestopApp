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
            // Arrange & Act
            var mainWindow = new myApp.MainWindow();
            var container = CreateTestContainer(mainWindow);

            // Assert
            mainWindow.Should().NotBeNull();
            container.Should().NotBeNull();
            container.Should().BeOfType<Avalonia.Controls.ContentControl>();
        }

        [Fact]
        public void ModeSelectorWindow_ShouldCreateSuccessfully()
        {
            // Arrange & Act
            var modeSelector = new myApp.ModeSelectorWindow();
            var container = CreateTestContainer(modeSelector);

            // Assert
            modeSelector.Should().NotBeNull();
            container.Should().NotBeNull();
            container.Should().BeOfType<Avalonia.Controls.ContentControl>();
        }

        [Fact]
        public void IntroWindow_ShouldCreateSuccessfully()
        {
            // Arrange & Act
            var introWindow = new myApp.IntroWindow();
            var container = CreateTestContainer(introWindow);

            // Assert
            introWindow.Should().NotBeNull();
            container.Should().NotBeNull();
            container.Should().BeOfType<Avalonia.Controls.ContentControl>();
        }
    }
}
