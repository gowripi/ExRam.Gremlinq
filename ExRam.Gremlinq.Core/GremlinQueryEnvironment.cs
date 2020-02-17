﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExRam.Gremlinq.Core
{
    public static class GremlinQueryEnvironment
    {
        private sealed class GremlinQueryEnvironmentImpl : IGremlinQueryEnvironment
        {
            public GremlinQueryEnvironmentImpl(
                IGraphModel model,
                IGremlinQuerySerializer serializer,
                IGremlinQueryExecutor executor,
                IGremlinQueryExecutionResultDeserializer deserializer,
                IImmutableDictionary<GremlinqOption, object> options,
                ILogger logger)
            {
                Model = model;
                Logger = logger;
                Options = options;
                Executor = executor;
                Serializer = serializer;
                Deserializer = deserializer;
            }

            public IGremlinQueryEnvironment ConfigureModel(Func<IGraphModel, IGraphModel> modelTransformation) => new GremlinQueryEnvironmentImpl(modelTransformation(Model), Serializer, Executor, Deserializer, Options, Logger);

            public IGremlinQueryEnvironment ConfigureOptions(Func<IImmutableDictionary<GremlinqOption, object>, IImmutableDictionary<GremlinqOption, object>> optionsTransformation) => new GremlinQueryEnvironmentImpl(Model, Serializer, Executor, Deserializer, optionsTransformation(Options), Logger);

            public IGremlinQueryEnvironment ConfigureLogger(Func<ILogger, ILogger> loggerTransformation) => new GremlinQueryEnvironmentImpl(Model, Serializer, Executor, Deserializer, Options, loggerTransformation(Logger));

            public IGremlinQueryEnvironment ConfigureDeserializer(Func<IGremlinQueryExecutionResultDeserializer, IGremlinQueryExecutionResultDeserializer> configurator) => new GremlinQueryEnvironmentImpl(Model, Serializer, Executor, configurator(Deserializer), Options, Logger);

            public IGremlinQueryEnvironment ConfigureSerializer(Func<IGremlinQuerySerializer, IGremlinQuerySerializer> configurator) => new GremlinQueryEnvironmentImpl(Model, configurator(Serializer), Executor, Deserializer, Options, Logger);

            public IGremlinQueryEnvironment ConfigureExecutor(Func<IGremlinQueryExecutor, IGremlinQueryExecutor> configurator) => new GremlinQueryEnvironmentImpl(Model, Serializer, configurator(Executor), Deserializer, Options, Logger);

            public ILogger Logger { get; }
            public IGraphModel Model { get; }
            public IGremlinQueryExecutor Executor { get; }
            public IGremlinQuerySerializer Serializer { get; }
            public IGremlinQueryExecutionResultDeserializer Deserializer { get; }
            public IImmutableDictionary<GremlinqOption, object> Options { get; }
        }

        public static readonly IGremlinQueryEnvironment Empty = new GremlinQueryEnvironmentImpl(
            GraphModel.Empty,
            GremlinQuerySerializer.Identity,
            GremlinQueryExecutor.Empty,
            GremlinQueryExecutionResultDeserializer.Empty,
            ImmutableDictionary<GremlinqOption, object>.Empty,
            NullLogger.Instance);

        public static readonly IGremlinQueryEnvironment Default = Empty
            .UseModel(GraphModel.Default(lookup => lookup
                .IncludeAssembliesFromAppDomain()))
            .UseSerializer(GremlinQuerySerializer.Default)
            .UseExecutor(GremlinQueryExecutor.Invalid);

        internal static readonly Step NoneWorkaround = new NotStep(GremlinQuery.Anonymous(Empty).Identity());

        public static IGremlinQueryEnvironment UseModel(this IGremlinQueryEnvironment source, IGraphModel model) => source.ConfigureModel(_ => model);

        public static IGremlinQueryEnvironment UseLogger(this IGremlinQueryEnvironment source, ILogger logger) => source.ConfigureLogger(_ => logger);

        public static IGremlinQueryEnvironment UseSerializer(this IGremlinQueryEnvironment environment, IGremlinQuerySerializer serializer) => environment.ConfigureSerializer(_ => serializer);

        public static IGremlinQueryEnvironment UseDeserializer(this IGremlinQueryEnvironment environment, IGremlinQueryExecutionResultDeserializer deserializer) => environment.ConfigureDeserializer(_ => deserializer);

        public static IGremlinQueryEnvironment UseExecutor(this IGremlinQueryEnvironment environment, IGremlinQueryExecutor executor) => environment.ConfigureExecutor(_ => executor);

        public static IAsyncEnumerable<TElement> Execute<TElement>(this IGremlinQueryEnvironment environment, IGremlinQueryBase<TElement> query)
        {
            var serialized = environment.Serializer
                .Serialize(query);

            if (serialized == null)
                return AsyncEnumerableEx.Throw<TElement>(new Exception("Can't serialize query."));

            return environment.Executor
                .Execute(serialized)
                .SelectMany(executionResult => environment.Deserializer
                    .Deserialize<TElement>(executionResult, query.AsAdmin().Environment));
        }

        public static IGremlinQueryEnvironment EchoGraphson(this IGremlinQueryEnvironment environment)
        {
            return environment
                .UseSerializer(GremlinQuerySerializer.Default)
                .UseExecutor(GremlinQueryExecutor.Echo)
                .UseDeserializer(GremlinQueryExecutionResultDeserializer.ToGraphson);
        }

        public static IGremlinQueryEnvironment EchoGroovy(this IGremlinQueryEnvironment environment)
        {
            return environment
                .ConfigureSerializer(serializer => serializer.ToGroovy())
                .UseExecutor(GremlinQueryExecutor.Echo)
                .UseDeserializer(GremlinQueryExecutionResultDeserializer.ToString);
        }
    }
}
