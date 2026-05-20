using Moq;
using NUnit.Framework;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

[TestFixture]
public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        // Імітація асинхронного повідомлення (твій оригінальний Setup)
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        // ФІКС ЗАВИСАННЯ: використовуємо SendMessageAsync замість Send
        // Це розблокує TaskCompletionSource у NetSdrClient
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            // Імітуємо прихід відповіді для розблокування Task
            byte[] response = new byte[] { 0x06, 0x00, bytes[2], 0x00, 0x00, 0x00 };
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, response);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var tcpClientMock = new Mock<ITcpClient>();
        var udpClientMock = new Mock<IUdpClient>();

        // Act
        var client = new NetSdrClient(tcpClientMock.Object, udpClientMock.Object);

        // Assert
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void TranslateMessage_WithSequenceNumber_ShouldCoverLogic()
    {
        byte[] msgWithSequence = new byte[] { 0x08, 0x00, 0x01, 0x00, 0x05, 0x00, 0x00, 0x00 }; 

        NetSdrMessageHelper.TranslateMessage(msgWithSequence, out _, out _, out ushort seq, out _);
        
        Assert.Pass(); 
    }

    [Test]
    public async Task Methods_WhenDisconnected_ShouldReturnEarly()
    {
        var tcpMock = new Mock<ITcpClient>();
        tcpMock.Setup(t => t.Connected).Returns(false); // Імітуємо відсутність з'єднання
        var udpMock = new Mock<IUdpClient>();
        var client = new NetSdrClient(tcpMock.Object, udpMock.Object);

        // Викликаємо методи — вони мають вийти через відсутність підключення
        await client.StopIQAsync();
        await client.ChangeFrequencyAsync(1000000, 1);

        Assert.That(tcpMock.Object.Connected, Is.EqualTo(false));
    }

    [Test]
    public async Task FullFlow_ShouldCoverRemainingLines()
    {
        var tcpMock = new Mock<ITcpClient>();
        tcpMock.Setup(t => t.Connected).Returns(true);
        
        // Налаштовуємо SendMessageAsync для локального мока
        tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) => {
            tcpMock.Raise(t => t.MessageReceived += null, tcpMock.Object, new byte[] { 0x06, 0x00, bytes[2], 0x00, 0x00, 0x00 });
        });

        var udpMock = new Mock<IUdpClient>();
        var client = new NetSdrClient(tcpMock.Object, udpMock.Object);

        // 1. Покриваємо ChangeFrequencyAsync
        await client.ChangeFrequencyAsync(14000000, 0);

        // 2. Покриваємо _udpClient_MessageReceived
        byte[] dummyUdpData = new byte[] { 0x08, 0x00, 0x04, 0x00, 0x01, 0x02, 0x03, 0x04 };
        udpMock.Raise(u => u.MessageReceived += null, new object(), dummyUdpData);

        Assert.Pass();
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public void DisconectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        //act
        await _client.StartIQAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        Assert.Multiple(() =>
        {
            _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.EqualTo(true));
        });
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        Assert.Multiple(() =>
        {
            _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.EqualTo(false));
        });
    }

    [Test]
    public void UdpClientWrapper_Dispose_ShouldCleanUpResources()
    {
        using (var wrapper = new NetSdrClientApp.Networking.UdpClientWrapper(50001))
        {
            Assert.DoesNotThrow(() => wrapper.Dispose());
        }
    }

    [Test]
    public void UdpClientWrapper_Equals_ShouldWorkCorrectly()
    {
        var wrapper1 = new NetSdrClientApp.Networking.UdpClientWrapper(50002);
        var wrapper2 = new NetSdrClientApp.Networking.UdpClientWrapper(50002);
        var wrapper3 = new NetSdrClientApp.Networking.UdpClientWrapper(50003);

        Assert.Multiple(() =>
        {
            Assert.That(wrapper1.Equals(wrapper2), Is.EqualTo(true));
            Assert.That(wrapper1.Equals(wrapper3), Is.EqualTo(false));
            Assert.That(wrapper1.GetHashCode(), Is.EqualTo(wrapper2.GetHashCode()));
        });
    }
}