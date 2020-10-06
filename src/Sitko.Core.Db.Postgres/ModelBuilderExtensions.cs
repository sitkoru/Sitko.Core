using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace Sitko.Core.Db.Postgres
{
    public static class ModelBuilderExtensions
    {
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
        };

        private static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSettings);
        }

        private static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }


        public static void RegisterJsonConversion<TEntity, TData>(this ModelBuilder modelBuilder,
            Expression<Func<TEntity, TData>> getProperty, string name)
            where TEntity : class where TData : new()
        {
            var valueComparer = new ValueComparer<TData>(
                (c1, c2) => c1!.Equals(c2),
                c => c!.GetHashCode(),
                c => Deserialize<TData>(Serialize(c!)));
            modelBuilder
                .Entity<TEntity>()
                .Property(getProperty)
                .HasColumnType("jsonb")
                .HasColumnName(name)
                .HasConversion(data => Serialize(data!),
                    json => Deserialize<TData>(json) ?? new TData())
                .Metadata.SetValueComparer(valueComparer);
        }

        public static void RegisterJsonCollectionConversion<TEntity, TData>(this ModelBuilder modelBuilder,
            Expression<Func<TEntity, ICollection<TData>>> getProperty, string name)
            where TEntity : class
        {
            var valueComparer = new ValueComparer<ICollection<TData>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
                c => Deserialize<ICollection<TData>>(Serialize(c)));
            modelBuilder
                .Entity<TEntity>()
                .Property(getProperty)
                .HasColumnType("jsonb")
                .HasColumnName(name)
                .HasConversion(data => Serialize(data),
                    json => Deserialize<ICollection<TData>>(json) ?? new List<TData>())
                .Metadata.SetValueComparer(valueComparer);
        }

        public static void RegisterJsonArrayConversion<TEntity, TData>(this ModelBuilder modelBuilder,
            Expression<Func<TEntity, TData[]>> getProperty, string name)
            where TEntity : class
        {
            var valueComparer = new ValueComparer<TData[]>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
                c => Deserialize<TData[]>(Serialize(c)));
            modelBuilder
                .Entity<TEntity>()
                .Property(getProperty)
                .HasColumnType("jsonb")
                .HasColumnName(name)
                .HasConversion(data => Serialize(data),
                    json => Deserialize<TData[]>(json) ?? new TData[0])
                .Metadata.SetValueComparer(valueComparer);
        }
    }
}
