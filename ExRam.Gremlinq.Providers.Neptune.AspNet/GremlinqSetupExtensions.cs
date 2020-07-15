﻿using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExRam.Gremlinq.Core.AspNet
{
    public static class GremlinqSetupExtensions
    {
        private sealed class UseNeptuneGremlinQueryEnvironmentTransformation : IGremlinQueryEnvironmentTransformation
        {
            private readonly IConfiguration _configuration;
            private readonly IEnumerable<IWebSocketGremlinQueryEnvironmentBuilderTransformation> _webSocketTransformations;

            // ReSharper disable once SuggestBaseTypeForParameter
            public UseNeptuneGremlinQueryEnvironmentTransformation(
                IGremlinqConfiguration configuration,
                IEnumerable<IWebSocketGremlinQueryEnvironmentBuilderTransformation> webSocketTransformations)
            {
                _webSocketTransformations = webSocketTransformations;
                _configuration = configuration
                    .GetSection("Neptune");
            }

            public IGremlinQueryEnvironment Transform(IGremlinQueryEnvironment environment)
            {
                return environment
                    .UseNeptune(builder => builder.Configure(_configuration, _webSocketTransformations));
            }
        }

        public static GremlinqSetup UseNeptune(this GremlinqSetup setup)
        {
            return setup
                .RegisterTypes(serviceCollection => serviceCollection.AddSingleton<IGremlinQueryEnvironmentTransformation, UseNeptuneGremlinQueryEnvironmentTransformation>());
        }
    }
}
