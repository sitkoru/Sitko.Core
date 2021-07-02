using FluentValidation;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModuleOptions : SearchModuleOptions
    {
        public string Prefix { get; set; } = string.Empty;
        public string Url { get; set; } = "http://localhost:9200";
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableClientLogging { get; set; } = false;
    }

    public class ElasticSearchModuleOptionsValidator : AbstractValidator<ElasticSearchModuleOptions>
    {
        public ElasticSearchModuleOptionsValidator()
        {
            RuleFor(o => o.Url).NotEmpty().WithMessage("Elastic url is empty");
        }
    }
}
