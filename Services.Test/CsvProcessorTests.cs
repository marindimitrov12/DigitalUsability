using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Moq;
using Services.Interfaces;
using Xunit;

namespace Services.Tests
{
    public class CsvProcessorTests
    {
        private readonly Mock<IExcelReader> _readerMock;
        private readonly Mock<IExcelReader2> _reader2Mock;
        private readonly Mock<INetworkHelper> _networkHelperMock;
        private readonly Mock<ICsvGenerator> _csvMock;
        private readonly Mock<ILogger<CsvProcessor.CsvProcessor>> _loggerMock;

        private readonly CsvProcessor.CsvProcessor _sut;

        public CsvProcessorTests()
        {
            _readerMock = new Mock<IExcelReader>();
            _reader2Mock = new Mock<IExcelReader2>();
            _networkHelperMock = new Mock<INetworkHelper>();
            _csvMock = new Mock<ICsvGenerator>();
            _loggerMock = new Mock<ILogger<CsvProcessor.CsvProcessor>>();

            var options = Options.Create(
                new ExcelSettings
                {
                    OutputPath = @"C:\Output"
                });

            _sut = new CsvProcessor.CsvProcessor(
                _readerMock.Object,
                _networkHelperMock.Object,
                _reader2Mock.Object,
                _csvMock.Object,
                options,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessAsync_Should_Open_And_Close_Csv()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange(It.IsAny<string>()))
                .Returns(("192.168.1.0", "192.168.1.255"));

            // Act
            await _sut.ProcessAsync();

            // Assert
            _csvMock.Verify(
                x => x.OpenAsync(It.IsAny<string>()),
                Times.Once);

            _csvMock.Verify(
                x => x.CloseAsync(),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_Should_Write_Row_With_Country_Data()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange("192.168.1.0/24"))
                .Returns(("192.168.1.0", "192.168.1.255"));

            ExcelModel writtenModel = null;

            _csvMock
                .Setup(x => x.WriteRowAsync(It.IsAny<ExcelModel>()))
                .Callback<ExcelModel>(model => writtenModel = model)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessAsync();

            // Assert
            Assert.NotNull(writtenModel);

            Assert.Equal("192.168.1.0/24", writtenModel.Network);
            Assert.Equal("123", writtenModel.GeonameId);

            Assert.Equal("United States", writtenModel.CountryName);
            Assert.Equal("US", writtenModel.CountryISO);

            Assert.Equal("North America", writtenModel.ContinentName);
            Assert.Equal("NA", writtenModel.ContinentCode);

            Assert.Equal("192.168.1.0", writtenModel.NetworkAddress);
            Assert.Equal("192.168.1.255", writtenModel.BroadCastAddress);

            Assert.Equal("192.168.1.1", writtenModel.HostIpRangeFrom);
            Assert.Equal("192.168.1.254", writtenModel.HostIpRangeTo);
        }

        [Fact]
        public async Task ProcessAsync_Should_Write_Empty_Country_Fields_When_Geoname_Not_Found()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(EmptyCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange(It.IsAny<string>()))
                .Returns(("10.0.0.0", "10.0.0.255"));

            ExcelModel writtenModel = null;

            _csvMock
                .Setup(x => x.WriteRowAsync(It.IsAny<ExcelModel>()))
                .Callback<ExcelModel>(model => writtenModel = model)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessAsync();

            // Assert
            Assert.NotNull(writtenModel);

            Assert.Equal(string.Empty, writtenModel.CountryName);
            Assert.Equal(string.Empty, writtenModel.CountryISO);
            Assert.Equal(string.Empty, writtenModel.ContinentName);
            Assert.Equal(string.Empty, writtenModel.ContinentCode);
        }

        [Fact]
        public async Task ProcessAsync_Should_Write_One_Row_Per_Network()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetMultipleNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange(It.IsAny<string>()))
                .Returns(("192.168.0.0", "192.168.0.255"));

            // Act
            await _sut.ProcessAsync();

            // Assert
            _csvMock.Verify(
                x => x.WriteRowAsync(It.IsAny<ExcelModel>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessAsync_Should_Handle_32_Bit_Cidr()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(Get32BitNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange("192.168.1.10/32"))
                .Returns(("192.168.1.10", "192.168.1.10"));

            ExcelModel writtenModel = null;

            _csvMock
                .Setup(x => x.WriteRowAsync(It.IsAny<ExcelModel>()))
                .Callback<ExcelModel>(model => writtenModel = model)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessAsync();

            // Assert
            Assert.NotNull(writtenModel);

            Assert.Equal("192.168.1.10", writtenModel.HostIpRangeFrom);
            Assert.Equal("192.168.1.10", writtenModel.HostIpRangeTo);
        }

        [Fact]
        public async Task ProcessAsync_Should_Handle_31_Bit_Cidr()
        {
            // Arrange
            _reader2Mock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(GetCountryRows());

            _readerMock
                .Setup(x => x.ReadLinesAsync(',', true))
                .Returns(Get31BitNetworkRows());

            _networkHelperMock
                .Setup(x => x.GetIpRange("192.168.1.0/31"))
                .Returns(("192.168.1.0", "192.168.1.1"));

            ExcelModel writtenModel = null;

            _csvMock
                .Setup(x => x.WriteRowAsync(It.IsAny<ExcelModel>()))
                .Callback<ExcelModel>(model => writtenModel = model)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ProcessAsync();

            // Assert
            Assert.NotNull(writtenModel);

            Assert.Equal("192.168.1.0", writtenModel.HostIpRangeFrom);
            Assert.Equal("192.168.1.1", writtenModel.HostIpRangeTo);
        }

        private async IAsyncEnumerable<string[]> GetCountryRows()
        {
            yield return new[]
            {
                "123",
                "",
                "NA",
                "North America",
                "US",
                "United States"
            };

            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<string[]> EmptyCountryRows()
        {
            await Task.CompletedTask;
            yield break;
        }

        private async IAsyncEnumerable<string[]> GetNetworkRows()
        {
            yield return new[]
            {
                "192.168.1.0/24",
                "123"
            };

            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<string[]> GetMultipleNetworkRows()
        {
            yield return new[]
            {
                "192.168.1.0/24",
                "123"
            };

            yield return new[]
            {
                "10.0.0.0/24",
                "123"
            };

            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<string[]> Get32BitNetworkRows()
        {
            yield return new[]
            {
                "192.168.1.10/32",
                "123"
            };

            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<string[]> Get31BitNetworkRows()
        {
            yield return new[]
            {
                "192.168.1.0/31",
                "123"
            };

            await Task.CompletedTask;
        }
    }
}