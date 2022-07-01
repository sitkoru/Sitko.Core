using Microsoft.EntityFrameworkCore;
using WASMDemo.Shared.Data.Models;

namespace WASMDemo.Shared;

public class WasmDbContext : DbContext
{
    public WasmDbContext(DbContextOptions<WasmDbContext> options) : base(options)
    {
    }
    
    public DbSet<TestEntity> Entities => Set<TestEntity>();
}