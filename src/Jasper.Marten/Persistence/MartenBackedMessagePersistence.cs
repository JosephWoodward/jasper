﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Jasper.Bus.Logging;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports;
using Jasper.Bus.Transports.Configuration;
using Jasper.Bus.Transports.Receiving;
using Jasper.Bus.Transports.Sending;
using Jasper.Bus.WorkerQueues;
using Jasper.Marten.Persistence.Resiliency;
using Marten;
using Marten.Util;

namespace Jasper.Marten.Persistence
{
    public class MartenBackedMessagePersistence : IPersistence
    {
        private readonly IDocumentStore _store;
        private readonly CompositeTransportLogger _logger;
        private readonly BusSettings _settings;
        private readonly OwnershipMarker _marker;

        public MartenBackedMessagePersistence(IDocumentStore store, CompositeTransportLogger logger, BusSettings settings, OwnershipMarker marker)
        {
            _store = store;
            _logger = logger;
            _settings = settings;
            _marker = marker;
        }

        public ISendingAgent BuildSendingAgent(Uri destination, ISender sender, CancellationToken cancellation)
        {
            return new MartenBackedSendingAgent(destination, _store, sender, cancellation, _logger, _settings, _marker);
        }

        public ISendingAgent BuildLocalAgent(Uri destination, IWorkerQueue queues)
        {
            return new LocalSendingAgent(destination, queues, _store, _marker);
        }

        public IListener BuildListener(IListeningAgent agent, IWorkerQueue queues)
        {
            return new MartenBackedListener(agent, queues, _store, _logger, _settings, _marker);
        }

        public void ClearAllStoredMessages()
        {
            using (var conn = _store.Tenancy.Default.CreateConnection())
            {
                conn.Open();

                conn.CreateCommand().Sql($"delete from {_marker.Incoming};delete from {_marker.Outgoing}")
                    .ExecuteNonQuery();

            }
        }

        public async Task ScheduleMessage(Envelope envelope)
        {
            if (!envelope.ExecutionTime.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(envelope), "No value for ExecutionTime");
            }

            envelope.Status = TransportConstants.Scheduled;
            envelope.OwnerId = TransportConstants.AnyNode;
            using (var session = _store.LightweightSession())
            {
                session.StoreIncoming(_marker, envelope);
                await session.SaveChangesAsync();
            }
        }
    }
}
