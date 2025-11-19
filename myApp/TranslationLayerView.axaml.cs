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

                StatusText.Text = $"Loaded {treeResponse.Tree.Components.Count} components";

                // Clear existing components
                ComponentsContainer.Children.Clear();
                _renderedComponents.Clear();

                Console.WriteLine($"Component tree has {treeResponse.Tree.Components.Count} total components");
                Console.WriteLine($"Component tree has {treeResponse.Tree.root_nodes.Count} root nodes");
                
                if (treeResponse.Tree.root_nodes.Count == 0)
                {
                    StatusText.Text = "No root nodes found. Components may not have been registered from extensions.";
                    Console.WriteLine("WARNING: No root nodes in component tree!");
                    return;
                }
                
                // Render root nodes
                foreach (var rootId in treeResponse.Tree.root_nodes)
                {
                    if (treeResponse.Tree.Components.TryGetValue(rootId, out var rootNode))
                    {
                        Console.WriteLine($"Root node {rootNode.Id} ({rootNode.Type}) has {rootNode.Children.Count} children");

                        var control = RenderComponent(rootNode, ComponentsContainer, treeResponse.Tree.Components);
                        if (control != null)
                        {
                            ComponentsContainer.Children.Add(control);
                            Console.WriteLine($"Added root control {rootNode.Type} to container");
                        }
                        else
                        {
                            Console.WriteLine($"Root control {rootNode.Type} returned null - not added");
                        }
                    }
                    else{
                        Console.WriteLine($"Root node {rootId} not found in tree");
                    }
                }
                
                Console.WriteLine($"Final container has {ComponentsContainer.Children.Count} child controls");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
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

