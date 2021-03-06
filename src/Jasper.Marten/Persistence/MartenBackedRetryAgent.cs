﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Jasper.Bus.Logging;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports;
using Jasper.Bus.Transports.Configuration;
using Jasper.Bus.Transports.Sending;
using Jasper.Bus.Transports.Tcp;
using Jasper.Marten.Persistence.Resiliency;
using Marten;
using Marten.Util;
using NpgsqlTypes;

namespace Jasper.Marten.Persistence
{
    public class MartenBackedRetryAgent : RetryAgent
    {
        private readonly IDocumentStore _store;
        private readonly EnvelopeTables _marker;
        private readonly CompositeTransportLogger _logger;
        private readonly string _deleteIncoming;

        public MartenBackedRetryAgent(IDocumentStore store, ISender sender, RetrySettings settings, EnvelopeTables marker, CompositeTransportLogger logger) : base(sender, settings)
        {
            _store = store;
            _marker = marker;
            _logger = logger;

            _deleteIncoming = $"delete from {_marker.Incoming} where id = ANY(:idlist)";
        }

        public override async Task EnqueueForRetry(OutgoingMessageBatch batch)
        {

            var expiredInQueue = Queued.Where(x => x.IsExpired()).ToArray();
            var expiredInBatch = batch.Messages.Where(x => x.IsExpired()).ToArray();


            try
            {
                using (var session = _store.LightweightSession())
                {
                    var expired = expiredInBatch.Concat(expiredInQueue).ToArray();

                    session.DeleteEnvelopes(_marker.Incoming, expired);

                    var all = Queued.Where(x => !expiredInQueue.Contains(x))
                        .Concat(batch.Messages.Where(x => !expiredInBatch.Contains(x)))
                        .ToList();

                    if (all.Count > _settings.MaximumEnvelopeRetryStorage)
                    {
                        var reassigned = all.Skip(_settings.MaximumEnvelopeRetryStorage).ToArray();


                        session.MarkOwnership(_marker.Incoming, TransportConstants.AnyNode, reassigned);
                    }

                    await session.SaveChangesAsync();

                    _logger.DiscardedExpired(expired);

                    Queued = all.Take(_settings.MaximumEnvelopeRetryStorage).ToList();
                }
            }
            catch (Exception e)
            {
                _logger.LogException(e, message:"Failed while trying to enqueue a message batch for retries");


#pragma warning disable 4014
                Task.Delay(100).ContinueWith(async _ => await EnqueueForRetry(batch));
#pragma warning restore 4014

            }
        }

        public IList<Envelope> Queued { get; private set; } = new List<Envelope>();

        protected override void afterRestarting(ISender sender)
        {
            var expired = Queued.Where(x => x.IsExpired());
            if (expired.Any())
            {
                using (var conn = _store.Tenancy.Default.CreateConnection())
                {
                    conn.Open();

                    conn.CreateCommand(_deleteIncoming)
                        .With("idlist", expired.Select(x => x.Id).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Uuid)
                        .ExecuteNonQuery();
                }
            }

            var toRetry = Queued.Where(x => !x.IsExpired()).ToArray();
            Queued = new List<Envelope>();

            foreach (var envelope in toRetry)
            {
                // It's perfectly okay to not wait on the task here
                _sender.Enqueue(envelope);
            }


        }
    }
}
