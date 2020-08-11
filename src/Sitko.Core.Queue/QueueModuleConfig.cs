using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Sitko.Core.Queue.MediatR;

namespace Sitko.Core.Queue
{
    public class QueueModuleConfig
    {
        public HashSet<Type> Middlewares { get; } = new HashSet<Type>();

        public List<(Type serviceType, Type implementationType)> TranslateMediatRTypes { get; } =
            new List<(Type serviceType, Type implementationType)>();

        public HashSet<QueueProcessorEntry> ProcessorEntries { get; } = new HashSet<QueueProcessorEntry>();
        public Dictionary<Type, IQueueMessageOptions> Options { get; } = new Dictionary<Type, IQueueMessageOptions>();

        public bool HealthChecksEnabled { get; private set; }

        public void EnableHealthChecks()
        {
            HealthChecksEnabled = true;
        }

        public void TranslateMediatRNotification<TNotification>() where TNotification : class, INotification
        {
            TranslateMediatRTypes.Add((typeof(INotificationHandler<TNotification>),
                typeof(MediatRTranslator<TNotification>)));
        }

        public void ConfigureMessage<T>(IQueueMessageOptions<T> options) where T : class
        {
            if (Options.ContainsKey(typeof(T)))
            {
                throw new Exception($"Options for type {typeof(T)} already registered");
            }

            Options.Add(typeof(T), options);
        }

        public void RegisterMiddleware<TMiddleware>() where TMiddleware : IQueueMiddleware
        {
            Middlewares.Add(typeof(TMiddleware));
        }

        public void RegisterMiddlewares<TAssembly>()
        {
            var assembly = typeof(TAssembly).Assembly;
            foreach (var type in assembly.ExportedTypes)
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (typeof(IQueueMiddleware).IsAssignableFrom(implementedInterface))
                    {
                        Middlewares.Add(type);
                    }
                }
            }
        }

        public void RegisterProcessor<TProcessor, TMessage>()
            where TProcessor : class, IQueueProcessor<TMessage> where TMessage : class
        {
            var entry = ProcessorEntries.FirstOrDefault(e => e.Type == typeof(TProcessor)) ??
                        new QueueProcessorEntry(typeof(TProcessor));
            if (!entry.MessageTypes.Contains(typeof(TMessage)))
            {
                entry.MessageTypes.Add(typeof(TMessage));
            }

            ProcessorEntries.Add(entry);
        }

        public void RegisterProcessors<TAssembly>()
        {
            var assembly = typeof(TAssembly).Assembly;
            foreach (var type in assembly.ExportedTypes)
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                var method = GetType().GetMethod(nameof(RegisterProcessor));
                if (method == null)
                {
                    throw new Exception("Can't find method RegisterProcessor");
                }

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (typeof(IQueueProcessor).IsAssignableFrom(implementedInterface) &&
                        implementedInterface.IsGenericType)
                    {
                        if (!type.IsPublic)
                        {
                            throw new Exception($"Processor class {type.FullName} must be public");
                        }

                        var typeParam = implementedInterface.GetGenericArguments()[0];
                        if (typeParam.IsClass && !typeParam.IsAbstract)
                        {
                            var genericMethod = method.MakeGenericMethod(type, typeParam);
                            genericMethod.Invoke(this, new object[0]);
                        }
                    }
                }
            }
        }
    }

    public class QueueProcessorEntry
    {
        public QueueProcessorEntry(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
        public List<Type> MessageTypes { get; } = new List<Type>();

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is QueueProcessorEntry entry && entry.Type == Type;
        }
    }
}
