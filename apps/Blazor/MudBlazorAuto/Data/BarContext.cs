﻿using Microsoft.EntityFrameworkCore;
using MudBlazorAuto.Data.Entities;
using Sitko.Core.App.Collections;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Storage;

namespace MudBlazorAuto.Data
{
    public class BarContext : DbContext
    {
        public DbSet<BarModel> Bars => Set<BarModel>();
        public DbSet<FooModel> Foos => Set<FooModel>();

        public BarContext(DbContextOptions<BarContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.RegisterJsonConversion<BarModel, StorageItem?>(model => model.StorageItem,
                nameof(BarModel.StorageItem));
            modelBuilder.RegisterJsonEnumerableConversion<BarModel, StorageItem, ValueCollection<StorageItem>>(
                model => model.StorageItems,
                nameof(BarModel.StorageItems));
        }
    }
}
