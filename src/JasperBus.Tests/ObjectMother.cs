﻿using JasperBus.Runtime;
using NSubstitute;

namespace JasperBus.Tests
{
    public static class ObjectMother
    {
        public static Envelope Envelope()
        {
            return new Envelope
            {
                Data = new byte[] {1, 2, 3, 4},
                Callback = Substitute.For<IMessageCallback>()
            };
        }
    }
}