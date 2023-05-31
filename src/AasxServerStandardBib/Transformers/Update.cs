namespace AasxServerStandardBib.Transformers
{
    public static class Update
    {
        private static readonly UpdateTransformer Transformer = new UpdateTransformer();

        public static void ToUpdateObject(IClass source, IClass target)
        {
            Transformer.Visit(source, target);
        }
    }
}
