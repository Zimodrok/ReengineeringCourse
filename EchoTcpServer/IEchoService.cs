using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public interface IEchoService
    {
        Task HandleStreamAsync(Stream stream, CancellationToken token);
    }
}