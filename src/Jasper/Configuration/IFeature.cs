﻿using System;
using System.IO;
using System.Threading.Tasks;
using Jasper.Internals;
using Jasper.Internals.Codegen;
using StructureMap;

namespace Jasper.Configuration
{
    public interface IFeature : IDisposable
    {
        Task<ServiceRegistry> Bootstrap(JasperRegistry registry);
        Task Activate(JasperRuntime runtime, GenerationRules generation);
        void Describe(JasperRuntime runtime, TextWriter writer);
    }
}
