namespace EchoServer
{
    public class EchoProcessor : IMessageProcessor
    {
        public string Process(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;
                
            return message;
        }
    }
}