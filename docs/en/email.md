# Email

[Русский](../ru/email.md) · [All topics](README.md)

`Sitko.Core.Email` provides `IMailSender`, `MailEntry`, attachments and HTML
templates; `Sitko.Core.Email.Smtp` connects SMTP delivery. You need
[registered Sitko.Core](application.md), a reachable SMTP server and permission
to send from the selected address. Call `AddSmtpEmail(...)` from the package namespace.

Section `Email:Smtp`: `Server=localhost`, `Port=25`, `UserName`, `Password`,
`EnableSsl=false`, `From=admin@localhost`. `Host=localhost` and `Scheme=http` are
additional shared `EmailModuleOptions` settings. Configure server, TLS,
credentials and sender for your environment; keep the password in an external
secret source.

Resolve `IMailSender` through DI in a scope and call
`SendMailAsync(new MailEntry(subject, recipients), htmlBody)`.
It returns `IOperationResult`: check success and error messages.
Recipients are processed sequentially; partial failures can occur after other
recipients received the email. Retrying the whole operation may send duplicates.
`SendHtmlMailAsync` supports Razor templates by path and Blazor components;
the application supplies template resources and parameter types.

For local development, register
`AddModule<DevEmailModule, DevEmailModuleOptions>()` from `Sitko.Core.Email`.
Its section is `Email:Dev`. It logs subjects/recipients and returns success but
does not deliver email. Use it instead of SMTP registration in that Host.
`SendInBackground` requires a separately registered `IBackgroundJobClient` and
throws `No background client!` without one. Removing the HangFire module does not
automatically provide a replacement for background delivery.

Verify SMTP using a controlled mailbox/environment: check the operation result,
received body and attachments, then server failure. DevEmail does not prove actual
delivery. The existing SMTP test sends real email; do not run it to verify documentation.

Sources: [API](../../src/Sitko.Core.Email/IMailSender.cs),
[sender](../../src/Sitko.Core.Email/FluentMailSender.cs),
[SMTP](../../src/Sitko.Core.Email.Smtp/SmtpEmailModuleConfig.cs),
[DevEmail](../../src/Sitko.Core.Email/DevEmailModule.cs).
