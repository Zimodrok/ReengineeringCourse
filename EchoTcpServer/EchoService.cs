using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class EchoService : IEchoService
    {
        public async Task HandleStreamAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            // Логіка читання та запису (тепер працює з будь-яким Stream)
            while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, token)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                Console.WriteLine($"Echoed {bytesRead} bytes.");
            }
        }
    }
}