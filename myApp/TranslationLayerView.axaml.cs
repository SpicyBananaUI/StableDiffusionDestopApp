using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using myApp.Services;

namespace myApp
{
    public partial class TranslationLayerView : Window
    {
        private readonly TranslationLayerService _service = new();
        private readonly HashSet<string> _renderedComponents = new();

        public TranslationLayerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await LoadComponentTreeAsync();
        }

        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            await LoadComponentTreeAsync();
        }

        private async Task LoadComponentTreeAsync()
        {
            if (!App.AppConfig.EnableTranslationLayer)
            {
                StatusText.Text = "";
                DisabledMessageText.Text = "Translation Layer is disabled in startup settings.";
                DisabledMessageText.IsVisible = true;
                ComponentsContainer.Children.Clear();
                return;
            }

            try
            {
                StatusText.Text = "Loading...";
                var treeResponse = await _service.GetComponentTreeAsync();

                if (!treeResponse.Active)
                {
                    StatusText.Text = treeResponse.Message ?? "Translation layer not active";
                    return;
                }

                // Clear existing components
                ComponentsContainer.Children.Clear();
                _renderedComponents.Clear();

                var extensions = treeResponse.Tree.Extensions;
                Console.WriteLine($"Component tree has {extensions.Count} extensions");

                if (extensions.Count == 0)
                {
                    StatusText.Text = "No extension components found.";
                    Console.WriteLine("WARNING: No extensions in component tree!");
                    return;
                }

                // Count total components
                int totalComponents = extensions.Values.Sum(ext => ext.component_count);
                StatusText.Text = $"Loaded {extensions.Count} extensions with {totalComponents} components";

                // Sort extensions alphabetically
                var sortedExtensions = extensions.OrderBy(kvp => kvp.Key);

                // Render each extension in its own expander
                foreach (var (extName, extTree) in sortedExtensions)
                {
                    Console.WriteLine($"Rendering extension: {extName} with {extTree.component_count} components");

                    // Create expander for this extension
                    var expander = new Expander
                    {
                        Margin = new Avalonia.Thickness(0, 5, 0, 5),
                        Background = Avalonia.Media.Brushes.DarkSlateGray,
                        BorderBrush = Avalonia.Media.Brushes.Gray,
                        BorderThickness = new Avalonia.Thickness(1),
                        Padding = new Avalonia.Thickness(10)
                    };

                    // Create header with compatibility status
                    string statusIcon = extTree.Supported == true ? "✓" : 
                                       extTree.Supported == false ? "✗" : "⚠";
                    string statusText = extTree.Supported == true ? "Supported" :
                                       extTree.Supported == false ? "Unsupported" : "Partially Supported";
                    
                    expander.Header = $"{statusIcon} {extName} ({statusText} - {extTree.component_count} components)";

                    // Create panel for extension components
                    var extPanel = new StackPanel 
                    { 
                        Orientation = Orientation.Vertical, 
                        Spacing = 10,
                        Margin = new Avalonia.Thickness(10, 5, 0, 0)
                    };

                    // Render root nodes for this extension
                    foreach (var rootId in extTree.root_nodes)
                    {
                        if (extTree.Components.TryGetValue(rootId, out var rootNode))
                        {
                            var control = RenderComponent(rootNode, extPanel, extTree.Components);
                            if (control != null)
                            {
                                extPanel.Children.Add(control);
                            }
                        }
                    }

                    expander.Content = extPanel;
                    ComponentsContainer.Children.Add(expander);
                }

                Console.WriteLine($"Final container has {ComponentsContainer.Children.Count} extension expanders");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                Console.WriteLine($"Error loading component tree: {ex}");
            }
        }

        private Control? RenderComponent(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            if (_renderedComponents.Contains(node.Id))
            {
                Console.WriteLine($"Skipping already rendered component: {node.Id} ({node.Type})");
                return null;
            }
            
            Console.WriteLine($"Rendering component: {node.Id} ({node.Type})");
            _renderedComponents.Add(node.Id);

            var control = node.Type switch
            {
                "blocks" => RenderBlocks(node, parentPanel, allComponents),
                "row" => RenderRow(node, parentPanel, allComponents),
                "column" => RenderColumn(node, parentPanel, allComponents),
                "group" => RenderGroup(node, parentPanel, allComponents),
                "accordion" => RenderAccordion(node, parentPanel, allComponents),
                "button" => RenderButton(node, parentPanel, allComponents),
                "textbox" => RenderTextBox(node, parentPanel, allComponents),
                "slider" => RenderSlider(node, parentPanel, allComponents),
                "checkbox" => RenderCheckbox(node, parentPanel, allComponents),
                "dropdown" => RenderDropdown(node, parentPanel, allComponents),
                "number" => RenderNumber(node, parentPanel, allComponents),
                "inputaccordionimpl" => RenderInputAccordionImpl(node, parentPanel, allComponents),
                _ => RenderPlaceholder(node, parentPanel, allComponents)
            };

            return control;
        }

        private Control RenderBlocks(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
            RenderChildren(node, panel, allComponents);
            return panel;
        }

        private Control RenderRow(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            RenderChildren(node, panel, allComponents);
            return panel;
        }

        private Control RenderColumn(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
            RenderChildren(node, panel, allComponents);
            return panel;
        }

        private Control RenderGroup(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var border = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.Gray,
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(10),
                CornerRadius = new Avalonia.CornerRadius(4)
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
            RenderChildren(node, panel, allComponents);
            border.Child = panel;

            return border;
        }

        private Control RenderAccordion(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Accordion";
            var isOpen = GetPropBool(node, "open") ?? false;

            var expander = new Expander
            {
                Header = label,
                IsExpanded = isOpen
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
            RenderChildren(node, panel, allComponents);
            expander.Content = panel;

            return expander;
        }

        private Control RenderInputAccordionImpl(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            // InputAccordionImpl is essentially a checkbox that controls visibility of content, often styled as an accordion.
            // In the backend python code, it inherits from Checkbox but acts as a container.
            // However, the python implementation creates a separate 'accordion' component internally.
            // If we see 'inputaccordionimpl', it's the checkbox part.
            
            var label = GetPropString(node, "label") ?? "Enable";
            var value = GetPropBool(node, "value") ?? false;

            var checkBox = new CheckBox
            {
                Content = label,
                IsChecked = value
            };

            var nodeId = node.Id;
            checkBox.IsCheckedChanged += async (s, e) =>
            {
                await _service.SetComponentValueAsync(nodeId, checkBox.IsChecked ?? false);
            };

            // It might have children if the interceptor captured them under this node, 
            // though usually the python side splits them into a separate accordion component.
            // If there are children, we render them in a panel that toggles visibility.
            if (node.Children.Count > 0)
            {
                var container = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
                container.Children.Add(checkBox);

                var contentPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10, Margin = new Avalonia.Thickness(20, 0, 0, 0) };
                contentPanel.IsVisible = value; // Initial visibility based on checkbox
                
                RenderChildren(node, contentPanel, allComponents);
                container.Children.Add(contentPanel);

                checkBox.IsCheckedChanged += (s, e) =>
                {
                    contentPanel.IsVisible = checkBox.IsChecked ?? false;
                };

                return container;
            }

            return checkBox;
        }

        private Control RenderButton(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Button";
            var button = new Button { Content = label };

            var nodeId = node.Id;
            button.Click += async (s, e) =>
            {
                await _service.TriggerComponentEventAsync(nodeId, "click");
            };

            RenderChildren(node, parentPanel, allComponents); // just in case a button has children
            return button;
        }

        private Control RenderTextBox(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label");
            var placeholder = GetPropString(node, "placeholder");
            var value = GetPropString(node, "value");
            var lines = GetPropInt(node, "lines") ?? 1;

            if (lines > 1)
            {
                var textBox = new TextBox
                {
                    Text = value ?? "",
                    Watermark = placeholder,
                    AcceptsReturn = true,
                    MinHeight = 100
                };

                var nodeId = node.Id;
                textBox.TextChanged += async (s, e) =>
                {
                    if (textBox.Text != null)
                    {
                        await _service.SetComponentValueAsync(nodeId, textBox.Text);
                    }
                };

                if (!string.IsNullOrEmpty(label))
                {
                    var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
                    panel.Children.Add(new TextBlock { Text = label });
                    panel.Children.Add(textBox);
                    return panel;
                }

                return textBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = value ?? "",
                    Watermark = placeholder
                };

                var nodeId = node.Id;
                textBox.TextChanged += async (s, e) =>
                {
                    if (textBox.Text != null)
                    {
                        await _service.SetComponentValueAsync(nodeId, textBox.Text);
                    }
                };

                if (!string.IsNullOrEmpty(label))
                {
                    var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
                    panel.Children.Add(new TextBlock { Text = label });
                    panel.Children.Add(textBox);
                    return panel;
                }

                RenderChildren(node, parentPanel, allComponents); // just in case a textbox has children
                return textBox;
            }
        }

        private Control RenderSlider(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Slider";
            var min = GetPropDouble(node, "minimum") ?? 0;
            var max = GetPropDouble(node, "maximum") ?? 100;
            var step = GetPropDouble(node, "step") ?? 1;
            var value = GetPropDouble(node, "value") ?? min;

            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                TickFrequency = step,
                IsSnapToTickEnabled = true
            };

            var nodeId = node.Id;
            slider.ValueChanged += async (s, e) =>
            {
                await _service.SetComponentValueAsync(nodeId, e.NewValue);
            };

            var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
            panel.Children.Add(new TextBlock { Text = $"{label}: {value:F2}" });
            panel.Children.Add(slider);

            // Update label when value changes
            slider.ValueChanged += (s, e) =>
            {
                if (panel.Children[0] is TextBlock labelText)
                {
                    labelText.Text = $"{label}: {e.NewValue:F2}";
                }
            };

            return panel;
        }

        private Control RenderCheckbox(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Checkbox";
            var value = GetPropBool(node, "value") ?? false;

            var checkBox = new CheckBox
            {
                Content = label,
                IsChecked = value
            };

            var nodeId = node.Id;
            checkBox.IsCheckedChanged += async (s, e) =>
            {
                await _service.SetComponentValueAsync(nodeId, checkBox.IsChecked ?? false);
            };

            RenderChildren(node, parentPanel, allComponents); // just in case a checkbox has children
            return checkBox;
        }

        private Control RenderDropdown(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Dropdown";
            
            // Extract choices from props
            var choices = new System.Collections.Generic.List<string>();
            if (node.Props.TryGetValue("choices", out var choicesElement))
            {
                if (choicesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var choice in choicesElement.EnumerateArray())
                    {
                        if (choice.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            choices.Add(choice.GetString() ?? "");
                        }
                    }
                }
            }

            var comboBox = new ComboBox
            {
                ItemsSource = choices,
                SelectedIndex = 0
            };

            var nodeId = node.Id;
            comboBox.SelectionChanged += async (s, e) =>
            {
                if (comboBox.SelectedItem is string selected)
                {
                    await _service.SetComponentValueAsync(nodeId, selected);
                }
            };

            if (!string.IsNullOrEmpty(label))
            {
                var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
                panel.Children.Add(new TextBlock { Text = label });
                panel.Children.Add(comboBox);
                return panel;
            }

            RenderChildren(node, parentPanel, allComponents); // just in case a dropdown has children
            return comboBox;
        }

        private Control RenderNumber(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var label = GetPropString(node, "label") ?? "Number";
            var value = GetPropDouble(node, "value") ?? 0;

            var numericUpDown = new NumericUpDown
            {
                Value = (decimal)value,
                Minimum = decimal.MinValue,
                Maximum = decimal.MaxValue
            };

            var nodeId = node.Id;
            numericUpDown.ValueChanged += async (s, e) =>
            {
                if (e.NewValue.HasValue)
                {
                    await _service.SetComponentValueAsync(nodeId, e.NewValue.Value);
                }
            };

            if (!string.IsNullOrEmpty(label))
            {
                var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
                panel.Children.Add(new TextBlock { Text = label });
                panel.Children.Add(numericUpDown);
                return panel;
            }

            RenderChildren(node, parentPanel, allComponents); // just in case an updown has children
            return numericUpDown;
        }

        private Control RenderPlaceholder(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            var border = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.Orange,
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(10),
                CornerRadius = new Avalonia.CornerRadius(4)
            };

            var text = new TextBlock
            {
                Text = $"[{node.Type} - Not Implemented]",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            if (node.Children.Count > 0)
            {
                var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10 };
                panel.Children.Add(text);
                RenderChildren(node, panel, allComponents);
                border.Child = panel;
            }
            else
            {
                border.Child = text;
            }
            RenderChildren(node, parentPanel, allComponents); // just in case an unrenderable (unsupported) has renderable children
            return border;
        }

        private void RenderChildren(
            TranslationLayerService.ComponentNode node,
            Panel parentPanel,
            System.Collections.Generic.Dictionary<string, TranslationLayerService.ComponentNode> allComponents)
        {
            // Use a set to track which children we've already rendered to avoid duplicates
            var renderedChildIds = new HashSet<string>();
            
            foreach (var childId in node.Children)
            {
                // Skip if we've already rendered this child (duplicate in children list)
                if (renderedChildIds.Contains(childId))
                {
                    Console.WriteLine($"Skipping duplicate child {childId}");
                    continue;
                }
                renderedChildIds.Add(childId);
                
                if (allComponents.TryGetValue(childId, out var childNode))
                {
                    var control = RenderComponent(childNode, parentPanel, allComponents);
                    if (control != null)
                    {
                        Console.WriteLine($"Rendering {childNode.Type} ({childNode.Id})");
                        parentPanel.Children.Add(control);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to render {childNode.Type} ({childNode.Id})");
                    }
                }
            }
        }

        // Helper methods to extract props
        private string? GetPropString(TranslationLayerService.ComponentNode node, string key)
        {
            if (node.Props.TryGetValue(key, out var element) && element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return element.GetString();
            }
            return null;
        }

        private bool? GetPropBool(TranslationLayerService.ComponentNode node, string key)
        {
            if (node.Props.TryGetValue(key, out var element) && element.ValueKind == System.Text.Json.JsonValueKind.True)
            {
                return true;
            }
            if (element.ValueKind == System.Text.Json.JsonValueKind.False)
            {
                return false;
            }
            return null;
        }

        private int? GetPropInt(TranslationLayerService.ComponentNode node, string key)
        {
            if (node.Props.TryGetValue(key, out var element) && element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return element.GetInt32();
            }
            return null;
        }

        private double? GetPropDouble(TranslationLayerService.ComponentNode node, string key)
        {
            if (node.Props.TryGetValue(key, out var element) && element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return element.GetDouble();
            }
            return null;
        }
    }
}

