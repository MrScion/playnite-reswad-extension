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
            var root = new StackPanel { Margin = new Thickness(20) };

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsHeader")));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsHint")));

            var enableCheck = new CheckBox { Content = ResourceProvider.GetString("LOCRewasdSettingsEnable"), Margin = new Thickness(0, 8, 0, 16) };
            enableCheck.SetBinding(ToggleButton.IsCheckedProperty,
                new Binding(nameof(RewasdSettingsViewModel.IntegrationEnabled)) { Mode = BindingMode.TwoWay });
            root.Children.Add(enableCheck);

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsCliHeader")));
            root.Children.Add(BuildFileRow(nameof(RewasdSettingsViewModel.CliPath), nameof(RewasdSettingsViewModel.BrowseCliCommand)));

            root.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsDevicesHeader"), new Thickness(0, 20, 0, 8)));
            root.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDevicesHint")));
            root.Children.Add(BuildDevicesGrid());

            Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
        }

        private static Grid BuildDevicesGrid()
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = BuildDeviceListPanel();
            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            var detailPanel = BuildDeviceDetailPanel();
            Grid.SetColumn(detailPanel, 2);
            grid.Children.Add(detailPanel);

            return grid;
        }

        private static StackPanel BuildDeviceListPanel()
        {
            var panel = new StackPanel();

            var deviceList = new ListBox { Height = 240, DisplayMemberPath = nameof(RewasdDeviceRow.DisplayName) };
            deviceList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(RewasdSettingsViewModel.Devices)));
            deviceList.SetBinding(Selector.SelectedItemProperty,
                new Binding(nameof(RewasdSettingsViewModel.SelectedDevice)) { Mode = BindingMode.TwoWay });
            panel.Children.Add(deviceList);

            var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var addButton = new Button { Content = ResourceProvider.GetString("LOCRewasdSettingsAddDevice"), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 8, 0) };
            addButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.AddDeviceCommand)));
            buttonsRow.Children.Add(addButton);

            var removeButton = new Button { Content = ResourceProvider.GetString("LOCRewasdSettingsRemoveDevice"), Padding = new Thickness(8, 4, 8, 4) };
            removeButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.RemoveDeviceCommand)));
            buttonsRow.Children.Add(removeButton);

            panel.Children.Add(buttonsRow);
            return panel;
        }

        /// <summary>
        /// Bound directly to the ViewModel (no DataContext swap to
        /// SelectedDevice) so per-field bindings use "SelectedDevice.X"
        /// paths — that keeps the Command bindings below (which live on the
        /// ViewModel, not on the selected device row) simple, with no
        /// RelativeSource/ancestor lookups needed anywhere in this method.
        /// </summary>
        private static StackPanel BuildDeviceDetailPanel()
        {
            var panel = new StackPanel();

            panel.Children.Add(BuildLabeledTextBox(ResourceProvider.GetString("LOCRewasdSettingsDeviceNameLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DisplayName), 140, 260));
            panel.Children.Add(BuildLabeledTextBox(ResourceProvider.GetString("LOCRewasdSettingsDeviceIdLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DeviceId), 140, 260));
            panel.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDeviceIdHint")));

            panel.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsDefaultProfileHeader"), new Thickness(0, 16, 0, 8)));
            panel.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsDefaultProfileHint")));
            panel.Children.Add(BuildFileRow("SelectedDevice." + nameof(RewasdDeviceRow.DefaultProfilePath), nameof(RewasdSettingsViewModel.BrowseSelectedDeviceProfileCommand)));
            panel.Children.Add(BuildLabeledTextBox(ResourceProvider.GetString("LOCRewasdSettingsSlotLabel"), "SelectedDevice." + nameof(RewasdDeviceRow.DefaultProfileSlot), 140, 100));

            var testButton = new Button
            {
                Content = ResourceProvider.GetString("LOCRewasdSettingsTestDefault"),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            testButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.TestSelectedDeviceDefaultProfileCommand)));
            panel.Children.Add(testButton);

            panel.Children.Add(Header(ResourceProvider.GetString("LOCRewasdSettingsLibraryProfilesHeader"), new Thickness(0, 20, 0, 8)));
            panel.Children.Add(Hint(ResourceProvider.GetString("LOCRewasdSettingsLibraryProfilesHint")));

            var libraryList = new ItemsControl();
            libraryList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("SelectedDevice." + nameof(RewasdDeviceRow.LibraryProfiles)));
            libraryList.ItemTemplate = BuildLibraryProfileRowTemplate();
            panel.Children.Add(libraryList);

            var addLibraryButton = new Button
            {
                Content = ResourceProvider.GetString("LOCRewasdSettingsAddLibraryProfile"),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            addLibraryButton.SetBinding(ButtonBase.CommandProperty, new Binding(nameof(RewasdSettingsViewModel.AddLibraryProfileCommand)));
            panel.Children.Add(addLibraryButton);

            return panel;
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

            var slotBox = new FrameworkElementFactory(typeof(TextBox));
            slotBox.SetValue(FrameworkElement.WidthProperty, 80.0);
            slotBox.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            slotBox.SetValue(DockPanel.DockProperty, Dock.Right);
            slotBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RewasdLibraryProfileRow.ProfileSlot)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row.AppendChild(slotBox);

            var nameLabel = new FrameworkElementFactory(typeof(TextBlock));
            nameLabel.SetValue(FrameworkElement.WidthProperty, 140.0);
            nameLabel.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            nameLabel.SetValue(DockPanel.DockProperty, Dock.Left);
            nameLabel.SetBinding(TextBlock.TextProperty, new Binding(nameof(RewasdLibraryProfileRow.LibraryDisplayName)));
            row.AppendChild(nameLabel);

            var pathBox = new FrameworkElementFactory(typeof(TextBox));
            pathBox.SetValue(TextBox.IsReadOnlyProperty, true);
            pathBox.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            pathBox.SetBinding(TextBox.TextProperty, new Binding(nameof(RewasdLibraryProfileRow.ProfilePath)));
            row.AppendChild(pathBox);

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

        /// <summary>Read-only path textbox + "Browse..." button, bound to a string property (by path) and a browse RelayCommand.</summary>
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
