using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Email.Tests
{
    public sealed class EmailDeliveryTests(ITestOutputHelper testOutputHelper)
        : BaseTest<EmailDeliveryTestScope>(testOutputHelper)
    {
        [Fact]
        public async Task SendMailAsyncDeliversHtmlAndFullAttachmentsToEachRecipient()
        {
            var scope = await GetScopeAsync();
            var sender = scope.GetService<RecordingSender>();
            await using var firstData = new MemoryStream([0x00, 0x7F, 0xFF]);
            await using var secondData = new MemoryStream([0x48, 0x69, 0x0A]);

            var result = await scope.GetService<IMailSender>()
                .SendMailAsync(CreateEntry(firstData, secondData), "<html><body><p>Known HTML</p></body></html>");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Collection(sender.Messages,
                static message => AssertMessage(message, "first@example.test"),
                static message => AssertMessage(message, "second@example.test"));
        }

        [Fact]
        public async Task SendMailAsyncReportsFirstRecipientFailureAndContinues()
        {
            var scope = await GetScopeAsync();
            var sender = scope.GetService<RecordingSender>();
            sender.FailingRecipient = "first@example.test";
            await using var firstData = new MemoryStream([0x00, 0x7F, 0xFF]);
            await using var secondData = new MemoryStream([0x48, 0x69, 0x0A]);

            var result = await scope.GetService<IMailSender>()
                .SendMailAsync(CreateEntry(firstData, secondData), "<html><body><p>Known HTML</p></body></html>");

            Assert.False(result.IsSuccess);
            Assert.Contains("recipient rejected", result.ErrorMessage);
            Assert.Collection(sender.Messages,
                static message => AssertMessage(message, "first@example.test"),
                static message => AssertMessage(message, "second@example.test"));
        }

        private static MailEntry CreateEntry(Stream firstData, Stream secondData) =>
            new("Known subject", ["first@example.test", "second@example.test"])
            {
                Attachments =
                {
                    new MailEntryAttachment("first.bin", "application/octet-stream", firstData),
                    new MailEntryAttachment("second.txt", "text/plain", secondData)
                }
            };

        private static void AssertMessage(RecordedMessage message, string recipient)
        {
            Assert.Equal("Known subject", message.Subject);
            Assert.Equal(new[] { recipient }, message.Recipients);
            Assert.True(message.IsHtml);
            Assert.Equal("<html><body><p>Known HTML</p></body></html>", message.Body);
            Assert.Collection(message.Attachments,
                static attachment =>
                {
                    Assert.Equal("first.bin", attachment.Filename);
                    Assert.Equal("application/octet-stream", attachment.ContentType);
                    Assert.Equal(new byte[] { 0x00, 0x7F, 0xFF }, attachment.Bytes);
                },
                static attachment =>
                {
                    Assert.Equal("second.txt", attachment.Filename);
                    Assert.Equal("text/plain", attachment.ContentType);
                    Assert.Equal("Hi\n"u8.ToArray(), attachment.Bytes);
                });
        }
    }

    public sealed class EmailDeliveryTestScope : BaseTestScope
    {
        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder,
            string name)
        {
            _ = base.ConfigureApplication(hostBuilder, name)
                .GetSitkoCore()
                .AddModule<DevEmailModule, DevEmailModuleOptions>();
            _ = hostBuilder.Services.AddSingleton<RecordingSender>();
            _ = hostBuilder.Services.AddSingleton<ISender>(static provider => provider.GetRequiredService<RecordingSender>());
            return hostBuilder;
        }
    }

    public sealed class RecordingSender : ISender
    {
        public List<RecordedMessage> Messages { get; } = [];
        public string? FailingRecipient { get; set; }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null) =>
            throw new NotSupportedException("The test expects asynchronous delivery.");

        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            var attachments = new List<RecordedAttachment>();
            foreach (var attachment in email.Data.Attachments)
            {
                using var copy = new MemoryStream();
                await attachment.Data.CopyToAsync(copy, token ?? CancellationToken.None);
                attachments.Add(new RecordedAttachment(attachment.Filename, attachment.ContentType, copy.ToArray()));
            }

            var message = new RecordedMessage(email.Data.Subject,
                [.. email.Data.ToAddresses.Select(static address => address.EmailAddress)],
                email.Data.IsHtml, email.Data.Body, attachments);
            Messages.Add(message);
            return new SendResponse
            {
                ErrorMessages = FailingRecipient is not null && message.Recipients.Contains(FailingRecipient)
                    ? ["recipient rejected"]
                    : []
            };
        }
    }

    public sealed record RecordedMessage(string Subject, string[] Recipients, bool IsHtml, string Body,
        IReadOnlyList<RecordedAttachment> Attachments);

    public sealed record RecordedAttachment(string Filename, string ContentType, byte[] Bytes);
}
