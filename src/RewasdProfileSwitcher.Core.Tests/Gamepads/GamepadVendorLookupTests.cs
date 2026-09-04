using RewasdProfileSwitcher.Core.Gamepads;
using Xunit;

namespace RewasdProfileSwitcher.Core.Tests.Gamepads
{
    public class GamepadVendorLookupTests
    {
        [Theory]
        [InlineData(0x045E, "Xbox")]
        [InlineData(0x054C, "PlayStation")]
        [InlineData(0x28DE, "Steam")]
        public void GetVendorName_KnownVendor_ReturnsFriendlyName(ushort vendorId, string expected)
        {
            Assert.Equal(expected, GamepadVendorLookup.GetVendorName(vendorId));
        }

        [Fact]
        public void GetVendorName_UnknownVendor_ReturnsNull()
        {
            Assert.Null(GamepadVendorLookup.GetVendorName(0xFFFF));
        }

        [Fact]
        public void BuildDisplayName_ProductNameAlreadyMentionsVendor_UsesProductNameAsIs()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0x045E, "Xbox Wireless Controller");
            Assert.Equal("Xbox Wireless Controller", result);
        }

        [Fact]
        public void BuildDisplayName_ProductNameDoesNotMentionVendor_PrefixesVendor()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0x054C, "Wireless Controller");
            Assert.Equal("PlayStation Wireless Controller", result);
        }

        [Fact]
        public void BuildDisplayName_UnknownVendorWithProductName_UsesProductNameAsIs()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0xFFFF, "Generic USB Gamepad");
            Assert.Equal("Generic USB Gamepad", result);
        }

        [Fact]
        public void BuildDisplayName_BlankProductNameWithKnownVendor_FallsBackToVendorName()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0x28DE, "");
            Assert.Equal("Steam", result);
        }

        [Fact]
        public void BuildDisplayName_BlankProductNameWithUnknownVendor_FallsBackToGenericLabel()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0xFFFF, null);
            Assert.Equal("Game controller", result);
        }

        [Fact]
        public void BuildDisplayName_VendorMatchIsCaseInsensitive()
        {
            var result = GamepadVendorLookup.BuildDisplayName(0x045E, "xbox elite series 2");
            Assert.Equal("xbox elite series 2", result);
        }
    }
}
