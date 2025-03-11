using FluentValidation;
using OpenSearch.Net;

namespace Sitko.Core.Search.OpenSearch;

public class OpenSearchModuleOptions : SearchModuleOptions
{
    public string Prefix { get; set; } = "";
    public string Url { get; set; } = "http://localhost:9200";
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableClientLogging { get; set; }
    public bool DisableCertificatesValidation { get; set; }
    public string CustomStemmer { get; set; } = "";
    public string PreTags { get; set; } = "";
    public string PostTags { get; set; } = "";
    public Refresh? Refresh { get; set; } = global::OpenSearch.Net.Refresh.False;
}

public class OpenSearchModuleOptionsValidator : AbstractValidator<OpenSearchModuleOptions>
{
    public OpenSearchModuleOptionsValidator() =>
        RuleFor(o => o.Url).NotEmpty().WithMessage("OpenSearch url is empty");
}
