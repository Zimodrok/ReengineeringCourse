using NUnit.Framework;
using NetSdrClientApp.Messages;
using System;
using System.Linq;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(actualCode, Is.EqualTo((short)code));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void TranslateMessage_ValidControlItem_ReturnsTrueAndParsesData()
        {
            // Arrange
            byte[] msg = { 0x06, 0x00, 0x20, 0x00, 0xAA, 0xBB }; 

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var type, out var itemCode, out var sequenceNum, out var body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(type, Is.EqualTo(NetSdrMessageHelper.MsgTypes.SetControlItem));
                Assert.That(itemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency));
                Assert.That(body, Is.EquivalentTo(new byte[] { 0xAA, 0xBB }));
            });
        }

        [Test]
        public void TranslateMessage_InvalidControlItemCode_ReturnsFalse()
        {
            // Arrange
            byte[] msg = { 0x06, 0x00, 0x99, 0x99, 0xAA, 0xBB };

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var type, out var itemCode, out _, out _);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False, "Метод має повернути false для невідомого коду");
                Assert.That(itemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            });
        }

        [Test]
        public void GetSamples_ValidInput_ReturnsCorrectIntegers()
        {
            // Arrange
            ushort sampleSizeBits = 16; 
            byte[] body = { 0x01, 0x00, 0x02, 0x00, 0xFF, 0x00 }; 

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSizeBits, body).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(samples.Count, Is.EqualTo(3));
                Assert.That(samples[0], Is.EqualTo(1));
                Assert.That(samples[1], Is.EqualTo(2));
                Assert.That(samples[2], Is.EqualTo(255));
            });
        }

        [Test]
        public void GetSamples_TooLargeSampleSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ushort invalidSampleSizeBits = 40; 
            byte[] body = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var samples = NetSdrMessageHelper.GetSamples(invalidSampleSizeBits, body).ToList();
            });
        }
    }
}