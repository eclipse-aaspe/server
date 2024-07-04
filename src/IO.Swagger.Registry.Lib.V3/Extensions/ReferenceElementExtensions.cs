namespace IO.Swagger.Registry.Lib.V3.Extensions;

using System.Linq;

public static class ReferenceElementExtensions
{
    /// <summary>
    /// Reverses the keys in the Value property of the ReferenceElement.
    /// </summary>
    /// <param name="referenceElement">The reference element whose keys are to be reversed.</param>
    public static void ReverseReferenceKeys(this ReferenceElement referenceElement)
    {
        if (referenceElement?.Value?.Keys == null) return;

        var keys = referenceElement.Value.Keys.ToList();
        keys.Reverse();

        referenceElement.Value.Keys = keys;
    }
}
