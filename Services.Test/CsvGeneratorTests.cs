using Models;
using Services;
using Services.Interfaces;
using System.Text;
using Xunit;

namespace Services.Tests
{
    public class CsvGeneratorTests
    {
        [Fact]
        public async Task WriteRowAsync_WithoutOpen_ThrowsException()
        {
            // Arrange
            var factory = new FakeStreamWriterFactory();

            var generator = new CsvGenerator.CsvGenerator(factory);

            var model = new ExcelModel();

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => generator.WriteRowAsync(model));
        }

        [Fact]
        public async Task OpenAsync_WhenFileDoesNotExist_WritesHeader()
        {
            // Arrange
            var factory = new FakeStreamWriterFactory();

            var generator = new CsvGenerator.CsvGenerator(factory);

            var path = Path.GetTempFileName();

            File.Delete(path);

            // Act
            await generator.OpenAsync(path);

            await generator.CloseAsync();

            // Assert
            var result = factory.GetContent();

            Assert.Contains(
                "Network;Network Address;Broadcast Address",
                result);
        }

        [Fact]
        public async Task WriteRowAsync_WritesCorrectCsvRow()
        {
            // Arrange
            var factory = new FakeStreamWriterFactory();

            var generator = new CsvGenerator.CsvGenerator(factory);

            var path = Path.GetTempFileName();

            await generator.OpenAsync(path);

            var model = new ExcelModel
            {
                Network = "TestNetwork",
                CountryName = "Bulgaria"
            };

            // Act
            await generator.WriteRowAsync(model);

            await generator.CloseAsync();

            // Assert
            var result = factory.GetContent();

            Assert.Contains("TestNetwork", result);

            Assert.Contains("Bulgaria", result);
        }

        [Fact]
        public async Task CloseAsync_CanBeCalledMultipleTimes()
        {
            // Arrange
            var factory = new FakeStreamWriterFactory();

            var generator = new CsvGenerator.CsvGenerator(factory);

            var path = Path.GetTempFileName();

            await generator.OpenAsync(path);

            // Act
            await generator.CloseAsync();

            var exception = await Record.ExceptionAsync(
                () => generator.CloseAsync());

            // Assert
            Assert.Null(exception);
        }
    }

    public class FakeStreamWriterFactory : IStreamWriterFactory
    {
        private readonly MemoryStream _memoryStream = new();

        private readonly StreamWriter _writer;

        public FakeStreamWriterFactory()
        {
            _writer = new StreamWriter(
                _memoryStream,
                Encoding.UTF8,
                leaveOpen: true);
        }

        public StreamWriter Create(
            string path,
            bool append,
            Encoding encoding)
        {
            return _writer;
        }

        public string GetContent()
        {
            _writer.Flush();

            _memoryStream.Position = 0;

            using var reader = new StreamReader(
                _memoryStream,
                Encoding.UTF8,
                leaveOpen: true);

            return reader.ReadToEnd();
        }
    }
}