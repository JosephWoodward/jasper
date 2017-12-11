﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports;
using Jasper.Marten.Persistence.Resiliency;
using Marten;
using Marten.Util;
using Npgsql;

namespace Jasper.Marten.Persistence
{
    public static class MartenStorageExtensions
    {
        public static async Task<List<Envelope>> ExecuteToEnvelopes(this NpgsqlCommand command)
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                var list = new List<Envelope>();

                while (await reader.ReadAsync())
                {
                    var bytes = await reader.GetFieldValueAsync<byte[]>(0);
                    var envelope = Envelope.Read(bytes);

                    list.Add(envelope);
                }

                return list;
            }
        }

        public static List<Envelope> LoadEnvelopes(this NpgsqlCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                var list = new List<Envelope>();

                while (reader.Read())
                {
                    var bytes = reader.GetFieldValue<byte[]>(0);
                    var envelope = Envelope.Read(bytes);
                    envelope.Status = reader.GetFieldValue<string>(1);
                    envelope.OwnerId = reader.GetFieldValue<int>(2);


                    if (!reader.IsDBNull(3))
                    {
                        var raw = reader.GetFieldValue<DateTime>(3);


                        envelope.ExecutionTime = raw.ToUniversalTime();
                    }

                    envelope.Attempts = reader.GetFieldValue<int>(4);

                    list.Add(envelope);
                }

                return list;
            }
        }

        public static List<Envelope> AllIncomingEnvelopes(this IQuerySession session)
        {
            var schema = session.DocumentStore.Tenancy.Default.DbObjects.SchemaTables()
                .FirstOrDefault(x => x.Name == PostgresqlEnvelopeStorage.IncomingTableName).Schema;



            return session.Connection
                .CreateCommand($"select body, status, owner_id, execution_time, attempts from {schema}.{PostgresqlEnvelopeStorage.IncomingTableName}")
                .LoadEnvelopes();
        }

        public static List<Envelope> AllOutgoingEnvelopes(this IQuerySession session)
        {
            var schema = session.DocumentStore.Tenancy.Default.DbObjects.SchemaTables()
                .FirstOrDefault(x => x.Name == PostgresqlEnvelopeStorage.IncomingTableName).Schema;



            return session.Connection
                .CreateCommand($"select body, '{TransportConstants.Outgoing}', owner_id, now() as execution_time, 0 from {schema}.{PostgresqlEnvelopeStorage.OutgoingTableName}")
                .LoadEnvelopes();
        }

        public static void StoreIncoming(this IDocumentSession session, OwnershipMarker marker, Envelope envelope)
        {
            var operation = new StoreIncomingEnvelope(marker.Incoming, envelope);
            session.QueueOperation(operation);
        }

        public static void StoreOutgoing(this IDocumentSession session, OwnershipMarker marker, Envelope envelope, int ownerId)
        {
            var operation = new StoreOutgoingEnvelope(marker.Outgoing, envelope, ownerId);
            session.QueueOperation(operation);
        }

        public static void StoreIncoming(this IDocumentSession session, OwnershipMarker marker, Envelope[] messages)
        {
            foreach (var envelope in messages)
            {
                var operation = new StoreIncomingEnvelope(marker.Incoming, envelope);
                session.QueueOperation(operation);
            }
        }
    }
}
