using System;

namespace AasxServerStandardBib.Transformers
{
    public static class Update
    {
        private static readonly UpdateTransformer Transformer = new UpdateTransformer();

        public static void ToUpdateObject(IClass source, IClass target)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            Transformer.Visit(source, target);
        }
    }
}
