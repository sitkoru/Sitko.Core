namespace Sitko.Core.Grpc;

public partial class ApiResponseError
{
    public string ErrorsString => $"Code: {Code}. Errors: {string.Join(". ", Errors)}";
}

