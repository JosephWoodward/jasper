﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Jasper.Http.Model;
using Jasper.Internals.Codegen;

namespace Jasper.Http.ContentHandling
{
    public class WriteText : IWriterRule
    {
        public bool TryToApply(RouteChain chain)
        {
            if (chain.ResourceType != typeof(string)) return false;

            chain.Postprocessors.Add(new CallWriteText(chain.Action.ReturnVariable));

            return true;
        }
    }

    public class CallWriteText : MethodCall
    {
        private static readonly MethodInfo _method = typeof(RouteHandler).GetMethod(nameof(RouteHandler.WriteText));

        public CallWriteText(Variable text) : base(typeof(CallWriteText), _method)
        {
            Variables[0] = text;
            IsLocal = true;
        }
    }
}
