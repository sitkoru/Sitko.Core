# Электронная почта

[English](../en/email.md) · [Все темы](README.md)

`Sitko.Core.Email` предоставляет `IMailSender`, `MailEntry`, вложения и HTML-шаблоны;
`Sitko.Core.Email.Smtp` подключает доставку через SMTP. Нужны
[зарегистрированный Sitko.Core](application.md), доступный SMTP-сервер и права
отправки от выбранного адреса. Вызовите `AddSmtpEmail(...)` из пространства имён пакета.

Секция `Email:Smtp`: `Server=localhost`, `Port=25`, `UserName`, `Password`,
`EnableSsl=false`, `From=admin@localhost`. `Host=localhost` и `Scheme=http` —
дополнительные общие параметры `EmailModuleOptions`. Реальные сервер, TLS,
учётные данные и адрес отправителя задайте для своего окружения; пароль храните
во внешнем источнике секретов.

Получите `IMailSender` из DI в scope и вызовите
`SendMailAsync(new MailEntry(subject, recipients), htmlBody)`.
Метод возвращает `IOperationResult`: проверьте успех и сообщения об ошибках.
Рассылка идёт последовательно по получателям; ошибка части получателей может
сосуществовать с уже доставленными письмами. Общий повтор способен отправить их снова.
Поддерживаются Razor-шаблоны по пути и Blazor-компоненты через `SendHtmlMailAsync`;
ресурсы шаблона и типы параметров предоставляет приложение.

Для локальной разработки можно подключить
`AddModule<DevEmailModule, DevEmailModuleOptions>()` из `Sitko.Core.Email`.
Секция — `Email:Dev`. Он пишет тему/получателей в журнал и возвращает успешный
результат, но не доставляет письмо. Это альтернатива SMTP-регистрации в таком Host.
`SendInBackground` требует отдельно зарегистрированного `IBackgroundJobClient`;
без него выбрасывает `No background client!`. Удаление модуля HangFire не создаёт
замену фоновой доставки автоматически.

Проверка SMTP: используйте контролируемый почтовый ящик/стенд, проверьте результат
операции, полученное тело и вложения, затем отказ сервера. DevEmail не подтверждает
реальную доставку. Существующий SMTP-тест отправляет настоящее письмо;
не запускайте его как проверку документации.

Источники: [API](../../src/Sitko.Core.Email/IMailSender.cs),
[отправитель](../../src/Sitko.Core.Email/FluentMailSender.cs),
[SMTP](../../src/Sitko.Core.Email.Smtp/SmtpEmailModuleConfig.cs),
[DevEmail](../../src/Sitko.Core.Email/DevEmailModule.cs).
