using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Playnite.SDK;

namespace RewasdProfileSwitcher.Playnite.Settings
{
    /// <summary>
    /// Settings view built in code — same reason as TRLE Levels' settings
    /// views: <c>dotnet build</c> without Visual Studio installed can't
    /// compile XAML markup for net462 (no PresentationBuildTasks).
    /// </summary>
    public class RewasdSettingsView : UserControl
    {
        public RewasdSettingsView()
        {
            var tabs = new TabControl { Margin = new Thickness(20) };

            tabs.Items.Add(new TabItem
            {
                Header = ResourceProvider.GetString("LOCRewasdSettingsTabGeneral"),
                Content = Scrollable(BuildGeneralTab()),
            });

            tabs.Items.Add(new TabItem
            {
                Header = ResourceProvider.GetString("LOCRewasdSettingsTabDevices"),
                Content = Scrollable(BuildDevicesTab()),
            });

            tabs.Items.Add(new TabItem
            {
                Header = ResourceProvider.GetString("LOCRewasdSettingsTabProfiles"),
                Content = Scrollable(BuildProfilesTab()),
            });

            Content = tabs;
        }

        private static ScrollViewer Scrollable(UIElement content)
        {
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = content,
                Margin = new Thickness(0, 12, 0, 0),
            };
        }

        /// <summary>Extension name/intro, the master on/off switch, and the shared reWASD CLI tool path.</summary>
        private static StackPanel BuildGeneralTab()
        {
            var root = new StackPanel();

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsHint")));

            var enableCheck = new CheckBox { Content = ResourceProvider.GetString("LOCRewasdSettingsEnable"), Margin = new Thickness(0, 8, 0, 16) };
            enableCheck.SetBinding(ToggleButton.IsCheckedProperty,
                new Binding(nameof(RewasdSettingsViewModel.IntegrationEnabled)) { Mode = BindingMode.TwoWay });
            root.Children.Add(enableCheck);

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsInstallFolderHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsInstallFolderHint")));
            root.Children.Add(BuildFileRow(nameof(RewasdSettingsViewModel.InstallFolder), nameof(RewasdSettingsViewModel.BrowseInstallFolderCommand)));

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsCliHeader"), new Thickness(0, 12, 0, 8)));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsCliHint")));
            root.Children.Add(BuildFileRow(nameof(RewasdSettingsViewModel.CliPath), nameof(RewasdSettingsViewModel.BrowseCliCommand)));

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsProfilesFolderHeader"), new Thickness(0, 12, 0, 8)));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsProfilesFolderHint")));
            root.Children.Add(BuildFileRow(nameof(RewasdSettingsViewModel.ProfilesFolder), nameof(RewasdSettingsViewModel.BrowseProfilesFolderCommand)));

            return root;
        }

        /// <summary>Add/remove devices, and each device's own name and reWASD Device ID.</summary>
        private static StackPanel BuildDevicesTab()
        {
            var root = new StackPanel();

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsDevicesHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDevicesHint")));

            var deviceList = new ListBox { Height = 200, DisplayMemberPath = nameof(RewasdDeviceRow.DisplayName), Margin = new Thickness(0, 8, 0, 0) };
            deviceList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(RewasdSettingsViewModel.Devices)));
            deviceList.SetBinding(Selector.SelectedItemProperty,
                new Binding(nameof(RewasdSettingsViewModel.SelectedDevice)) { Mode = BindingMode.TwoWay });
            root.Children.Add(deviceList);

            var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 20) };

            var addButton = new Button { Content = ResourceProvider.GetString("LOCRewasdSettingsAddDevice"), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 8, 0) };
            addButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.AddDeviceCommand)));
            buttonsRow.Children.Add(addButton);

            var removeButton = new Button { Content = ResourceProvider.GetString("LOCRewasdSettingsRemoveDevice"), Padding = new Thickness(8, 4, 8, 4) };
            removeButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.RemoveDeviceCommand)));
            buttonsRow.Children.Add(removeButton);

            root.Children.Add(buttonsRow);

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsSelectedDeviceHeader")));
            root.Children.Add(BuildLabeledTextBox(ResourceProvider.GetString("LOCRewasdSettingsDeviceNameLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DisplayName), 140, 260));
            root.Children.Add(BuildLabeledTextBox(ResourceProvider.GetString("LOCRewasdSettingsDeviceIdLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DeviceId), 140, 260));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDeviceIdHint")));

            return root;
        }

        /// <summary>
        /// The selected device's default profile and per-library overrides.
        /// Bound directly to the ViewModel (no DataContext swap to
        /// SelectedDevice) so per-field bindings use "SelectedDevice.X"
        /// paths — that keeps the Command bindings below (which live on the
        /// ViewModel, not on the selected device row) simple, with no
        /// RelativeSource/ancestor lookups needed anywhere in this method.
        /// </summary>
        private static StackPanel BuildProfilesTab()
        {
            var root = new StackPanel();

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsProfilesHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsProfilesHint")));

            var selectedDeviceLabel = new TextBlock { FontStyle = FontStyles.Italic, Margin = new Thickness(0, 0, 0, 16) };
            selectedDeviceLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            selectedDeviceLabel.SetBinding(TextBlock.TextProperty, new Binding("SelectedDevice." + nameof(RewasdDeviceRow.DisplayName))
            {
                TargetNullValue = ResourceProvider.GetString("LOCRewasdSettingsNoDeviceSelected"),
                FallbackValue = ResourceProvider.GetString("LOCRewasdSettingsNoDeviceSelected"),
                StringFormat = ResourceProvider.GetString("LOCRewasdSettingsEditingDeviceFormat"),
            });
            root.Children.Add(selectedDeviceLabel);

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsDefaultProfileHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDefaultProfileHint")));
            root.Children.Add(BuildFileRow("SelectedDevice." + nameof(RewasdDeviceRow.DefaultProfilePath), nameof(RewasdSettingsViewModel.BrowseSelectedDeviceProfileCommand)));
            root.Children.Add(BuildLabeledSlotComboBox(ResourceProvider.GetString("LOCRewasdSettingsSlotLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DefaultProfileSlot), 140, 100));

            var testButton = new Button
            {
                Content = ResourceProvider.GetString("LOCRewasdSettingsTestDefault"),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            testButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.TestSelectedDeviceDefaultProfileCommand)));
            root.Children.Add(testButton);

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsLibraryProfilesHeader"), new Thickness(0, 20, 0, 8)));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsLibraryProfilesHint")));

            var libraryList = new ItemsControl();
            libraryList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("SelectedDevice." + nameof(RewasdDeviceRow.LibraryProfiles)));
            libraryList.ItemTemplate = BuildLibraryProfileRowTemplate();
            root.Children.Add(libraryList);

            var addLibraryButton = new Button
            {
                Content = ResourceProvider.GetString("LOCRewasdSettingsAddLibraryProfile"),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            addLibraryButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.AddLibraryProfileCommand)));
            root.Children.Add(addLibraryButton);

            return root;
        }

        /// <summary>
        /// One row: library name, profile path, slot, Test/Remove buttons.
        /// The two buttons use plain Click events instead of Command
        /// bindings (a Command bound here would need to reach back up to
        /// the ViewModel past the row's own DataContext — same tradeoff
        /// TRLE Levels' savegames grid made for its row double-click, see
        /// TrleSavegamesView.FindAncestor) — the handler reads the row off
        /// the clicked button's own DataContext, and the ViewModel off this
        /// view's DataContext (always the ViewModel, set by Playnite).
        /// </summary>
        private static DataTemplate BuildLibraryProfileRowTemplate()
        {
            var row = new FrameworkElementFactory(typeof(DockPanel));
            row.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 6));

            var removeButton = new FrameworkElementFactory(typeof(Button));
            removeButton.SetValue(ContentControl.ContentProperty, ResourceProvider.GetString("LOCRewasdSettingsRemove"));
            removeButton.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            removeButton.SetValue(Control.PaddingProperty, new Thickness(8, 2, 8, 2));
            removeButton.SetValue(DockPanel.DockProperty, Dock.Right);
            removeButton.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnRemoveLibraryProfileClick);
            row.AppendChild(removeButton);

            var testButton = new FrameworkElementFactory(typeof(Button));
            testButton.SetValue(ContentControl.ContentProperty, ResourceProvider.GetString("LOCRewasdSettingsTest"));
            testButton.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            testButton.SetValue(Control.PaddingProperty, new Thickness(8, 2, 8, 2));
            testButton.SetValue(DockPanel.DockProperty, Dock.Right);
            testButton.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnTestLibraryProfileClick);
            row.AppendChild(testButton);

            var passthroughCheck = new FrameworkElementFactory(typeof(CheckBox));
            passthroughCheck.SetValue(ContentControl.ContentProperty, ResourceProvider.GetString("LOCRewasdSettingsLibraryPassthroughLabel"));
            passthroughCheck.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
            passthroughCheck.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            passthroughCheck.SetValue(DockPanel.DockProperty, Dock.Right);
            passthroughCheck.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(RewasdLibraryProfileRow.Passthrough)) { Mode = BindingMode.TwoWay });
            row.AppendChild(passthroughCheck);

            var slotBox = new FrameworkElementFactory(typeof(ComboBox));
            slotBox.SetValue(FrameworkElement.WidthProperty, 100.0);
            slotBox.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            slotBox.SetValue(DockPanel.DockProperty, Dock.Right);
            slotBox.SetValue(ItemsControl.ItemsSourceProperty, RewasdSlotOption.All);
            slotBox.SetValue(ItemsControl.DisplayMemberPathProperty, nameof(RewasdSlotOption.Label));
            slotBox.SetValue(Selector.SelectedValuePathProperty, nameof(RewasdSlotOption.Value));
            slotBox.SetBinding(Selector.SelectedValueProperty, new Binding(nameof(RewasdLibraryProfileRow.ProfileSlot)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            slotBox.SetValue(FrameworkElement.StyleProperty, CollapsedWhenPassthroughStyle(typeof(ComboBox)));
            row.AppendChild(slotBox);

            var nameLabel = new FrameworkElementFactory(typeof(TextBlock));
            nameLabel.SetValue(FrameworkElement.WidthProperty, 140.0);
            nameLabel.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            nameLabel.SetValue(DockPanel.DockProperty, Dock.Left);
            nameLabel.SetBinding(TextBlock.TextProperty, new Binding(nameof(RewasdLibraryProfileRow.LibraryDisplayName)));
            row.AppendChild(nameLabel);

            // Profile path — only meaningful (and only shown) when this row isn't Passthrough.
            var pathBox = new FrameworkElementFactory(typeof(TextBox));
            pathBox.SetValue(TextBox.IsReadOnlyProperty, true);
            pathBox.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            pathBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RewasdLibraryProfileRow.ProfilePath)));
            pathBox.SetValue(FrameworkElement.StyleProperty, CollapsedWhenPassthroughStyle(typeof(TextBox)));
            row.AppendChild(pathBox);

            // Shown instead of the path box only when this row is Passthrough.
            var passthroughHint = new FrameworkElementFactory(typeof(TextBlock));
            passthroughHint.SetValue(TextBlock.TextProperty, ResourceProvider.GetString("LOCRewasdSettingsLibraryPassthroughHint"));
            passthroughHint.SetValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            passthroughHint.SetValue(TextBlock.OpacityProperty, 0.7);
            passthroughHint.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            passthroughHint.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            passthroughHint.SetValue(FrameworkElement.StyleProperty, VisibleOnlyWhenPassthroughStyle());
            row.AppendChild(passthroughHint);

            return new DataTemplate(typeof(RewasdLibraryProfileRow)) { VisualTree = row };
        }

        /// <summary>
        /// The row's Remove button has no declarative Command binding (see
        /// <see cref="BuildLibraryProfileRowTemplate"/> for why) — this
        /// walks up the visual tree to find the ViewModel (whichever
        /// ancestor's DataContext is one) and reads the clicked row off the
        /// button's own DataContext, same pattern as
        /// TrleSavegamesView.FindAncestor.
        /// </summary>
        private static void OnRemoveLibraryProfileClick(object sender, RoutedEventArgs e)
        {
            if (FindAncestorDataContext<RewasdSettingsViewModel>(sender as DependencyObject) is RewasdSettingsViewModel vm &&
                (sender as FrameworkElement)?.DataContext is RewasdLibraryProfileRow row)
            {
                vm.RemoveLibraryProfile(row);
            }
        }

        private static void OnTestLibraryProfileClick(object sender, RoutedEventArgs e)
        {
            if (FindAncestorDataContext<RewasdSettingsViewModel>(sender as DependencyObject) is RewasdSettingsViewModel vm &&
                (sender as FrameworkElement)?.DataContext is RewasdLibraryProfileRow row)
            {
                vm.TestLibraryProfile(row);
            }
        }

        /// <summary>
        /// A Style, for the given control type, that collapses the element
        /// when the row's Passthrough is true. Based on Playnite's current
        /// theme's implicit style for that type (looked up by the bare type
        /// as the resource key, same as WPF's own implicit-style lookup) —
        /// a plain <c>new Style(controlType)</c> with no BasedOn would
        /// override that implicit style entirely and fall back to raw
        /// default WPF chrome (wrong colors/borders), losing the rest of
        /// the settings dialog's look.
        /// </summary>
        private static Style CollapsedWhenPassthroughStyle(Type controlType)
        {
            var style = new Style(controlType, Application.Current.TryFindResource(controlType) as Style);
            var trigger = new DataTrigger
            {
                Binding = new Binding(nameof(RewasdLibraryProfileRow.Passthrough)),
                Value = true,
            };
            trigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            style.Triggers.Add(trigger);
            return style;
        }

        /// <summary>The inverse of <see cref="CollapsedWhenPassthroughStyle"/> — visible only when the row's Passthrough is true. Same implicit-style basis, see there for why.</summary>
        private static Style VisibleOnlyWhenPassthroughStyle()
        {
            var style = new Style(typeof(TextBlock), Application.Current.TryFindResource(typeof(TextBlock)) as Style);
            style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            var trigger = new DataTrigger
            {
                Binding = new Binding(nameof(RewasdLibraryProfileRow.Passthrough)),
                Value = true,
            };
            trigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
            style.Triggers.Add(trigger);
            return style;
        }

        private static T FindAncestorDataContext<T>(DependencyObject start) where T : class
        {
            var current = start;
            while (current != null)
            {
                if (current is FrameworkElement element && element.DataContext is T match)
                {
                    return match;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>Read-only path textbox + "Browse..." button, bound to a string property (by path) and a browse RelayCommand — works for either a file or a folder picker, depending on what the bound command opens.</summary>
        private static DockPanel BuildFileRow(string propertyPath, string browseCommandPropertyName)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

            var browseButton = new Button { Content = ResourceProvider.GetString("LOCRewasdSettingsBrowse"), Padding = new Thickness(8, 4, 8, 4) };
            browseButton.SetBinding(ButtonBase.CommandProperty, new Binding(browseCommandPropertyName));
            DockPanel.SetDock(browseButton, Dock.Right);
            row.Children.Add(browseButton);

            var box = new TextBox { IsReadOnly = true, Margin = new Thickness(0, 0, 8, 0) };
            box.SetBinding(TextBox.TextProperty, new Binding(propertyPath));
            row.Children.Add(box);

            return row;
        }

        /// <summary>
        /// Label + Slot dropdown row (slot1..slot4, see <see cref="RewasdSlotOption"/>),
        /// two-way bound to a string property (by path) holding reWASD's
        /// raw slot value.
        /// </summary>
        private static DockPanel BuildLabeledSlotComboBox(string labelText, string propertyPath, double labelWidth, double boxWidth)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

            var label = Hint(labelText);
            label.Margin = new Thickness(0, 0, 8, 0);
            label.Width = labelWidth;
            DockPanel.SetDock(label, Dock.Left);
            row.Children.Add(label);

            var combo = new ComboBox
            {
                Width = boxWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                ItemsSource = RewasdSlotOption.All,
                DisplayMemberPath = nameof(RewasdSlotOption.Label),
                SelectedValuePath = nameof(RewasdSlotOption.Value),
            };
            combo.SetBinding(Selector.SelectedValueProperty, new Binding(propertyPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row.Children.Add(combo);

            return row;
        }

        /// <summary>Label + free-text textbox row, two-way bound to a string property (by path).</summary>
        private static DockPanel BuildLabeledTextBox(string labelText, string propertyPath, double labelWidth, double boxWidth)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

            var label = Hint(labelText);
            label.Margin = new Thickness(0, 0, 8, 0);
            label.Width = labelWidth;
            DockPanel.SetDock(label, Dock.Left);
            row.Children.Add(label);

            var box = new TextBox { Width = boxWidth, HorizontalAlignment = HorizontalAlignment.Left };
            box.SetBinding(TextBox.TextProperty, new Binding(propertyPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row.Children.Add(box);

            return row;
        }

        private static TextBlock Header(string text, Thickness? margin = null)
        {
            var block = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Margin = margin ?? new Thickness(0, 0, 0, 8),
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return block;
        }

        private static TextBlock Hint(string text)
        {
            var block = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                Margin = new Thickness(0, 4, 0, 8),
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return block;
        }
    }
}
