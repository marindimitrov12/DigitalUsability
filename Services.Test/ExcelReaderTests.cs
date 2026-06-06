using Services.Interfaces;
using System.Text;
using Xunit;

namespace Services.Tests
{
    public class ExcelReaderTests
    {
        [Fact]
        public void Constructor_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var factory = new FakeStreamReaderFactory("");

            // Act + Assert
            Assert.Throws<ArgumentException>(
                () => new ExcelReader.ExcelReader("", factory));
        }

        [Fact]
        public void Constructor_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            var factory = new FakeStreamReaderFactory("");

            var path = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            // Act + Assert
            Assert.Throws<FileNotFoundException>(
                () => new ExcelReader.ExcelReader(path, factory));
        }

        [Fact]
        public async Task ReadLinesAsync_WithSkipHeader_ReadsDataRowsOnly()
        {
            // Arrange
            var csv =
                "Name,Country\n" +
                "John,Bulgaria\n" +
                "Mike,Germany";

            var path = CreateTempFile();

            var factory = new FakeStreamReaderFactory(csv);
            var reader = new ExcelReader.ExcelReader(path, factory);

            var result = new List<string[]>();

            // Act
            await foreach (var row in reader.ReadLinesAsync())
            {
                result.Add(row);
            }

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Equal("John", result[0][0]);
            Assert.Equal("Bulgaria", result[0][1]);

            Assert.Equal("Mike", result[1][0]);
            Assert.Equal("Germany", result[1][1]);
        }

        [Fact]
        public async Task ReadLinesAsync_WithoutSkipHeader_ReadsAllRows()
        {
            // Arrange
            var csv =
                @"Name,Country
John,Bulgaria";

            var path = CreateTempFile();

            var factory = new FakeStreamReaderFactory(csv);
            var reader = new ExcelReader.ExcelReader(path, factory);

            var result = new List<string[]>();

            // Act
            await foreach (var row in reader.ReadLinesAsync(skipHeader: false))
            {
                result.Add(row);
            }

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Equal("Name", result[0][0]);
            Assert.Equal("Country", result[0][1]);

            Assert.Equal("John", result[1][0]);
            Assert.Equal("Bulgaria", result[1][1]);
        }

        [Fact]
        public async Task ReadLinesAsync_EmptyFile_ReturnsNoRows()
        {
            // Arrange
            var csv = string.Empty;

            var path = CreateTempFile();

            var factory = new FakeStreamReaderFactory(csv);
            var reader = new ExcelReader.ExcelReader(path, factory);

            var result = new List<string[]>();

            // Act
            await foreach (var row in reader.ReadLinesAsync())
            {
                result.Add(row);
            }

            // Assert
            Assert.Empty(result);
        }

        private static string CreateTempFile()
        {
            return Path.GetTempFileName();
        }
    }

    public class FakeStreamReaderFactory : IStreamReaderFactory
    {
        private readonly string _content;

        public FakeStreamReaderFactory(string content)
        {
            _content = content;
        }

        public StreamReader Create(string path)
        {
            var bytes = Encoding.UTF8.GetBytes(_content);
            var stream = new MemoryStream(bytes);

            return new StreamReader(stream);
        }
    }
}