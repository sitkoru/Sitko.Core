using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using Sitko.Core.App.Collections;
using Sitko.Core.Repository;
using Sitko.Core.Storage;

namespace MudBlazorUnited.Data.Entities;

public class BarModel : Entity<Guid>
{
    public string Bar { get; set; } = "";

    public List<FooModel> Foos { get; set; } = new();
    public Guid? FooId { get; set; }
    [ForeignKey(nameof(FooId))] public FooModel? Foo { get; set; }
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public decimal Sum { get; set; } = 100;

    public StorageItem? StorageItem { get; set; }

    public ValueCollection<StorageItem> StorageItems { get; set; } = new();
}

public class BarModelValidator : AbstractValidator<BarModel>
{
    public BarModelValidator()
    {
        RuleFor(m => m.Bar).NotEmpty().WithMessage("Blaaaaa");
        RuleFor(m => m.StorageItem).NotEmpty().WithMessage("Upload me!");
        RuleFor(m => m.StorageItems).NotEmpty().WithMessage("Upload us!");
    }
}
