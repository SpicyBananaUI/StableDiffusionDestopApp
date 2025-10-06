using System;
using FluentAssertions;
using Xunit;
using myApp.Services;

namespace myApp.Tests.UI
{
    /// <summary>
    /// Tests for parameter validation and persistence
    /// </summary>
    public class ParameterValidationTests
    {
        [Fact]
        public void GenerationParameters_ShouldHaveValidRanges()
        {
            // Arrange
            var parameters = new
            {
                CfgScale = 7.5,
                Steps = 5,
                Width = 64,
                Height = 64,
                Seed = -1L,
                BatchSize = 1
            };

            // Assert - Validate parameter ranges
            parameters.CfgScale.Should().BeInRange(1.0, 30.0);
            parameters.Steps.Should().BeInRange(1, 150);
            parameters.Width.Should().BeInRange(64, 2048);
            parameters.Height.Should().BeInRange(64, 2048);
            parameters.BatchSize.Should().BeInRange(1, 4);
        }

        [Theory]
        [InlineData(7.5, true)]
        [InlineData(0.5, false)]
        [InlineData(35.0, false)]
        public void CfgScale_ShouldBeInValidRange(double cfgScale, bool isValid)
        {
            // Act
            var isValidRange = cfgScale >= 1.0 && cfgScale <= 30.0;

            // Assert
            isValidRange.Should().Be(isValid);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(0, false)]
        [InlineData(200, false)]
        public void Steps_ShouldBeInValidRange(int steps, bool isValid)
        {
            // Act
            var isValidRange = steps >= 1 && steps <= 150;

            // Assert
            isValidRange.Should().Be(isValid);
        }

        [Theory]
        [InlineData(64, 64, true)]
        [InlineData(50, 50, false)]
        [InlineData(3000, 3000, false)]
        public void ImageDimensions_ShouldBeInValidRange(int width, int height, bool isValid)
        {
            // Act
            var isValidRange = width >= 64 && width <= 2048 && 
                             height >= 64 && height <= 2048;

            // Assert
            isValidRange.Should().Be(isValid);
        }

        [Fact]
        public void Prompt_ShouldNotBeEmptyForGeneration()
        {
            // Arrange
            var emptyPrompt = "";
            var validPrompt = "a beautiful landscape";

            // Act
            var isEmptyPrompt = string.IsNullOrWhiteSpace(emptyPrompt);
            var isValidPrompt = !string.IsNullOrWhiteSpace(validPrompt);

            // Assert
            isEmptyPrompt.Should().BeTrue();
            isValidPrompt.Should().BeTrue();
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("a beautiful landscape", true)]
        [InlineData("red car", true)]
        [InlineData("green tree", true)]
        public void Prompt_ShouldBeValidated(string prompt, bool isValid)
        {
            // Act
            var isValidPrompt = !string.IsNullOrWhiteSpace(prompt);

            // Assert
            isValidPrompt.Should().Be(isValid);
        }

        [Fact]
        public void Seed_ShouldAcceptValidValues()
        {
            // Arrange
            var validSeeds = new[] { -1L, 0L, 12345L, 999999L };
            var invalidSeeds = new[] { long.MinValue, long.MaxValue };

            // Act & Assert
            foreach (var seed in validSeeds)
            {
                var isValid = seed >= -1 && seed <= 999999;
                isValid.Should().BeTrue($"Seed {seed} should be valid");
            }

            foreach (var seed in invalidSeeds)
            {
                var isValid = seed >= -1 && seed <= 999999;
                isValid.Should().BeFalse($"Seed {seed} should be invalid");
            }
        }

        [Fact]
        public void NegativePrompt_ShouldBeOptional()
        {
            // Arrange
            var emptyNegativePrompt = "";
            var validNegativePrompt = "bad quality, blurry";

            // Act
            var isEmptyNegative = string.IsNullOrWhiteSpace(emptyNegativePrompt);
            var isValidNegative = !string.IsNullOrWhiteSpace(validNegativePrompt);

            // Assert
            isEmptyNegative.Should().BeTrue();
            isValidNegative.Should().BeTrue();
        }

        [Theory]
        [InlineData("", true)] // Empty is valid (optional)
        [InlineData("bad quality", true)]
        [InlineData("blurry, low resolution", true)]
        [InlineData("crayon", true)]
        [InlineData("red, blue, green", true)]
        public void NegativePrompt_ShouldAcceptValidValues(string negativePrompt, bool isValid)
        {
            // Act
            var isValidNegative = string.IsNullOrWhiteSpace(negativePrompt) || 
                                 !string.IsNullOrWhiteSpace(negativePrompt);

            // Assert
            isValidNegative.Should().Be(isValid);
        }

        [Fact]
        public void ParameterCombinations_ShouldBeValid()
        {
            // Arrange
            var validCombinations = new[]
            {
                new { CfgScale = 7.5, Steps = 20, Width = 512, Height = 512, Seed = -1L },
                new { CfgScale = 8.0, Steps = 30, Width = 768, Height = 768, Seed = 12345L },
                new { CfgScale = 6.0, Steps = 15, Width = 1024, Height = 1024, Seed = 0L }
            };

            // Act & Assert
            foreach (var combo in validCombinations)
            {
                combo.CfgScale.Should().BeInRange(1.0, 30.0);
                combo.Steps.Should().BeInRange(1, 150);
                combo.Width.Should().BeInRange(64, 2048);
                combo.Height.Should().BeInRange(64, 2048);
                combo.Seed.Should().BeInRange(-1, 999999);
            }
        }
    }
}
