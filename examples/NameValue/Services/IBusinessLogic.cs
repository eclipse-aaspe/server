
namespace ApiNoUi.Services;

public interface IBusinessLogic
{
    public Task<List<NameValueRecord>> QueryAsync(string? domain, string? name, string? value);
}
