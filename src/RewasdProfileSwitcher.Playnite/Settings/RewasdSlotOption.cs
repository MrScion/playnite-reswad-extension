using System.Collections.Generic;

namespace RewasdProfileSwitcher.Playnite.Settings
{
    /// <summary>
    /// One entry in a Slot dropdown — reWASD devices have four slots
    /// (slot1..slot4) by default, so this is a fixed list rather than free
    /// text, to save the user from typing (and mistyping) reWASD's raw
    /// slot names.
    /// </summary>
    public sealed class RewasdSlotOption
    {
        public string Value { get; }
        public string Label { get; }

        public RewasdSlotOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public static readonly IReadOnlyList<RewasdSlotOption> All = new[]
        {
            new RewasdSlotOption("slot1", "Slot 1"),
            new RewasdSlotOption("slot2", "Slot 2"),
            new RewasdSlotOption("slot3", "Slot 3"),
            new RewasdSlotOption("slot4", "Slot 4"),
        };
    }
}
