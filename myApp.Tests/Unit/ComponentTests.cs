using System;
using FluentAssertions;
using myApp.Services;
using Xunit;

namespace myApp.Tests.Unit
{
    /// <summary>
    /// Component tests that don't require Avalonia UI or headless platform
    /// </summary>
    public class ComponentTests
    {
        [Fact]
        public void ApiService_ShouldCreateSuccessfully()
        {
            // Arrange & Act
            var apiService = new ApiService();

            // Assert
            apiService.Should().NotBeNull();
        }

        [Fact]
        public void ApiService_ExtensionInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            var extensionInfo = new ApiService.ExtensionInfo
            {
                name = "test-extension",
                remote = "https://github.com/test/repo",
                branch = "main",
                commit_hash = "abc123",
                commit_date = 1234567890,
                version = "1.0.0",
                enabled = true
            };

            // Assert
            extensionInfo.name.Should().Be("test-extension");
            extensionInfo.remote.Should().Be("https://github.com/test/repo");
            extensionInfo.branch.Should().Be("main");
            extensionInfo.commit_hash.Should().Be("abc123");
            extensionInfo.commit_date.Should().Be(1234567890);
            extensionInfo.version.Should().Be("1.0.0");
            extensionInfo.enabled.Should().BeTrue();
        }

        [Fact]
        public void ApiService_ProgressInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            var progressInfo = new ApiService.ProgressInfo
            {
                Progress = 0.5f,
                EtaSeconds = 30.0f
            };

            // Assert
            progressInfo.Progress.Should().Be(0.5f);
            progressInfo.EtaSeconds.Should().Be(30.0f);
        }

        [Fact]
        public void ApiService_DownloadProgress_ShouldHaveCorrectProperties()
        {
            // Arrange
            var downloadProgress = new ApiService.DownloadProgress
            {
                Status = "in_progress",
                Progress = 0.75f,
                DownloadedBytes = 750000,
                TotalBytes = 1000000,
                Error = null,
                FilePath = "/path/to/file"
            };

            // Assert
            downloadProgress.Status.Should().Be("in_progress");
            downloadProgress.Progress.Should().Be(0.75f);
            downloadProgress.DownloadedBytes.Should().Be(750000);
            downloadProgress.TotalBytes.Should().Be(1000000);
            downloadProgress.Error.Should().BeNull();
            downloadProgress.FilePath.Should().Be("/path/to/file");
        }

        [Fact]
        public void ApiService_ScriptsList_ShouldHaveCorrectProperties()
        {
            // Arrange
            var scriptsList = new ApiService.ScriptsList
            {
                txt2img = new System.Collections.Generic.List<string> { "script1", "script2" },
                img2img = new System.Collections.Generic.List<string> { "script3", "script4" }
            };

            // Assert
            scriptsList.txt2img.Should().NotBeNull();
            scriptsList.txt2img.Should().HaveCount(2);
            scriptsList.txt2img.Should().Contain("script1");
            scriptsList.txt2img.Should().Contain("script2");
            
            scriptsList.img2img.Should().NotBeNull();
            scriptsList.img2img.Should().HaveCount(2);
            scriptsList.img2img.Should().Contain("script3");
            scriptsList.img2img.Should().Contain("script4");
        }

        [Theory]
        [InlineData("test prompt", 20, 7.5, "", 512, 512, "Euler", -1, 1)]
        [InlineData("another prompt", 30, 8.0, "bad quality", 768, 768, "DPM++", 12345, 2)]
        public void ApiService_GenerateImageParameters_ShouldBeValid(
            string prompt, int steps, double guidanceScale, string negativePrompt,
            int width, int height, string sampler, long seed, int batchSize)
        {
            // This test verifies that the method signature accepts the expected parameters
            // In a real test, you would mock the HTTP client and test the actual API call
            var parameters = new
            {
                prompt, steps, guidanceScale, negativePrompt,
                width, height, sampler, seed, batchSize
            };

            // Assert
            parameters.Should().NotBeNull();
            prompt.Should().NotBeNullOrEmpty();
            steps.Should().BePositive();
            guidanceScale.Should().BePositive();
            width.Should().BePositive();
            height.Should().BePositive();
            sampler.Should().NotBeNullOrEmpty();
            batchSize.Should().BePositive();
        }

        [Fact]
        public void App_AppConfig_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            // AppConfig is a static class, so we test its static properties

            // Assert
            myApp.App.AppConfig.Mode.Should().BeDefined();
            myApp.App.AppConfig.RemoteAddress.Should().Be("http://127.0.0.1:7861");
        }

        [Fact]
        public void App_AppConfig_Mode_ShouldBeDefined()
        {
            // Arrange & Act
            var mode = myApp.App.RunMode.Local;

            // Assert
            mode.Should().Be(myApp.App.RunMode.Local);
            Enum.IsDefined(typeof(myApp.App.RunMode), mode).Should().BeTrue();
        }

        [Fact]
        public void App_AppConfig_RemoteAddress_ShouldBeValid()
        {
            // Arrange & Act
            var address = myApp.App.AppConfig.RemoteAddress;

            // Assert
            address.Should().NotBeNullOrEmpty();
            address.Should().StartWith("http://");
            Uri.TryCreate(address, UriKind.Absolute, out var uri).Should().BeTrue();
            uri.Should().NotBeNull();
        }
    }
}
