using NUnit.Framework;
using EchoServer;

namespace EchoServerTests;

[TestFixture]
public class EchoProcessorTests
{
    private EchoProcessor _processor;

    [SetUp]
    public void Setup()
    {
        _processor = new EchoProcessor();
    }

    [Test]
    public void Process_ValidMessage_ReturnsSameMessage()
    {
        string input = "Hello, SDR!";
        string result = _processor.Process(input);
        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    [TestCase("")]
    [TestCase("   ")]
    public void Process_InvalidMessage_ReturnsEmptyString(string input)
    {
        string result = _processor.Process(input);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Process_NullMessage_ReturnsEmptyString()
    {
        string result = _processor.Process(null!);
        Assert.That(result, Is.EqualTo(string.Empty));
    }
}
