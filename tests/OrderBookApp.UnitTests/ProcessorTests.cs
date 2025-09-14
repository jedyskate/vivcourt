using OrderBookApp.Processors;
using Shouldly;
using System.Text;
using NUnit.Framework;

namespace OrderBookApp.UnitTests;

public class ProcessorTests
{
    [Test]
    [TestCase("input1.stream", "output1.log")]
    [TestCase("input2.stream", "output2.log")]
    public async Task Processor_Should_Produce_Correct_Output_From_Stream(string inputStreamFile, string outputLogFile)
    {
        // Arrange
        await using var stringWriter = new StringWriter();
        var processor = new OrderBookProcessor(5, stringWriter);
        var inputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "items", inputStreamFile);
        var expectedOutputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "items", outputLogFile);
        
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
        await using var fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        processor.ProcessStream(fileStream);
        var actualOutput = stringWriter.ToString();

        // Assert
        actualOutput.ShouldBe(expectedOutput);
    }
}
