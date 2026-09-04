using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Playnite.SDK;
using RewasdProfileSwitcher.Core.Gamepads;
using RewasdProfileSwitcher.Core.Rewasd;
using RewasdProfileSwitcher.Playnite.Gamepads;

namespace RewasdProfileSwitcher.Playnite.Settings
{
    /// <summary>One entry in a device's per-library profile list — edit-time wrapper over <see cref="RewasdLibraryProfile"/>.</summary>
    public sealed class RewasdLibraryProfileRow : ObservableObject
    {
        private string _profilePath;
        private string _profileSlot;

        public Guid LibraryPluginId { get; }
        public string LibraryDisplayName { get; }

        public string ProfilePath
        {
            get => _profilePath;
            set => SetValue(ref _profilePath, value);
        }

        public string ProfileSlot
        {
            get => _profileSlot;
            set => SetValue(ref _profileSlot, value);
        }

        public RewasdLibraryProfileRow(Guid libraryPluginId, string libraryDisplayName, string profilePath, string profileSlot)
        {
            LibraryPluginId = libraryPluginId;
            LibraryDisplayName = libraryDisplayName;
            _profilePath = profilePath;
            _profileSlot = profileSlot;
        }

        public RewasdLibraryProfile ToModel()
        {
            return new RewasdLibraryProfile
            {
                LibraryPluginId = LibraryPluginId,
                LibraryDisplayName = LibraryDisplayName,
                ProfilePath = ProfilePath ?? "",
                ProfileSlot = ProfileSlot ?? "",
            };
        }
    }

    /// <summary>One managed reWASD device — edit-time wrapper over <see cref="RewasdDeviceSettings"/>.</summary>
    public sealed class RewasdDeviceRow : ObservableObject
    {
        private string _displayName;
        private string _deviceId;
        private string _defaultProfilePath;
        private string _defaultProfileSlot;

        public Guid Id { get; }

        public string DisplayName
        {
            get => _displayName;
            set => SetValue(ref _displayName, value);
        }

        public string DeviceId
        {
            get => _deviceId;
            set => SetValue(ref _deviceId, value);
        }

        public string DefaultProfilePath
        {
            get => _defaultProfilePath;
            set => SetValue(ref _defaultProfilePath, value);
        }

        public string DefaultProfileSlot
        {
            get => _defaultProfileSlot;
            set => SetValue(ref _defaultProfileSlot, value);
        }

        public ObservableCollection<RewasdLibraryProfileRow> LibraryProfiles { get; } = new ObservableCollection<RewasdLibraryProfileRow>();

        public RewasdDeviceRow(Guid id, string displayName, string deviceId, string defaultProfilePath, string defaultProfileSlot)
        {
            Id = id;
            _displayName = displayName;
            _deviceId = deviceId;
            _defaultProfilePath = defaultProfilePath;
            _defaultProfileSlot = defaultProfileSlot;
        }

        public static RewasdDeviceRow FromModel(RewasdDeviceSettings model)
        {
            var row = new RewasdDeviceRow(model.Id, model.DisplayName, model.DeviceId, model.DefaultProfilePath, model.DefaultProfileSlot);
            foreach (var profile in model.LibraryProfiles)
            {
                row.LibraryProfiles.Add(new RewasdLibraryProfileRow(profile.LibraryPluginId, profile.LibraryDisplayName, profile.ProfilePath, profile.ProfileSlot));
            }
            return row;
        }

        public RewasdDeviceSettings ToModel()
        {
            return new RewasdDeviceSettings
            {
                Id = Id,
                DisplayName = DisplayName ?? "",
                DeviceId = DeviceId ?? "",
                DefaultProfilePath = DefaultProfilePath ?? "",
                DefaultProfileSlot = DefaultProfileSlot ?? "",
                LibraryProfiles = LibraryProfiles.Select(p => p.ToModel()).ToList(),
            };
        }
    }

    public class RewasdSettingsViewModel : ObservableObject, ISettings
    {
        private readonly RewasdProfileSwitcherPlugin _plugin;
        private readonly RewasdSettings _settings;

        private bool _beforeIntegrationEnabled;
        private string _beforeCliPath;
        private List<RewasdDeviceSettings> _beforeDevices;

        private bool _integrationEnabled;
        private string _cliPath;
        private RewasdDeviceRow _selectedDevice;

        public bool IntegrationEnabled
        {
            get => _integrationEnabled;
            set => SetValue(ref _integrationEnabled, value);
        }

        public string CliPath
        {
            get => _cliPath;
            set => SetValue(ref _cliPath, value);
        }

        public ObservableCollection<RewasdDeviceRow> Devices { get; } = new ObservableCollection<RewasdDeviceRow>();

        public RewasdDeviceRow SelectedDevice
        {
            get => _selectedDevice;
            set => SetValue(ref _selectedDevice, value);
        }

        public RelayCommand BrowseCliCommand { get; }
        public RelayCommand AddDeviceCommand { get; }
        public RelayCommand RemoveDeviceCommand { get; }
        public RelayCommand BrowseSelectedDeviceProfileCommand { get; }
        public RelayCommand TestSelectedDeviceDefaultProfileCommand { get; }
        public RelayCommand AddLibraryProfileCommand { get; }

        public RewasdSettingsViewModel(RewasdProfileSwitcherPlugin plugin)
        {
            _plugin = plugin;
            _settings = plugin.LoadPluginSettings<RewasdSettings>() ?? new RewasdSettings();

            IntegrationEnabled = _settings.IntegrationEnabled;
            CliPath = _settings.CliPath;
            foreach (var device in _settings.Devices)
            {
                Devices.Add(RewasdDeviceRow.FromModel(device));
            }
            SelectedDevice = Devices.FirstOrDefault();

            BrowseCliCommand = new RelayCommand(BrowseCli);
            AddDeviceCommand = new RelayCommand(AddDevice);
            RemoveDeviceCommand = new RelayCommand(RemoveDevice, () => SelectedDevice != null);
            BrowseSelectedDeviceProfileCommand = new RelayCommand(BrowseSelectedDeviceProfile, () => SelectedDevice != null);
            TestSelectedDeviceDefaultProfileCommand = new RelayCommand(TestSelectedDeviceDefaultProfile, () => SelectedDevice != null);
            AddLibraryProfileCommand = new RelayCommand(AddLibraryProfile, () => SelectedDevice != null);
        }

        private void BrowseCli()
        {
            var file = _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdCliFilter"));
            if (!string.IsNullOrEmpty(file))
            {
                CliPath = file;
            }
        }

        /// <summary>
        /// Offers a picker of currently-connected gamepads (see
        /// <see cref="GamepadDetector"/>) to prefill the new device's name —
        /// this has nothing to do with reWASD's own Device ID, which still
        /// has to be pasted in by hand (reWASD doesn't expose it outside its
        /// own GUI). Falls back to today's blank-name device when no
        /// gamepad is detected, or when the user picks "Add manually".
        /// </summary>
        private void AddDevice()
        {
            List<DetectedGamepad> detected;
            try
            {
                detected = GamepadDetector.GetConnectedGamepads();
            }
            catch
            {
                detected = new List<DetectedGamepad>();
            }

            if (detected.Count == 0)
            {
                AddDeviceWithName(ResourceProvider.GetString("LOCRewasdNewDeviceName"));
                return;
            }

            var initialItems = detected
                .Select(g => (GenericItemOption)new GamepadPickerOption(g, GamepadVendorLookup.BuildDisplayName(g.VendorId, g.ProductName)))
                .ToList();
            initialItems.Add(new GamepadPickerOption(null, ResourceProvider.GetString("LOCRewasdAddDeviceManually")));

            var chosen = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(
                initialItems,
                query => initialItems.Where(i => i.Name.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                "",
                ResourceProvider.GetString("LOCRewasdDevicePickerCaption"));

            if (chosen is GamepadPickerOption picked)
            {
                AddDeviceWithName(picked.Gamepad != null ? picked.Name : ResourceProvider.GetString("LOCRewasdNewDeviceName"));
            }
        }

        private void AddDeviceWithName(string displayName)
        {
            var device = new RewasdDeviceRow(Guid.NewGuid(), displayName, "", "", "slot1");
            Devices.Add(device);
            SelectedDevice = device;
        }

        private void RemoveDevice()
        {
            if (SelectedDevice == null)
            {
                return;
            }

            var index = Devices.IndexOf(SelectedDevice);
            Devices.Remove(SelectedDevice);
            SelectedDevice = Devices.Count == 0 ? null : Devices[Math.Min(index, Devices.Count - 1)];
        }

        private void BrowseSelectedDeviceProfile()
        {
            if (SelectedDevice == null)
            {
                return;
            }

            var file = _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdProfileFilter"));
            if (!string.IsNullOrEmpty(file))
            {
                SelectedDevice.DefaultProfilePath = file;
            }
        }

        private void TestSelectedDeviceDefaultProfile()
        {
            if (SelectedDevice == null)
            {
                return;
            }

            _plugin.TestProfile(CliPath, SelectedDevice.DeviceId, SelectedDevice.DefaultProfilePath, SelectedDevice.DefaultProfileSlot);
        }

        /// <summary>
        /// Picks a library (from ones already represented in the game
        /// database) and a profile file, then adds or updates that library's
        /// override for the selected device.
        /// </summary>
        private void AddLibraryProfile()
        {
            if (SelectedDevice == null)
            {
                return;
            }

            var libraries = _plugin.GetKnownLibraries();
            if (libraries.Count == 0)
            {
                _plugin.PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCRewasdNoLibrariesFound"), "reWASD Profile Switcher");
                return;
            }

            var initialItems = libraries
                .Select(l => (GenericItemOption)new LibraryPickerOption(l.PluginId, l.DisplayName, $"{l.DisplayName} ({l.GameCount})"))
                .ToList();

            var chosen = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(
                initialItems,
                query => initialItems.Where(i => i.Name.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                "",
                ResourceProvider.GetString("LOCRewasdLibraryPickerCaption"));

            if (!(chosen is LibraryPickerOption picked))
            {
                return;
            }

            var file = _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdProfileFilter"));
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            var existing = SelectedDevice.LibraryProfiles.FirstOrDefault(p => p.LibraryPluginId == picked.LibraryPluginId);
            if (existing != null)
            {
                existing.ProfilePath = file;
            }
            else
            {
                SelectedDevice.LibraryProfiles.Add(new RewasdLibraryProfileRow(picked.LibraryPluginId, picked.LibraryDisplayName, file, "slot1"));
            }
        }

        /// <summary>Called from the settings view's per-row "Remove" click handler (no declarative command binding inside the row DataTemplate — see RewasdSettingsView).</summary>
        internal void RemoveLibraryProfile(RewasdLibraryProfileRow row)
        {
            SelectedDevice?.LibraryProfiles.Remove(row);
        }

        /// <summary>Called from the settings view's per-row "Test" click handler.</summary>
        internal void TestLibraryProfile(RewasdLibraryProfileRow row)
        {
            if (SelectedDevice == null || row == null)
            {
                return;
            }

            _plugin.TestProfile(CliPath, SelectedDevice.DeviceId, row.ProfilePath, row.ProfileSlot);
        }

        public void BeginEdit()
        {
            _beforeIntegrationEnabled = IntegrationEnabled;
            _beforeCliPath = CliPath;
            _beforeDevices = Devices.Select(d => d.ToModel()).ToList();
        }

        public void CancelEdit()
        {
            IntegrationEnabled = _beforeIntegrationEnabled;
            CliPath = _beforeCliPath;

            Devices.Clear();
            foreach (var device in _beforeDevices ?? new List<RewasdDeviceSettings>())
            {
                Devices.Add(RewasdDeviceRow.FromModel(device));
            }
            SelectedDevice = Devices.FirstOrDefault();
        }

        public void EndEdit()
        {
            _settings.IntegrationEnabled = IntegrationEnabled;
            _settings.CliPath = (CliPath ?? "").Trim();
            _settings.Devices = Devices.Select(d => d.ToModel()).ToList();
            _plugin.SavePluginSettings(_settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }

        /// <summary>Subclassed to carry the detected gamepad (null for the "Add manually" entry) through the picker.</summary>
        private sealed class GamepadPickerOption : GenericItemOption
        {
            public DetectedGamepad Gamepad { get; }

            public GamepadPickerOption(DetectedGamepad gamepad, string label) : base(label, "")
            {
                Gamepad = gamepad;
            }
        }

        /// <summary>Subclassed to carry the library's PluginId (and its plain, count-free label) through the picker, since <see cref="GenericItemOption"/> only has Name/Description.</summary>
        private sealed class LibraryPickerOption : GenericItemOption
        {
            public Guid LibraryPluginId { get; }
            public string LibraryDisplayName { get; }

            public LibraryPickerOption(Guid libraryPluginId, string libraryDisplayName, string pickerLabel) : base(pickerLabel, "")
            {
                LibraryPluginId = libraryPluginId;
                LibraryDisplayName = libraryDisplayName;
            }
        }
    }
}
