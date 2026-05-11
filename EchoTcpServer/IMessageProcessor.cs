namespace EchoServer
{
    public interface IMessageProcessor
    {
        string Process(string message);
    }
}