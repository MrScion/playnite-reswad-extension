using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Playnite.SDK;
using RewasdProfileSwitcher.Core.Gamepads;
using RewasdProfileSwitcher.Core.Rewasd;
using RewasdProfileSwitcher.Playnite.Gamepads;
using RewasdProfileSwitcher.Playnite.Rewasd;

namespace RewasdProfileSwitcher.Playnite.Settings
{
    /// <summary>One entry in a device's per-library profile list — edit-time wrapper over <see cref="RewasdLibraryProfile"/>.</summary>
    public sealed class RewasdLibraryProfileRow : ObservableObject
    {
        private string _profilePath;
        private string _profileSlot;
        private bool _passthrough;

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

        /// <summary>See <see cref="RewasdLibraryProfile.Passthrough"/>.</summary>
        public bool Passthrough
        {
            get => _passthrough;
            set => SetValue(ref _passthrough, value);
        }

        public RewasdLibraryProfileRow(Guid libraryPluginId, string libraryDisplayName, string profilePath, string profileSlot, bool passthrough = false)
        {
            LibraryPluginId = libraryPluginId;
            LibraryDisplayName = libraryDisplayName;
            _profilePath = profilePath;
            _profileSlot = profileSlot;
            _passthrough = passthrough;
        }

        public RewasdLibraryProfile ToModel()
        {
            return new RewasdLibraryProfile
            {
                LibraryPluginId = LibraryPluginId,
                LibraryDisplayName = LibraryDisplayName,
                ProfilePath = ProfilePath ?? "",
                ProfileSlot = ProfileSlot ?? "",
                Passthrough = Passthrough,
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
                row.LibraryProfiles.Add(new RewasdLibraryProfileRow(profile.LibraryPluginId, profile.LibraryDisplayName, profile.ProfilePath, profile.ProfileSlot, profile.Passthrough));
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
        private string _beforeInstallFolder;
        private string _beforeCliPath;
        private string _beforeProfilesFolder;
        private List<RewasdDeviceSettings> _beforeDevices;

        private bool _integrationEnabled;
        private string _installFolder;
        private string _cliPath;
        private string _profilesFolder;
        private RewasdDeviceRow _selectedDevice;

        public bool IntegrationEnabled
        {
            get => _integrationEnabled;
            set => SetValue(ref _integrationEnabled, value);
        }

        /// <summary>reWASD's install folder — only used to auto-detect <see cref="CliPath"/> below (see <see cref="TryAutoDetectCliPath"/>), never read by the CLI controller itself.</summary>
        public string InstallFolder
        {
            get => _installFolder;
            set => SetValue(ref _installFolder, value);
        }

        public string CliPath
        {
            get => _cliPath;
            set => SetValue(ref _cliPath, value);
        }

        /// <summary>
        /// Folder holding reWASD's <c>Profiles\&lt;name&gt;\Controller\*.rewasd</c>
        /// layout — used only to offer a picker of found .rewasd files (see
        /// <see cref="PickProfileFile"/>) instead of a plain file browser.
        /// </summary>
        public string ProfilesFolder
        {
            get => _profilesFolder;
            set => SetValue(ref _profilesFolder, value);
        }

        public ObservableCollection<RewasdDeviceRow> Devices { get; } = new ObservableCollection<RewasdDeviceRow>();

        public RewasdDeviceRow SelectedDevice
        {
            get => _selectedDevice;
            set => SetValue(ref _selectedDevice, value);
        }

        public RelayCommand BrowseInstallFolderCommand { get; }
        public RelayCommand BrowseCliCommand { get; }
        public RelayCommand BrowseProfilesFolderCommand { get; }
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
            InstallFolder = _settings.InstallFolder;
            CliPath = _settings.CliPath;
            ProfilesFolder = _settings.ProfilesFolder;
            foreach (var device in _settings.Devices)
            {
                Devices.Add(RewasdDeviceRow.FromModel(device));
            }
            SelectedDevice = Devices.FirstOrDefault();

            // Only on first load, and only if there's no CliPath yet (e.g. a
            // brand-new install, or an existing settings file saved before
            // InstallFolder existed) — an explicit Browse afterwards (see
            // BrowseInstallFolder) always overwrites, since that's a
            // deliberate user action.
            if (string.IsNullOrEmpty(CliPath))
            {
                TryAutoDetectCliPath();
            }

            BrowseInstallFolderCommand = new RelayCommand(BrowseInstallFolder);
            BrowseCliCommand = new RelayCommand(BrowseCli);
            BrowseProfilesFolderCommand = new RelayCommand(BrowseProfilesFolder);
            AddDeviceCommand = new RelayCommand(AddDevice);
            RemoveDeviceCommand = new RelayCommand(RemoveDevice, () => SelectedDevice != null);
            BrowseSelectedDeviceProfileCommand = new RelayCommand(BrowseSelectedDeviceProfile, () => SelectedDevice != null);
            TestSelectedDeviceDefaultProfileCommand = new RelayCommand(TestSelectedDeviceDefaultProfile, () => SelectedDevice != null);
            AddLibraryProfileCommand = new RelayCommand(AddLibraryProfile, () => SelectedDevice != null);
        }

        private void BrowseInstallFolder()
        {
            var folder = _plugin.PlayniteApi.Dialogs.SelectFolder(InstallFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                InstallFolder = folder;
                TryAutoDetectCliPath();
            }
        }

        /// <summary>
        /// Looks for reWASDCommandLine.exe under <see cref="InstallFolder"/>
        /// (reWASD's own CLI has no "where am I installed" command to ask
        /// instead — see CLAUDE.md's "Gamepad auto-detect" section for the
        /// same limitation on Device ID). Searches subfolders too since the
        /// exact layout isn't guaranteed, just "usually" the folder root.
        /// Silently does nothing if the folder doesn't exist or nothing is
        /// found — never blocks picking a folder or clears an existing
        /// CliPath.
        /// </summary>
        private void TryAutoDetectCliPath()
        {
            if (string.IsNullOrEmpty(InstallFolder) || !Directory.Exists(InstallFolder))
            {
                return;
            }

            try
            {
                var found = Directory.GetFiles(InstallFolder, "reWASDCommandLine.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(found))
                {
                    CliPath = found;
                }
            }
            catch
            {
                // Best-effort only — e.g. access denied on a subfolder. Leave CliPath as-is.
            }
        }

        private void BrowseCli()
        {
            var file = _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdCliFilter"));
            if (!string.IsNullOrEmpty(file))
            {
                CliPath = file;
            }
        }

        private void BrowseProfilesFolder()
        {
            var folder = _plugin.PlayniteApi.Dialogs.SelectFolder(ProfilesFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                ProfilesFolder = folder;
            }
        }

        /// <summary>
        /// Offers a picker of .rewasd files found under
        /// <see cref="ProfilesFolder"/> (see <see cref="RewasdProfileFileScanner"/>
        /// for the on-disk layout this expects), with a "browse manually"
        /// fallback entry — same picker-plus-manual-fallback shape as
        /// <see cref="AddDevice"/>'s gamepad picker. Falls straight back to
        /// a plain file browser (today's behavior) when no profiles folder
        /// is configured, or nothing was found in it. Returns null if the
        /// user cancelled.
        /// </summary>
        private string PickProfileFile()
        {
            List<RewasdProfileFile> found;
            try
            {
                found = RewasdProfileFileScanner.Scan(ProfilesFolder);
            }
            catch
            {
                found = new List<RewasdProfileFile>();
            }

            if (found.Count == 0)
            {
                return _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdProfileFilter"));
            }

            var items = found
                .Select(f => (GenericItemOption)new ProfileFilePickerOption(f.FilePath, f.DisplayName))
                .ToList();
            items.Add(new ProfileFilePickerOption(null, ResourceProvider.GetString("LOCRewasdProfileFileBrowseManually")));

            var chosen = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(
                items,
                query => items.Where(i => i.Name.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                "",
                ResourceProvider.GetString("LOCRewasdProfileFilePickerCaption"));

            if (!(chosen is ProfileFilePickerOption picked))
            {
                return null;
            }

            return picked.FilePath ?? _plugin.PlayniteApi.Dialogs.SelectFile(ResourceProvider.GetString("LOCRewasdProfileFilter"));
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

            var file = PickProfileFile();
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
        /// database), then whether it should get a profile file or
        /// passthrough (remap off — real device visible as-is, e.g. for
        /// Steam Input to see an actual Steam Controller instead of
        /// reWASD's virtual Xbox 360 pad), then adds or updates that
        /// library's override for the selected device.
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

            var libraryItems = libraries
                .Select(l => (GenericItemOption)new LibraryPickerOption(l.PluginId, l.DisplayName, $"{l.DisplayName} ({l.GameCount})"))
                .ToList();

            var chosenLibrary = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(
                libraryItems,
                query => libraryItems.Where(i => i.Name.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                "",
                ResourceProvider.GetString("LOCRewasdLibraryPickerCaption"));

            if (!(chosenLibrary is LibraryPickerOption picked))
            {
                return;
            }

            var modeItems = new List<GenericItemOption>
            {
                new LibraryModeOption(false, ResourceProvider.GetString("LOCRewasdLibraryModeProfile")),
                new LibraryModeOption(true, ResourceProvider.GetString("LOCRewasdLibraryModePassthrough")),
            };

            var chosenMode = _plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(
                modeItems,
                query => modeItems,
                "",
                ResourceProvider.GetString("LOCRewasdLibraryModePickerCaption"));

            if (!(chosenMode is LibraryModeOption mode))
            {
                return;
            }

            if (mode.Passthrough)
            {
                UpsertLibraryProfile(picked, "", "", true);
                return;
            }

            var file = PickProfileFile();
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            UpsertLibraryProfile(picked, file, "slot1", false);
        }

        private void UpsertLibraryProfile(LibraryPickerOption picked, string profilePath, string profileSlot, bool passthrough)
        {
            var existing = SelectedDevice.LibraryProfiles.FirstOrDefault(p => p.LibraryPluginId == picked.LibraryPluginId);
            if (existing != null)
            {
                existing.ProfilePath = profilePath;
                existing.ProfileSlot = profileSlot;
                existing.Passthrough = passthrough;
            }
            else
            {
                SelectedDevice.LibraryProfiles.Add(new RewasdLibraryProfileRow(picked.LibraryPluginId, picked.LibraryDisplayName, profilePath, profileSlot, passthrough));
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

            if (row.Passthrough)
            {
                _plugin.TestPassthrough(CliPath, SelectedDevice.DeviceId);
            }
            else
            {
                _plugin.TestProfile(CliPath, SelectedDevice.DeviceId, row.ProfilePath, row.ProfileSlot);
            }
        }

        public void BeginEdit()
        {
            _beforeIntegrationEnabled = IntegrationEnabled;
            _beforeInstallFolder = InstallFolder;
            _beforeCliPath = CliPath;
            _beforeProfilesFolder = ProfilesFolder;
            _beforeDevices = Devices.Select(d => d.ToModel()).ToList();
        }

        public void CancelEdit()
        {
            IntegrationEnabled = _beforeIntegrationEnabled;
            InstallFolder = _beforeInstallFolder;
            CliPath = _beforeCliPath;
            ProfilesFolder = _beforeProfilesFolder;

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
            _settings.InstallFolder = (InstallFolder ?? "").Trim();
            _settings.CliPath = (CliPath ?? "").Trim();
            _settings.ProfilesFolder = (ProfilesFolder ?? "").Trim();
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

        /// <summary>Subclassed to carry whether "Passthrough" was picked through the second AddLibraryProfile picker.</summary>
        private sealed class LibraryModeOption : GenericItemOption
        {
            public bool Passthrough { get; }

            public LibraryModeOption(bool passthrough, string label) : base(label, "")
            {
                Passthrough = passthrough;
            }
        }

        /// <summary>Subclassed to carry the found .rewasd file's path (null for the "browse manually" entry) through <see cref="PickProfileFile"/>.</summary>
        private sealed class ProfileFilePickerOption : GenericItemOption
        {
            public string FilePath { get; }

            public ProfileFilePickerOption(string filePath, string label) : base(label, "")
            {
                FilePath = filePath;
            }
        }
    }
}
