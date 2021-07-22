using System;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModuleOptions<TDbContext> : BaseDbModuleOptions<TDbContext>,
        IModuleOptionsWithValidation
        where TDbContext : DbContext
    {
        public Type GetValidatorType() => typeof(InMemoryDatabaseModuleOptionsValidator<TDbContext>);
    }

    public class
        InMemoryDatabaseModuleOptionsValidator<TDbContext> : BaseDbModuleOptionsValidator<
            InMemoryDatabaseModuleOptions<TDbContext>, TDbContext> where TDbContext : DbContext
    {
        public InMemoryDatabaseModuleOptionsValidator() => RuleFor(o => o.Database).NotEmpty().WithMessage("Empty InMemory database name");
    }
}
