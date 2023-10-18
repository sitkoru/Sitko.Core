using Microsoft.EntityFrameworkCore;
using Sitko.Core.Tasks.Data;

namespace MudBlazorUnited.Tasks;

public class MudBlazorTasksDbContext(DbContextOptions<MudBlazorTasksDbContext> options) : TasksDbContext<MudBlazorBaseTask>(options);
