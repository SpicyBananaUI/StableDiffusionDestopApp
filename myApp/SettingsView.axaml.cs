using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using myApp.Services;

namespace myApp;

public partial class SettingsView : UserControl
{
    private ApiService _apiService;
    private readonly Dictionary<ToggleSwitch, BoolOptionBinding> _boolBindings = new();
    private readonly List<BoolOptionBinding> _boolBindingList = new();
    private readonly Dictionary<Slider, NumberOptionBinding> _numberBindings = new();
    private readonly List<NumberOptionBinding> _numberBindingList = new();

    public SettingsView()
    {
        WaitForApiReadyAsync();
        InitializeComponent();
        RegisterBindings();
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        
        ConfigManager.Load();
        
        var apiKeyBox = this.FindControl<TextBox>("ApiKeyBox");
        if (apiKeyBox != null)
            apiKeyBox.Text = ConfigManager.Settings.ApiKey ??  string.Empty;
        
        var saveButton = this.FindControl<Button>("SaveApiKeyButton");
        if (saveButton != null)
            saveButton.Click += OnSaveApiKeyClicked;
    }

    private async void WaitForApiReadyAsync()
    {
        while (App.ApiService == null)
        {
            await Task.Delay(100);
        }

        _apiService = App.ApiService!;
    }

    private void OnSaveApiKeyClicked(object sender, RoutedEventArgs e)
    {
        var apiKeyBox = this.FindControl<TextBox>("ApiKeyBox");
        var newKey = apiKeyBox?.Text?.Trim();

        if (string.IsNullOrEmpty(newKey))
        {
            return;
        }
        
        ConfigManager.Settings.ApiKey = newKey;
        ConfigManager.Save();
        
        try
        {
            App.PromptAssistant = new PromptAssistantService(ConfigManager.Settings.ApiKey);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to initialize prompt assistant service. Error: {ex.Message}");
            return;
        }
    }

    private void RegisterBindings()
    {
        
        RegisterBoolBinding("UpcastAttentionToggle", "UpcastStatusText", "upcast_attn",
            "Now forcing 32-bit attention.",
            "Using mixed-precision attention.");

        RegisterBoolBinding("AutoVaePrecisionToggle", "AutoVaePrecisionStatusText", "auto_vae_precision",
            "Automatic float32 fallback for the VAE is enabled.",
            "VAE will remain in its original precision.");

        RegisterBoolBinding("AutoVaeBfloatToggle", "AutoVaeBfloatStatusText", "auto_vae_precision_bfloat16",
            "bfloat16 fallback will trigger before float32.",
            "bfloat16 fallback disabled; VAE jumps straight to float32 if needed.");

        RegisterBoolBinding("QuantizationToggle", "QuantizationStatusText", "enable_quantization",
            "Quantized sampler kernels enabled.",
            "Quantized sampler kernels disabled.");

        RegisterBoolBinding("CheckpointsKeepCpuToggle", "CheckpointsKeepCpuStatusText", "sd_checkpoints_keep_in_cpu",
            "Idle checkpoints will stay in system RAM.",
            "Idle checkpoints can occupy VRAM.");

        RegisterBoolBinding("CacheFp16Toggle", "CacheFp16StatusText", "cache_fp16_weight",
            "fp16 weights cached to improve FP8 quality.",
            "fp16 cache disabled to save system memory.");

        RegisterBoolBinding("PersistentCondToggle", "PersistentCondStatusText", "persistent_cond_cache",
            "Prompt conditioning stays cached between jobs.",
            "Prompt conditioning will be recomputed per job.");

        RegisterBoolBinding("BatchCondToggle", "BatchCondStatusText", "batch_cond_uncond",
            "Batching cond/uncond steps for speed.",
            "Batching disabled to lower VRAM usage.");

        RegisterBoolBinding("PadCondToggle", "PadCondStatusText", "pad_cond_uncond",
            "Prompt padding enabled for consistent shapes.",
            "Prompt padding disabled for lower VRAM usage.");

        RegisterBoolBinding("SigmaMinToggle", "SigmaMinStatusText", "s_min_uncond_all",
            "Negative guidance skip applies to every step.",
            "Negative guidance skip uses default cadence.");

        RegisterNumberBinding("CheckpointCacheSlider", "CheckpointCacheValueText", "CheckpointCacheStatusText", "sd_checkpoint_cache",
            value => $"{value:F0}",
            value => value <= 0
                ? "No checkpoints cached in RAM."
                : $"{value:F0} checkpoint slot(s) cached in RAM.",
            applyThreshold: 0.5);

        RegisterNumberBinding("MemmonSlider", "MemmonValueText", "MemmonStatusText", "memmon_poll_rate",
            value => $"{value:F0}/s",
            value => value <= 0
                ? "VRAM telemetry disabled."
                : $"Polling every {value:F0}× per second.",
            applyThreshold: 0.5);

        RegisterNumberBinding("TokenMergingSlider", "TokenMergingValueText", "TokenMergingStatusText", "token_merging_ratio",
            value => $"{value:F2}",
            value => value <= 0
                ? "Token merging disabled."
                : $"Token merging ratio set to {value:F2}.",
            applyThreshold: 0.01);
    }

    private void RegisterBoolBinding(string toggleName, string statusName, string optionKey, string enabledMessage, string disabledMessage)
    {
        var toggle = this.FindControl<ToggleSwitch>(toggleName);
        var status = this.FindControl<TextBlock>(statusName);

        if (toggle is null || status is null)
            return;

        var binding = new BoolOptionBinding(toggle, status, optionKey, enabledMessage, disabledMessage);
        _boolBindings[toggle] = binding;
        _boolBindingList.Add(binding);

        toggle.IsEnabled = false;
        status.Text = "Waiting for backend...";
        toggle.PropertyChanged += BoolToggleOnPropertyChanged;
    }

    private void RegisterNumberBinding(string sliderName, string valueName, string statusName, string optionKey, Func<double, string> valueFormatter, Func<double, string> summaryFormatter, double applyThreshold)
    {
        var slider = this.FindControl<Slider>(sliderName);
        var valueText = this.FindControl<TextBlock>(valueName);
        var status = this.FindControl<TextBlock>(statusName);

        if (slider is null || valueText is null || status is null)
            return;

        var binding = new NumberOptionBinding(slider, valueText, status, optionKey, valueFormatter, summaryFormatter, applyThreshold);
        _numberBindings[slider] = binding;
        _numberBindingList.Add(binding);

        UpdateNumberBindingValue(binding, slider.Value);
        status.Text = "Waiting for backend...";
        slider.IsEnabled = false;

        slider.PropertyChanged += NumberSliderOnPropertyChanged;
        slider.AddHandler(InputElement.PointerReleasedEvent, NumberSliderOnPointerReleased, RoutingStrategies.Bubble);
        slider.LostFocus += NumberSliderOnLostFocus;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        while (_apiService == null)
        {
            await Task.Delay(100);
        }
        
        this.AttachedToVisualTree -= OnAttachedToVisualTree;

        var boolTasks = _boolBindingList.Select(LoadBoolBindingAsync);
        var numberTasks = _numberBindingList.Select(LoadNumberBindingAsync);

        await Task.WhenAll(boolTasks.Concat(numberTasks));
    }

    private void BoolToggleOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ToggleSwitch.IsCheckedProperty || sender is not ToggleSwitch toggle)
            return;

        if (!_boolBindings.TryGetValue(toggle, out var binding))
            return;

        if (binding.IsInternalChange || binding.IsApplying)
            return;

        var desired = toggle.IsChecked ?? false;
        binding.IsApplying = true;
        _ = ApplyBoolBindingAsync(binding, desired);
    }

    private void NumberSliderOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty || sender is not Slider slider)
            return;

        if (!_numberBindings.TryGetValue(slider, out var binding))
            return;

        UpdateNumberBindingValue(binding, slider.Value);
    }

    private void NumberSliderOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (!_numberBindings.TryGetValue(slider, out var binding))
            return;

        MaybeApplyNumberBinding(binding);
    }

    private void NumberSliderOnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (!_numberBindings.TryGetValue(slider, out var binding))
            return;

        MaybeApplyNumberBinding(binding);
    }

    private void MaybeApplyNumberBinding(NumberOptionBinding binding)
    {
        if (binding.IsUpdatingFromBackend || binding.IsApplying)
            return;

        var desired = binding.Slider.Value;
        if (Math.Abs(desired - binding.LastKnownValue) < binding.ApplyThreshold)
            return;

        binding.IsApplying = true;
        _ = ApplyNumberBindingAsync(binding, desired);
    }

    private async Task LoadBoolBindingAsync(BoolOptionBinding binding)
    {
        try
        {
            binding.Status.Text = "Loading setting from backend...";
            binding.Toggle.IsEnabled = false;

            var current = await _apiService.GetBoolOptionAsync(binding.OptionKey);
            if (current.HasValue)
            {
                binding.IsInternalChange = true;
                binding.Toggle.IsChecked = current.Value;
                binding.IsInternalChange = false;
                binding.LastKnownValue = current.Value;

                binding.Status.Text = binding.GetStatusMessage(current.Value);
                binding.Toggle.IsEnabled = true;
            }
            else
            {
                binding.Status.Text = "Backend not reachable or option unsupported.";
            }
        }
        catch (Exception ex)
        {
            binding.Status.Text = $"Failed to read setting ({ex.Message}).";
        }
    }

    private async Task ApplyBoolBindingAsync(BoolOptionBinding binding, bool desiredState)
    {
        try
        {
            binding.Status.Text = "Applying setting...";
            binding.Toggle.IsEnabled = false;

            var succeeded = await _apiService.SetBoolOptionAsync(binding.OptionKey, desiredState);

            if (succeeded)
            {
                var confirmed = await _apiService.GetBoolOptionAsync(binding.OptionKey);
                if (confirmed.HasValue)
                {
                    binding.LastKnownValue = confirmed.Value;
                    binding.IsInternalChange = true;
                    binding.Toggle.IsChecked = confirmed.Value;
                    binding.IsInternalChange = false;
                    binding.Status.Text = binding.GetStatusMessage(confirmed.Value);
                }
                else
                {
                    binding.IsInternalChange = true;
                    binding.Toggle.IsChecked = binding.LastKnownValue;
                    binding.IsInternalChange = false;
                    binding.Status.Text = "Backend did not confirm the change.";
                }
            }
            else
            {
                binding.IsInternalChange = true;
                binding.Toggle.IsChecked = binding.LastKnownValue;
                binding.IsInternalChange = false;
                binding.Status.Text = "Backend rejected the update.";
            }
        }
        catch (Exception ex)
        {
            binding.IsInternalChange = true;
            binding.Toggle.IsChecked = binding.LastKnownValue;
            binding.IsInternalChange = false;
            binding.Status.Text = $"Failed to apply setting ({ex.Message}).";
        }
        finally
        {
            binding.Toggle.IsEnabled = true;
            binding.IsApplying = false;
        }
    }

    private async Task LoadNumberBindingAsync(NumberOptionBinding binding)
    {
        try
        {
            binding.StatusText.Text = "Loading setting from backend...";
            binding.Slider.IsEnabled = false;

            var current = await _apiService.GetNumberOptionAsync(binding.OptionKey);
            if (current.HasValue)
            {
                var snapped = SnapToSliderTicks(binding.Slider, current.Value);
                binding.IsUpdatingFromBackend = true;
                binding.Slider.Value = snapped;
                binding.IsUpdatingFromBackend = false;
                binding.LastKnownValue = snapped;

                UpdateNumberBindingValue(binding, snapped);
                binding.StatusText.Text = binding.SummaryFormatter(snapped);
                binding.Slider.IsEnabled = true;
            }
            else
            {
                binding.StatusText.Text = "Backend not reachable or option unsupported.";
            }
        }
        catch (Exception ex)
        {
            binding.StatusText.Text = $"Failed to read setting ({ex.Message}).";
        }
    }

    private async Task ApplyNumberBindingAsync(NumberOptionBinding binding, double desiredValue)
    {
        try
        {
            binding.StatusText.Text = "Applying setting...";
            binding.Slider.IsEnabled = false;

            var snappedDesired = SnapToSliderTicks(binding.Slider, desiredValue);
            var succeeded = await _apiService.SetNumberOptionAsync(binding.OptionKey, snappedDesired);

            if (succeeded)
            {
                var confirmed = await _apiService.GetNumberOptionAsync(binding.OptionKey);
                if (confirmed.HasValue)
                {
                    var snapped = SnapToSliderTicks(binding.Slider, confirmed.Value);
                    binding.IsUpdatingFromBackend = true;
                    binding.Slider.Value = snapped;
                    binding.IsUpdatingFromBackend = false;
                    binding.LastKnownValue = snapped;

                    UpdateNumberBindingValue(binding, snapped);
                    binding.StatusText.Text = binding.SummaryFormatter(snapped);
                }
                else
                {
                    binding.IsUpdatingFromBackend = true;
                    binding.Slider.Value = binding.LastKnownValue;
                    binding.IsUpdatingFromBackend = false;
                    UpdateNumberBindingValue(binding, binding.LastKnownValue);
                    binding.StatusText.Text = "Backend did not confirm the change.";
                }
            }
            else
            {
                binding.IsUpdatingFromBackend = true;
                binding.Slider.Value = binding.LastKnownValue;
                binding.IsUpdatingFromBackend = false;
                UpdateNumberBindingValue(binding, binding.LastKnownValue);
                binding.StatusText.Text = "Backend rejected the update.";
            }
        }
        catch (Exception ex)
        {
            binding.IsUpdatingFromBackend = true;
            binding.Slider.Value = binding.LastKnownValue;
            binding.IsUpdatingFromBackend = false;
            UpdateNumberBindingValue(binding, binding.LastKnownValue);
            binding.StatusText.Text = $"Failed to apply setting ({ex.Message}).";
        }
        finally
        {
            binding.Slider.IsEnabled = true;
            binding.IsApplying = false;
        }
    }

    private static double SnapToSliderTicks(Slider slider, double value)
    {
        var clamped = Math.Clamp(value, slider.Minimum, slider.Maximum);
        if (slider.IsSnapToTickEnabled && slider.TickFrequency > 0)
        {
            var steps = Math.Round((clamped - slider.Minimum) / slider.TickFrequency);
            clamped = slider.Minimum + steps * slider.TickFrequency;
        }

        return clamped;
    }

    private static void UpdateNumberBindingValue(NumberOptionBinding binding, double value)
    {
        binding.ValueText.Text = binding.ValueFormatter(value);
    }

    private sealed class BoolOptionBinding
    {
        public BoolOptionBinding(ToggleSwitch toggle, TextBlock status, string optionKey, string enabledMessage, string disabledMessage)
        {
            Toggle = toggle;
            Status = status;
            OptionKey = optionKey;
            EnabledMessage = enabledMessage;
            DisabledMessage = disabledMessage;
        }

        public ToggleSwitch Toggle { get; }
        public TextBlock Status { get; }
        public string OptionKey { get; }
        public string EnabledMessage { get; }
        public string DisabledMessage { get; }
        public bool IsInternalChange { get; set; }
        public bool IsApplying { get; set; }
        public bool LastKnownValue { get; set; }

        public string GetStatusMessage(bool isEnabled) => isEnabled ? EnabledMessage : DisabledMessage;
    }

    private sealed class NumberOptionBinding
    {
        public NumberOptionBinding(Slider slider, TextBlock valueText, TextBlock statusText, string optionKey, Func<double, string> valueFormatter, Func<double, string> summaryFormatter, double applyThreshold)
        {
            Slider = slider;
            ValueText = valueText;
            StatusText = statusText;
            OptionKey = optionKey;
            ValueFormatter = valueFormatter;
            SummaryFormatter = summaryFormatter;
            ApplyThreshold = applyThreshold;
        }

        public Slider Slider { get; }
        public TextBlock ValueText { get; }
        public TextBlock StatusText { get; }
        public string OptionKey { get; }
        public Func<double, string> ValueFormatter { get; }
        public Func<double, string> SummaryFormatter { get; }
        public double ApplyThreshold { get; }
        public double LastKnownValue { get; set; }
        public bool IsUpdatingFromBackend { get; set; }
        public bool IsApplying { get; set; }
    }
}