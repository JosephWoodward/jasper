﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baseline.Dates;
using Jasper.Bus.Runtime;
using Jasper.Bus.Transports;
using Jasper.Bus.Transports.Sending;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Bus.Transports.Sending
{
    public class PingerTests
    {
        [Fact]
        public void ping_until_connected()
        {
            var completed = new ManualResetEvent(false);

            using (var pinger = new Pinger(new StubSender(5), 50.Milliseconds(), () => completed.Set()))
            {
                completed.WaitOne(1.Seconds())
                    .ShouldBeTrue();
            }


        }


    }

    public class StubSender : ISender
    {
        private readonly int _failureCount;

        public StubSender(int failureCount)
        {
            _failureCount = failureCount;
        }

        public void Dispose()
        {
        }

        public void Start(ISenderCallback callback)
        {
        }

        public readonly IList<Envelope> Queued = new List<Envelope>();

        public Task Enqueue(Envelope envelope)
        {
            Queued.Add(envelope);
            return Task.CompletedTask;
        }

        public Uri Destination { get; } = TransportConstants.LoopbackUri;

        public int QueuedCount => 0;

        public bool Latched { get; private set; }

        public Task LatchAndDrain()
        {
            Latched = false;
            return Task.CompletedTask;
        }

        public void Unlatch()
        {
            Latched = true;
        }

        private int _count = 0;

        public Task Ping()
        {
            _count++;

            if (_count < _failureCount)
            {
                throw new Exception("No!");
            }

            return Task.CompletedTask;

        }
    }
}
