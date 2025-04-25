namespace AasxServerStandardBib.GraphQL;

using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ParameterNamesMiddleware
{
    private readonly FieldDelegate _next;

    public ParameterNamesMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var parameterNames = new List<string>();

        if (context.Selection.SelectionSet != null)
        {
            parameterNames = context.Selection.SelectionSet.Selections
                .OfType<FieldNode>()
                .Select(subField => subField.Name.Value)
                .ToList();
        }

        context.ContextData["ParameterNames"] = parameterNames;

        await _next(context);
    }
}
