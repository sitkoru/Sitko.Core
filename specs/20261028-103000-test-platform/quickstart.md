# Проверка

Нужен SDK10.0.403. Из корня выполнить `dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net10.0 --report-trx --minimum-expected-tests 1`. Каталог получить через --list-tests; класс выбрать через --filter-class. CI добавляет --report-github. Сравнить каталог с исходным и проверить ненулевой код при временной ошибке существующего утверждения без сохранения её в коммите.
