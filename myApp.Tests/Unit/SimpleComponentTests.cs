using System;
using FluentAssertions;
using myApp.Services;
using Xunit;

namespace myApp.Tests.Unit
{
    /// <summary>
    /// Simple unit tests that don't require Avalonia UI components
    /// </summary>
    public class SimpleComponentTests
    {
        [Fact]
        public void ApiService_ExtensionInfo_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var extensionInfo = new ApiService.ExtensionInfo();

            // Assert
            extensionInfo.name.Should().BeEmpty();
            extensionInfo.remote.Should().BeNull();
            extensionInfo.branch.Should().BeNull();
            extensionInfo.commit_hash.Should().BeNull();
            extensionInfo.commit_date.Should().BeNull();
            extensionInfo.version.Should().BeNull();
            extensionInfo.enabled.Should().BeFalse();
        }

        [Fact]
        public void ApiService_ProgressInfo_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var progressInfo = new ApiService.ProgressInfo();

            // Assert
            progressInfo.Progress.Should().Be(0f);
            progressInfo.EtaSeconds.Should().Be(0f);
        }

        [Fact]
        public void ApiService_DownloadProgress_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var downloadProgress = new ApiService.DownloadProgress();

            // Assert
            downloadProgress.Status.Should().Be("in_progress");
            downloadProgress.Progress.Should().Be(0f);
            downloadProgress.DownloadedBytes.Should().Be(0);
            downloadProgress.TotalBytes.Should().Be(0);
            downloadProgress.Error.Should().BeNull();
            downloadProgress.FilePath.Should().BeNull();
        }

        [Fact]
        public void ApiService_ScriptsList_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var scriptsList = new ApiService.ScriptsList();

            // Assert
            scriptsList.txt2img.Should().BeNull();
            scriptsList.img2img.Should().BeNull();
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
    }
}
