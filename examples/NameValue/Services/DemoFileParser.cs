
namespace ApiNoUi.Services;

public class DemoFileParser : IFileParser
{
    public static List<NameValueRecord> table = [];
    private readonly ILogger<DemoFileParser> _logger;
    public DemoFileParser(ILogger<DemoFileParser> logger) => _logger = logger;

    public static bool Parse(string filePath, string user = "")
    {
        var lines = File.ReadAllLines(filePath);
        var domain = "";

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0)
            {
                domain = lines[i];

                if (user != "" && domain != user)
                {
                    return false;
                }

                table.RemoveAll(t => t.Domain == domain);
            }
            else
            {
                var split = lines[i].Split(" :: ");
                if (domain != "" && split.Length == 2)
                {
                    table.Add(new NameValueRecord(domain, split[0], split[1]));
                }
            }
        }

        return true;
    }

    public Task ParseAsync(string filePath, string user = "")
    {
        _logger.LogInformation("Parser: {Path}", filePath);

        Parse(filePath, user);

        return Task.CompletedTask;
    }
}
