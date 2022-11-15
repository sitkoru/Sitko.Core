using Microsoft.Extensions.Configuration;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Email.Tests;

public abstract class BasicTests<T> : BaseTest<T>
    where T : IBaseTestScope
{
    protected BasicTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Send()
    {
        var scope = await GetScopeAsync();
        var mailer = scope.GetService<IMailSender>();
        var configuration = scope.GetService<IConfiguration>();
        const string txtName = "file.txt";
        const string imgName = "img.jpg";
        await using var txtFile = File.Open($"Data/{txtName}", FileMode.Open);
        await using var imgFile = File.Open($"Data/{imgName}", FileMode.Open);
        var recipients = configuration["EMAIL_TESTS_RECIPIENTS"]!.Split(',');
        var entry = new MailEntry("Test email", recipients)
        {
            Attachments =
            {
                new MailEntryAttachment(txtName, "plain/text", txtFile),
                new MailEntryAttachment(imgName, "image/jpg", imgFile)
            }
        };

        var result = await mailer.SendMailAsync(entry, "<strong>Test email</strong>");
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }
}

