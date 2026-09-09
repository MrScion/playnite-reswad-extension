using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using RewasdProfileSwitcher.Core.Rewasd;
using RewasdProfileSwitcher.Playnite.Settings;

namespace RewasdProfileSwitcher.Playnite
{
    /// <summary>
    /// Switches the active reWASD gamepad profile(s) when any Playnite game
    /// starts or stops. Each configured device applies its own profile,
    /// either a per-library override (matched by the started game's
    /// <c>PluginId</c>) or its default — and always its default when a game
    /// closes, regardless of which library it belonged to.
    /// </summary>
    public class RewasdProfileSwitcherPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("a56345f8-1e5b-4876-8ecc-2a103fa1117a");

        public RewasdProfileSwitcherPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return new RewasdSettingsViewModel(this);
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new RewasdSettingsView();
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            var settings = LoadPluginSettings<RewasdSettings>() ?? new RewasdSettings();
            if (!settings.IntegrationEnabled)
            {
                return;
            }

            foreach (var device in settings.Devices)
            {
                var profile = RewasdProfileResolver.ResolveStartProfile(
                    args.Game.PluginId, device.DefaultProfilePath, device.DefaultProfileSlot, device.LibraryProfiles);

                try
                {
                    if (profile.IsPassthrough)
                    {
                        RewasdCliController.SetRemapState(settings.CliPath, device.DeviceId, false);
                    }
                    else
                    {
                        RewasdCliController.SetRemapState(settings.CliPath, device.DeviceId, true);
                        RewasdCliController.ApplyProfile(settings.CliPath, device.DeviceId, profile.ProfilePath, profile.ProfileSlot);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to apply reWASD profile for device '{device.DisplayName}' when starting '{args.Game.Name}'.");
                }
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            var settings = LoadPluginSettings<RewasdSettings>() ?? new RewasdSettings();
            if (!settings.IntegrationEnabled)
            {
                return;
            }

            foreach (var device in settings.Devices)
            {
                try
                {
                    // Always re-enable remap first — the game that just closed may have
                    // been a passthrough library, which left remap off for this device.
                    RewasdCliController.SetRemapState(settings.CliPath, device.DeviceId, true);
                    RewasdCliController.ApplyProfile(settings.CliPath, device.DeviceId, device.DefaultProfilePath, device.DefaultProfileSlot);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to apply reWASD default profile for device '{device.DisplayName}' when stopping '{args.Game.Name}'.");
                }
            }
        }

        /// <summary>
        /// Runs a one-off profile switch from the settings dialog's "Test"
        /// buttons, using whatever CLI path/device id are currently typed in
        /// (not necessarily saved yet) — same "test with what's on screen"
        /// pattern as the library picker's Add flow.
        /// </summary>
        internal void TestProfile(string cliPath, string deviceId, string profilePath, string profileSlot)
        {
            try
            {
                RewasdCliController.SetRemapState(cliPath, deviceId, true);
                RewasdCliController.ApplyProfile(cliPath, deviceId, profilePath, profileSlot);
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCRewasdTestOk"), "reWASD Profile Switcher");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "reWASD profile test failed.");
                PlayniteApi.Dialogs.ShowErrorMessage(
                    ResourceProvider.GetString("LOCRewasdTestErrorPrefix") + ex.Message, "reWASD Profile Switcher");
            }
        }

        /// <summary>
        /// Runs a one-off "turn remap off" from the settings dialog's Test
        /// button on a passthrough library row — releases the virtual
        /// controller so the real physical device becomes visible to
        /// Windows/Steam as-is, same effect this device gets on game start
        /// for a library configured as passthrough.
        /// </summary>
        internal void TestPassthrough(string cliPath, string deviceId)
        {
            try
            {
                RewasdCliController.SetRemapState(cliPath, deviceId, false);
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCRewasdTestPassthroughOk"), "reWASD Profile Switcher");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "reWASD passthrough test failed.");
                PlayniteApi.Dialogs.ShowErrorMessage(
                    ResourceProvider.GetString("LOCRewasdTestErrorPrefix") + ex.Message, "reWASD Profile Switcher");
            }
        }

        /// <summary>
        /// Distinct libraries currently represented in the game database,
        /// each labeled with the most common <c>Game.Source.Name</c> for
        /// that <c>PluginId</c> (falling back to the raw id when no game has
        /// a Source set). Manually-added games (<c>PluginId == Guid.Empty</c>)
        /// always get the fixed "no library" label instead — never a
        /// Source-derived one, since a manually-added game can carry any
        /// free-text Source a user chose for their own bookkeeping, and
        /// that must not rename/hide this group. Only libraries with at
        /// least one game already in the database can be picked this way —
        /// there is no other way to enumerate installed library plugins
        /// from a GenericPlugin.
        /// </summary>
        internal List<KnownLibrary> GetKnownLibraries()
        {
            return PlayniteApi.Database.Games
                .GroupBy(g => g.PluginId)
                .Select(group =>
                {
                    string label;
                    if (group.Key == Guid.Empty)
                    {
                        // Manually-added games can each carry their own free-text Source
                        // (e.g. a user labels one "GOG" for their own bookkeeping) even
                        // though none of them belong to an actual library plugin. Never
                        // let an individual game's Source rename this group — it must
                        // stay recognizable as "no library" regardless of what any single
                        // manually-added game's Source happens to be set to.
                        label = ResourceProvider.GetString("LOCRewasdNoLibraryLabel");
                    }
                    else
                    {
                        label = group
                            .Select(g => g.Source?.Name)
                            .FirstOrDefault(name => !string.IsNullOrEmpty(name));

                        if (string.IsNullOrEmpty(label))
                        {
                            label = group.Key.ToString();
                        }
                    }

                    return new KnownLibrary(group.Key, label, group.Count());
                })
                .OrderBy(l => l.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal sealed class KnownLibrary
        {
            public Guid PluginId { get; }
            public string DisplayName { get; }
            public int GameCount { get; }

            public KnownLibrary(Guid pluginId, string displayName, int gameCount)
            {
                PluginId = pluginId;
                DisplayName = displayName;
                GameCount = gameCount;
            }
        }
    }
}
