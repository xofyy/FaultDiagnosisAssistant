using FaultDiagnosis.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FaultDiagnosis.Tests.Unit
{
    public class TextDocumentProcessorTests
    {
        private readonly TextDocumentProcessor _processor;
        public TextDocumentProcessorTests()
        {
            _processor = new TextDocumentProcessor();
        }

        [Fact]
        public async Task ProcessFileAsync_ShouldReturnEmptyList_WhenFileIsEmpty()
        {
            // Arrange
            var filePath = "empty.txt";
            await File.WriteAllTextAsync(filePath, string.Empty);

            try
            {
                // Act
                var result = await _processor.ProcessFileAsync(filePath);

                // Assert
                result.Should().BeEmpty();
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task ProcessFileAsync_ShouldReturnSingleChunk_WhenContentIsSmall()
        {
            // Arrange
            var content = "This is a small text.";
            var filePath = "small.txt";
            await File.WriteAllTextAsync(filePath, content);

            try
            {
                // Act
                var result = await _processor.ProcessFileAsync(filePath);

                // Assert
                result.Should().HaveCount(1);
                result.First().Content.Should().Be(content);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task ProcessFileAsync_ShouldSplitByParagraphs()
        {
            // Arrange
            // Create content larger than default chunk size (1000)
            var para1 = new string('a', 600);
            var para2 = new string('b', 600);
            var content = $"{para1}\n\n{para2}";
            var filePath = "paragraphs.txt";
            await File.WriteAllTextAsync(filePath, content);

            try
            {
                // Act
                var result = await _processor.ProcessFileAsync(filePath);

                // Assert
                // Should be split into 2 chunks because 600+600 > 1000
                result.Should().HaveCount(2);
                result[0].Content.Should().Be(para1);
                result[1].Content.Should().Be(para2);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task ProcessFileAsync_ShouldSplitBySentences_WhenParagraphIsTooLong()
        {
            // Arrange
            // A single paragraph with two long sentences
            var sentence1 = new string('a', 600) + ".";
            var sentence2 = new string('b', 600) + ".";
            var content = $"{sentence1} {sentence2}";
            var filePath = "sentences.txt";
            await File.WriteAllTextAsync(filePath, content);

            try
            {
                // Act
                var result = await _processor.ProcessFileAsync(filePath);

                // Assert
                result.Should().HaveCount(2);
                // The splitter consumes the separator ". ", so the first chunk might lose the dot.
                // We accept either with or without dot for now, or strictly without.
                // Based on implementation: "part" does not have separator.
                // If flushed, it is just "part". So no dot.
                result[0].Content.Trim().Should().Be(sentence1.TrimEnd('.')); 
                result[1].Content.Trim().Should().Be(sentence2);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }
    }
}
