using System;
using System.IO;
using System.Threading.Tasks;
using Jasper.Bus.Runtime;
using Jasper.Bus.Runtime.Invocation;
using Jasper.Bus.Transports.Configuration;

namespace Jasper.Bus.Transports
{
    public interface ITransport : IDisposable
    {
        string Protocol { get; }

        Task Send(Envelope envelope, Uri destination);

        IChannel[] Start(IHandlerPipeline pipeline, BusSettings settings);

        Uri DefaultReplyUri();

        TransportState State { get; }
        void Describe(TextWriter writer);
    }
}
