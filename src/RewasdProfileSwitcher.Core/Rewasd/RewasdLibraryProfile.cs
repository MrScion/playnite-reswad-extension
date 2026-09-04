using System;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// A reWASD profile to apply on game start for games belonging to one
    /// specific Playnite library (identified by <see cref="LibraryPluginId"/>,
    /// Playnite's <c>Game.PluginId</c> — e.g. Steam, GOG, or a third-party
    /// library extension). <see cref="LibraryDisplayName"/> is a cached label
    /// for display only, never used to match a game.
    /// </summary>
    public class RewasdLibraryProfile
    {
        public Guid LibraryPluginId { get; set; }

        public string LibraryDisplayName { get; set; } = "";

        public string ProfilePath { get; set; } = "";

        public string ProfileSlot { get; set; } = "slot1";
    }
}
