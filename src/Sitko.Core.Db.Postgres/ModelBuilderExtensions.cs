using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sitko.Core.App.Json;

namespace Sitko.Core.Db.Postgres;

public static class ModelBuilderExtensions
{
    public static void RegisterJsonConversion<TEntity, TData>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, TData>> getProperty, string name, bool throwOnError = true)
        where TEntity : class where TData : new()
    {
        var valueComparer = new ValueComparer<TData>(
            (c1, c2) => c1!.Equals(c2),
            c => c!.GetHashCode(),
            c => JsonHelper.DeserializeWithMetadata<TData>(
                JsonHelper.SerializeWithMetadata(c!, throwOnError, false), throwOnError, false)!);
        modelBuilder
            .Entity<TEntity>()
            .Property(getProperty)
            .HasColumnType("jsonb")
            .HasColumnName(name)
            .HasConversion(data => JsonHelper.SerializeWithMetadata(data!, throwOnError, false),
                json => JsonHelper.DeserializeWithMetadata<TData>(json, throwOnError, false) ?? new TData())
            .Metadata.SetValueComparer(valueComparer);
    }

    public static void RegisterJsonCollectionConversion<TEntity, TData>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, ICollection<TData>>> getProperty, string name, bool throwOnError = true)
        where TEntity : class
    {
        var valueComparer = new ValueComparer<ICollection<TData>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
            c => JsonHelper.DeserializeWithMetadata<ICollection<TData>>(
                JsonHelper.SerializeWithMetadata(c, throwOnError, false), throwOnError, false)!);
        modelBuilder
            .Entity<TEntity>()
            .Property(getProperty)
            .HasColumnType("jsonb")
            .HasColumnName(name)
            .HasConversion(data => JsonHelper.SerializeWithMetadata(data, throwOnError, false),
                json => JsonHelper.DeserializeWithMetadata<ICollection<TData>>(json, throwOnError, false) ??
                        new List<TData>())
            .Metadata.SetValueComparer(valueComparer);
    }

    public static void RegisterJsonArrayConversion<TEntity, TData>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, TData[]>> getProperty, string name, bool throwOnError = true)
        where TEntity : class
    {
        var valueComparer = new ValueComparer<TData[]>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
            c => JsonHelper.DeserializeWithMetadata<TData[]>(
                JsonHelper.SerializeWithMetadata(c, throwOnError, false), throwOnError, false)!);
        modelBuilder
            .Entity<TEntity>()
            .Property(getProperty)
            .HasColumnType("jsonb")
            .HasColumnName(name)
            .HasConversion(data => JsonHelper.SerializeWithMetadata(data, throwOnError, false),
                json => JsonHelper.DeserializeWithMetadata<TData[]>(json, throwOnError, false) ?? Array.Empty<TData>())
            .Metadata.SetValueComparer(valueComparer);
    }

    public static void RegisterJsonEnumerableConversion<TEntity, TData, TEnumerable>(this ModelBuilder modelBuilder,
        Expression<Func<TEntity, TEnumerable>> getProperty, string name, bool throwOnError = true)
        where TEntity : class
        where TEnumerable : IEnumerable<TData>, new()
    {
        var valueComparer = new ValueComparer<TEnumerable>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
            c => JsonHelper.DeserializeWithMetadata<TEnumerable>(
                JsonHelper.SerializeWithMetadata(c, throwOnError, false), throwOnError, false)!);
        modelBuilder
            .Entity<TEntity>()
            .Property(getProperty)
            .HasColumnType("jsonb")
            .HasColumnName(name)
            .HasConversion(data => JsonHelper.SerializeWithMetadata(data, throwOnError,false),
                json => JsonHelper.DeserializeWithMetadata<TEnumerable>(json, throwOnError,false) ??
                        new TEnumerable())
            .Metadata.SetValueComparer(valueComparer);
    }

    public static ModelBuilder ConfigureSchema(this ModelBuilder modelBuilder, DbContextOptions options)
    {
        var schemaExtension = options.FindExtension<SchemaDbContextOptionsExtension>();
        if (schemaExtension is not null)
        {
            if (schemaExtension.IsCustomSchema)
            {
                modelBuilder.HasDefaultSchema(schemaExtension.Schema);
            }
        }

        return modelBuilder;
    }
}
