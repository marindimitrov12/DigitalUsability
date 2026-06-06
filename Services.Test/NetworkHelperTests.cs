using System;
using Xunit;

namespace Services.Tests
{
    public class NetworkHelperTests
    {
        private readonly NetworkHelper.NetworkHelper _sut;

        public NetworkHelperTests()
        {
            _sut = new NetworkHelper.NetworkHelper();
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0", "192.168.1.255")]
        [InlineData("10.0.0.0/8", "10.0.0.0", "10.255.255.255")]
        [InlineData("172.16.0.0/12", "172.16.0.0", "172.31.255.255")]
        [InlineData("192.168.1.10/24", "192.168.1.0", "192.168.1.255")]
        public void GetIpRange_ValidCidr_ReturnsCorrectRange(
            string cidr,
            string expectedFrom,
            string expectedTo)
        {
            // Act
            var (from, to) = _sut.GetIpRange(cidr);

            // Assert
            Assert.Equal(expectedFrom, from);
            Assert.Equal(expectedTo, to);
        }

        [Fact]
        public void GetIpRange_PrefixLength32_ReturnsSingleIp()
        {
            // Arrange
            string cidr = "192.168.1.100/32";

            // Act
            var (from, to) = _sut.GetIpRange(cidr);

            // Assert
            Assert.Equal("192.168.1.100", from);
            Assert.Equal("192.168.1.100", to);
        }

        [Fact]
        public void GetIpRange_PrefixLength0_ReturnsFullIpv4Range()
        {
            // Arrange
            string cidr = "0.0.0.0/0";

            // Act
            var (from, to) = _sut.GetIpRange(cidr);

            // Assert
            Assert.Equal("0.0.0.0", from);
            Assert.Equal("255.255.255.255", to);
        }

        [Theory]
        [InlineData("")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("/24")]
        [InlineData("192.168.1.0/24/1")]
        public void GetIpRange_InvalidCidrFormat_ThrowsArgumentException(string cidr)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _sut.GetIpRange(cidr));
        }

        [Theory]
        [InlineData("999.999.999.999/24")]
        [InlineData("abc.def.ghi.jkl/24")]
        public void GetIpRange_InvalidIpAddress_ThrowsArgumentException(string cidr)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _sut.GetIpRange(cidr));

            // Verify it's wrapping a FormatException
            Assert.IsType<FormatException>(ex.InnerException);
        }

        [Theory]
        [InlineData("192.168.1.0/-1")]
        [InlineData("192.168.1.0/33")]
        public void GetIpRange_InvalidPrefixLength_ThrowsException(string cidr)
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _sut.GetIpRange(cidr));
        }
    }
}