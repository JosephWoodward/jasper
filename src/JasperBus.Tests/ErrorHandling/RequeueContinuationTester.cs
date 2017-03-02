using System;
using JasperBus.ErrorHandling;
using JasperBus.Runtime.Invocation;
using NSubstitute;
using Xunit;

namespace JasperBus.Tests.ErrorHandling
{
    public class RequeueContinuationTester
    {
        [Fact]
        public void executing_just_puts_it_back_in_line_at_the_back_of_the_queue()
        {
            var envelope = ObjectMother.Envelope();

            var context = Substitute.For<IEnvelopeContext>();
            

            RequeueContinuation.Instance.Execute(envelope, context, DateTime.Now);

            envelope.Callback.Received(1).Requeue();
        }
    }
}