using OrderBookApp.Processors;
using Shouldly;
using System.Text;
using NUnit.Framework;

namespace OrderBookApp.UnitTests;

public class ProcessorTests
{
    [Test]
    public async Task Processor_Should_Produce_Correct_Output_From_Stream()
    {
        // Arrange
        var stringWriter = new StringWriter();
        var processor = new OrderBookProcessor(5, stringWriter);
        var inputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "items", "input1.stream");
        var expectedOutputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "items", "output1.log");
        
        var expectedOutputBuilder = new StringBuilder();
        using (var reader = new StreamReader(expectedOutputPath))
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                expectedOutputBuilder.AppendLine(line);
            }
        }
        var expectedOutput = expectedOutputBuilder.ToString();

        // Act
        processor.ProcessStream(inputPath);
        var actualOutput = stringWriter.ToString();

        // Assert
        actualOutput.ShouldBe(expectedOutput);
    }
}
