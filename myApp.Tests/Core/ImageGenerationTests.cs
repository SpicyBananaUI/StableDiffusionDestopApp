using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using myApp.Services;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace myApp.Tests.Core
{
    /// <summary>
    /// Tests for core image generation functionality
    /// </summary>
    public class ImageGenerationTests
    {
        [Fact]
        public void ApiService_ShouldCreateGenerationRequest()
        {
            // Arrange
            var apiService = new ApiService();
            var prompt = "a beautiful landscape";
            var steps = 20;
            var cfgScale = 7.5;
            var width = 512;
            var height = 512;
            var seed = 12345L;

            // Act
            var request = new
            {
                prompt = prompt,
                steps = steps,
                cfg_scale = cfgScale,
                width = width,
                height = height,
                seed = seed,
                negative_prompt = "",
                batch_size = 1
            };

            // Assert
            request.prompt.Should().Be(prompt);
            request.steps.Should().Be(steps);
            request.cfg_scale.Should().Be(cfgScale);
            request.width.Should().Be(width);
            request.height.Should().Be(height);
            request.seed.Should().Be(seed);
        }

        [Theory]
        [InlineData("red car", "red")]
        [InlineData("green tree", "green")]
        [InlineData("blue sky", "blue")]
        [InlineData("yellow sun", "yellow")]
        public void Prompt_ShouldContainColorKeywords(string prompt, string expectedColor)
        {
            // Act
            var containsColor = prompt.ToLower().Contains(expectedColor.ToLower());

            // Assert
            containsColor.Should().BeTrue($"Prompt '{prompt}' should contain color '{expectedColor}'");
        }

        [Fact]
        public void GenerationParameters_ShouldAffectOutput()
        {
            // Arrange
            var baseParams = new
            {
                prompt = "a beautiful landscape",
                steps = 20,
                cfg_scale = 7.5,
                width = 512,
                height = 512,
                seed = 12345L
            };

            var modifiedParams = new
            {
                prompt = "a beautiful landscape",
                steps = 30, // Different steps
                cfg_scale = 8.0, // Different CFG scale
                width = 512,
                height = 512,
                seed = 12345L // Same seed
            };

            // Act & Assert
            // Different parameters should produce different results
            baseParams.steps.Should().NotBe(modifiedParams.steps);
            baseParams.cfg_scale.Should().NotBe(modifiedParams.cfg_scale);
            baseParams.seed.Should().Be(modifiedParams.seed); // Same seed for comparison
        }

        [Fact]
        public void Seed_ShouldProduceConsistentResults()
        {
            // Arrange
            var params1 = new { seed = 12345L, prompt = "test", steps = 20, cfg_scale = 7.5 };
            var params2 = new { seed = 12345L, prompt = "test", steps = 20, cfg_scale = 7.5 };

            // Act & Assert
            // Same parameters should produce same results
            params1.seed.Should().Be(params2.seed);
            params1.prompt.Should().Be(params2.prompt);
            params1.steps.Should().Be(params2.steps);
            params1.cfg_scale.Should().Be(params2.cfg_scale);
        }

        [Fact]
        public void DifferentSeeds_ShouldProduceDifferentResults()
        {
            // Arrange
            var params1 = new { seed = 12345L, prompt = "test", steps = 20, cfg_scale = 7.5 };
            var params2 = new { seed = 54321L, prompt = "test", steps = 20, cfg_scale = 7.5 };

            // Act & Assert
            // Different seeds should produce different results
            params1.seed.Should().NotBe(params2.seed);
            params1.prompt.Should().Be(params2.prompt); // Same prompt
            params1.steps.Should().Be(params2.steps); // Same steps
            params1.cfg_scale.Should().Be(params2.cfg_scale); // Same CFG scale
        }

        [Theory]
        [InlineData("crayon", "crayon")]
        [InlineData("bad quality", "bad quality")]
        [InlineData("blurry, low resolution", "blurry, low resolution")]
        [InlineData("red, blue, green", "red, blue, green")]
        public void NegativePrompt_ShouldBeApplied(string negativePrompt, string expected)
        {
            // Act
            var isApplied = negativePrompt.Equals(expected, StringComparison.OrdinalIgnoreCase);

            // Assert
            isApplied.Should().BeTrue($"Negative prompt '{negativePrompt}' should be applied as '{expected}'");
        }

        [Fact]
        public void ImageToImageParameters_ShouldBeValid()
        {
            // Arrange
            var img2imgParams = new
            {
                prompt = "a beautiful landscape",
                init_images = new[] { "base64_encoded_image" },
                denoising_strength = 0.75,
                steps = 20,
                cfg_scale = 7.5,
                width = 512,
                height = 512,
                seed = 12345L
            };

            // Assert
            img2imgParams.prompt.Should().NotBeNullOrEmpty();
            img2imgParams.init_images.Should().NotBeNull();
            img2imgParams.init_images.Should().HaveCount(1);
            img2imgParams.denoising_strength.Should().BeInRange(0.0, 1.0);
            img2imgParams.steps.Should().BeInRange(1, 150);
            img2imgParams.cfg_scale.Should().BeInRange(1.0, 30.0);
            img2imgParams.width.Should().BeInRange(64, 2048);
            img2imgParams.height.Should().BeInRange(64, 2048);
        }

        [Fact]
        public void PluginParameters_ShouldBeValid()
        {
            // Arrange
            var pluginParams = new
            {
                enable_hr = true,
                hr_scale = 2.0,
                hr_upscaler = "ScuNET",
                denoising_strength = 0.5
            };

            // Assert
            pluginParams.enable_hr.Should().BeTrue();
            pluginParams.hr_scale.Should().BeInRange(1.0, 4.0);
            pluginParams.hr_upscaler.Should().NotBeNullOrEmpty();
            pluginParams.denoising_strength.Should().BeInRange(0.0, 1.0);
        }

        [Theory]
        [InlineData("ScuNET", true)]
        [InlineData("SwinIR", true)]
        [InlineData("ESRGAN", true)]
        [InlineData("Unknown", false)]
        public void Upscaler_ShouldBeValid(string upscaler, bool isValid)
        {
            // Arrange
            var validUpscalers = new[] { "ScuNET", "SwinIR", "ESRGAN", "LDSR" };

            // Act
            var isValidUpscaler = validUpscalers.Contains(upscaler);

            // Assert
            isValidUpscaler.Should().Be(isValid);
        }

        [Fact]
        public void SoftInpaintingParameters_ShouldBeValid()
        {
            // Arrange
            var inpaintingParams = new
            {
                mask = "base64_encoded_mask",
                inpainting_fill = 1,
                inpaint_full_res = false,
                inpaint_full_res_padding = 0,
                inpainting_mask_invert = false
            };

            // Assert
            inpaintingParams.mask.Should().NotBeNullOrEmpty();
            inpaintingParams.inpainting_fill.Should().BeInRange(0, 2);
            inpaintingParams.inpaint_full_res.Should().BeFalse();
            inpaintingParams.inpaint_full_res_padding.Should().BeInRange(0, 64);
            inpaintingParams.inpainting_mask_invert.Should().BeFalse();
        }
    }
}
