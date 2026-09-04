namespace RewasdProfileSwitcher.Playnite.Gamepads
{
    /// <summary>One HID gamepad/joystick currently connected, as read off the device itself — not a reWASD device, no relation to reWASD's own Device ID (see GamepadDetector for why that can't be detected).</summary>
    public sealed class DetectedGamepad
    {
        public ushort VendorId { get; }
        public ushort ProductId { get; }
        public string ProductName { get; }
        public string DevicePath { get; }

        public DetectedGamepad(ushort vendorId, ushort productId, string productName, string devicePath)
        {
            VendorId = vendorId;
            ProductId = productId;
            ProductName = productName;
            DevicePath = devicePath;
        }
    }
}
