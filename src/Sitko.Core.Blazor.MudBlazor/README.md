# Sitko.Core.Blazor.MudBlazor

Обёртка над компонентами [MudBlazor](https://mudblazor.com/)

#### MudTable
Реализация [компонента таблицы](https://mudblazor.com/components/table#default-table)
для [Sitko.Core.Repository](https://github.com/sitkoru/Sitko.Core/tree/master/src/Sitko.Core.Repository)

Примеры работы: [apps](https://github.com/sitkoru/Sitko.Core/tree/master/src/Sitko.Core.Blazor.MudBlazor)

Применение:
1. Создать компонент, наследующийся от `MudRepositoryTable`
````c#
public class BarRepositoryList : MudRepositoryTable<BarModel, Guid, BarRepository>
{
}
````
2. Вместо `<MudTable>`  вызываем созданный компонент
````c#
<BarRepositoryList>
    <HeaderContent>
        ...
    </HeaderContent>
    <ChildContent>
        ...
    </ChildContent>
    <FooterContent>
    ...
    </FooterContent>
</BarRepositoryList>
````
3. Для изменения запроса получения данных необходимо указать параметр параметр `ConfigureQuery`
````c#
private Task ConfigureQueryAsync(IRepositoryQuery<BarModel> query)
{
    query.Where(...).Take(...).Skip(...);

    return Task.CompletedTask;
}
````
4. По умолчанию, фильтры (номер страницы, кол-во на странице, сортировка) при изменении не отображаются в url. 
Это поведение можно отключить, указав `EnableAddFiltersToUrl="true"`
5. Если необходимо добавить в url доп. фильтры, необходимо указать параметры `AddParamsToUrl` и `GetParamsFromUrl`
````c#
private Task<Dictionary<string, object?>> AddParamsToUrlAsync()
{
    var urlParams = new Dictionary<string, object?>();
    urlParams.Add("newParamTitle", "newParamValue");

    return Task.FromResult(urlParams);
}
````

````c#
[Parameter]
[SupplyParameterFromQuery(Name = "newParamTitle")]
public string? NewParam { get; set; }
private Task GetParamsFromUrlAsync()
{
    var hasChanged = false;
    if (!string.IsNullOrEmpty(Title))
    {
        YourFilter.Value = NewParam;
        hasChanged = true;
    }
    if (hasChanged)
    {
        StateHasChanged();
    }

    return Task.CompletedTask;
}
````
