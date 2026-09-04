using System.Collections.Generic;

namespace RewasdProfileSwitcher.Core.Gamepads
{
    /// <summary>
    /// Maps a USB HID vendor id to a friendly brand name, for gamepads
    /// detected by <c>GamepadDetector</c> (Playnite project — this lookup
    /// is pure data/logic so it can be unit tested without real hardware).
    /// </summary>
    public static class GamepadVendorLookup
    {
        private static readonly Dictionary<ushort, string> KnownVendors = new Dictionary<ushort, string>
        {
            { 0x045E, "Xbox" },       // Microsoft
            { 0x054C, "PlayStation" }, // Sony
            { 0x28DE, "Steam" },       // Valve
            { 0x057E, "Nintendo" },
            { 0x2DC8, "8BitDo" },
            { 0x20D6, "PowerA" },
        };

        public static string GetVendorName(ushort vendorId)
        {
            return KnownVendors.TryGetValue(vendorId, out var name) ? name : null;
        }

        /// <summary>
        /// Combines the vendor guess with the device's own reported product
        /// name into one label for the device picker. If the product name
        /// already mentions the vendor (most controllers' own strings do,
        /// e.g. "Xbox Wireless Controller"), the vendor isn't prefixed
        /// again. Falls back to just the vendor name, or a generic label,
        /// when the product name is blank (some controllers don't report
        /// one over HID).
        /// </summary>
        public static string BuildDisplayName(ushort vendorId, string productName)
        {
            var vendor = GetVendorName(vendorId);
            var trimmedProduct = (productName ?? "").Trim();

            if (string.IsNullOrEmpty(trimmedProduct))
            {
                return vendor ?? "Game controller";
            }

            if (vendor == null || trimmedProduct.IndexOf(vendor, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return trimmedProduct;
            }

            return $"{vendor} {trimmedProduct}";
        }
    }
}
