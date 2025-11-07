using System.Linq.Dynamic.Core;
namespace ApiNoUi.Services;

public class DemoBusinessLogic : IBusinessLogic
{
    private string expression(ref string input)
    {
        var ex = "Equals";

        if (input.Contains("%"))
        {
            if (input.StartsWith("%") && input.EndsWith("%"))
            {
                ex = "Contains";
            }
            else
            {
                if (input.StartsWith("%"))
                {
                    ex = "EndsWith";
                }
                else if (input.EndsWith("%"))
                {
                    ex = "StartsWith";
                }
            }
            input = input.Replace("%", "");
        }
        input = input.ToLower();

        return ex;
    }
    public Task<List<NameValueRecord>> QueryAsync(string? domain, string? name, string? value)
    {
        var result = new List<NameValueRecord>();
        bool wildCard = false;

        if (domain != null && domain.Contains("%"))
        {
            wildCard = true;
        }
        if (name != null && name.Contains("%"))
        {
            wildCard = true;
        }
        if (value != null && value.Contains("%"))
        {
            wildCard = true;
        }

        if (wildCard)
        {
            if (string.IsNullOrEmpty(domain))
            {
                domain = "%%";
            }
            if (string.IsNullOrEmpty(name))
            {
                name = "%%";
            }
            if (string.IsNullOrEmpty(value))
            {
                value = "%%";
            }

            var domainExpression = expression(ref domain);
            var nameExpression = expression(ref name);
            var valueExpression = expression(ref value);

            var ex = $"Domain.ToLower().{domainExpression}(\"{domain}\") && Name.ToLower().{nameExpression}(\"{name}\") && Value.ToLower().{valueExpression}(\"{value}\")";
            result = DemoFileParser.table.AsQueryable().Where(ex).ToList();
        }
        else
        {
            if (!string.IsNullOrEmpty(domain))
            {
                domain = domain.ToLower();
            }
            if (!string.IsNullOrEmpty(name))
            {
                name = name.ToLower();
            }
            if (!string.IsNullOrEmpty(value))
            {
                value = value.ToLower();
            }

            foreach (var t in DemoFileParser.table)
            {
                string d = t.Domain.ToLower();
                string n = t.Name.ToLower();
                string v = t.Value.ToLower();

                var domainExpression = "true";
                var nameExpression = "true";
                var valueExpression = "true";
                if (!string.IsNullOrEmpty(domain))
                {
                    domainExpression = expression(ref d);
                    domainExpression = $"\"{domain}\".{domainExpression}(\"{d}\")";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    nameExpression = expression(ref n);
                    nameExpression = $"\"{name}\".{nameExpression}(\"{n}\")";
                }
                if (!string.IsNullOrEmpty(value))
                {
                    valueExpression = expression(ref v);
                    valueExpression = $"\"{value}\".{valueExpression}(\"{v}\")";
                }

                var ex = $"{domainExpression} && {nameExpression} && {valueExpression}";
                List<NameValueRecord> list = [t];
                result.AddRange(list.AsQueryable().Where(ex).ToList());
            }
        }

        return Task.FromResult(result);
    }
}
