namespace Sitko.Core.Blazor.Forms;

public record FormContext<TEntity>(TEntity Entity, BaseForm<TEntity> Form) where TEntity : class, new();
