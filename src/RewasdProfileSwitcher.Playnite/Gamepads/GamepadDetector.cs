using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Playnite.SDK;

namespace RewasdProfileSwitcher.Playnite.Gamepads
{
    /// <summary>
    /// Enumerates currently-connected HID game controllers (Xbox, PS5/
    /// DualSense, Steam Controller, and any other joystick/gamepad — filtered
    /// by HID usage, not a hardcoded vendor list) via raw SetupAPI/hid.dll
    /// P/Invoke. Used only to prefill a friendly name in the "Add device"
    /// picker — this has nothing to do with reWASD's own Device ID, which
    /// reWASD does not expose outside its own GUI ("Copy device ID"); see
    /// reWASDCommandLine.exe's documented commands (apply/select_slot/
    /// clear_slot/remap/version/help — no device-listing command exists).
    ///
    /// Never throws: enumeration failures, and failures reading any single
    /// device, are swallowed and logged — a detection hiccup must not block
    /// adding a device by hand.
    /// </summary>
    public static class GamepadDetector
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private const int DIGCF_PRESENT = 0x02;
        private const int DIGCF_DEVICEINTERFACE = 0x10;
        private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
        private const ushort HID_USAGE_JOYSTICK = 0x04;
        private const ushort HID_USAGE_GAMEPAD = 0x05;

        public static List<DetectedGamepad> GetConnectedGamepads()
        {
            var results = new List<DetectedGamepad>();

            try
            {
                HidD_GetHidGuid(out var hidGuid);
                var deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
                {
                    return results;
                }

                try
                {
                    var index = 0;
                    while (true)
                    {
                        var interfaceData = new SP_DEVICE_INTERFACE_DATA();
                        interfaceData.cbSize = Marshal.SizeOf(interfaceData);
                        if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                        {
                            break;
                        }
                        index++;

                        try
                        {
                            var gamepad = TryReadGamepad(deviceInfoSet, ref interfaceData);
                            if (gamepad != null)
                            {
                                results.Add(gamepad);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug(ex, "Skipped one HID device while detecting gamepads.");
                        }
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Gamepad detection failed.");
                return new List<DetectedGamepad>();
            }

            // A single physical controller can expose more than one HID
            // top-level collection with a gamepad/joystick usage (e.g. a
            // separate one for haptics/touchpad reporting) — keep one entry
            // per distinct (VendorId, ProductId).
            return results
                .GroupBy(g => new { g.VendorId, g.ProductId })
                .Select(group => group.OrderByDescending(g => !string.IsNullOrEmpty(g.ProductName)).First())
                .ToList();
        }

        private static DetectedGamepad TryReadGamepad(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA interfaceData)
        {
            var devicePath = GetDevicePath(deviceInfoSet, ref interfaceData);
            if (string.IsNullOrEmpty(devicePath))
            {
                return null;
            }

            var handle = CreateFile(devicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                return null;
            }

            try
            {
                if (!IsGamepadOrJoystick(handle))
                {
                    return null;
                }

                var attributes = new HIDD_ATTRIBUTES { Size = Marshal.SizeOf<HIDD_ATTRIBUTES>() };
                if (!HidD_GetAttributes(handle, ref attributes))
                {
                    return null;
                }

                var productName = TryGetProductString(handle);
                return new DetectedGamepad(attributes.VendorID, attributes.ProductID, productName, devicePath);
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        private static bool IsGamepadOrJoystick(IntPtr handle)
        {
            if (!HidD_GetPreparsedData(handle, out var preparsedData) || preparsedData == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var caps = new HIDP_CAPS();
                if (HidP_GetCaps(preparsedData, ref caps) != HIDP_STATUS_SUCCESS)
                {
                    return false;
                }

                return caps.UsagePage == HID_USAGE_PAGE_GENERIC &&
                    (caps.Usage == HID_USAGE_GAMEPAD || caps.Usage == HID_USAGE_JOYSTICK);
            }
            finally
            {
                HidD_FreePreparsedData(preparsedData);
            }
        }

        private static string TryGetProductString(IntPtr handle)
        {
            var buffer = new StringBuilder(256);
            return HidD_GetProductString(handle, buffer, buffer.Capacity * 2) ? buffer.ToString() : "";
        }

        private static string GetDevicePath(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA interfaceData)
        {
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out var requiredSize, IntPtr.Zero);
            if (requiredSize <= 0)
            {
                return null;
            }

            var buffer = Marshal.AllocHGlobal(requiredSize);
            try
            {
                // cbSize of SP_DEVICE_INTERFACE_DETAIL_DATA is 6 on x86 (4-byte int + 2-byte
                // char alignment padding) and 8 on x64 — the classic gotcha with this struct;
                // only the leading int matters for the call, the rest is opaque to us here.
                Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize);

                if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, buffer, requiredSize, out _, IntPtr.Zero))
                {
                    return null;
                }

                return Marshal.PtrToStringAuto(IntPtr.Add(buffer, sizeof(int)));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // --- P/Invoke declarations -----------------------------------------

        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING = 3;
        private const int HIDP_STATUS_SUCCESS = 0x110000;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetAttributes(IntPtr hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetProductString(IntPtr hidDeviceObject, StringBuilder buffer, int bufferLengthBytes);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, out int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
