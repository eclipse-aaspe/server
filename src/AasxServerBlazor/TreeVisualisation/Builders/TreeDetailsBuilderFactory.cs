using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;

namespace AasxServerBlazor.TreeVisualisation.Builders;

internal static class TreeDetailsBuilderFactory
{
    public static ITreeDetailsBuilder Create(TreeItem treeItem)
    {
        return treeItem?.Tag switch
        {
            AssetAdministrationShell => new AssetAdministrationShellDetailsBuilder(),
            Submodel => new SubmodelDetailsBuilder(),
            Property => new PropertyDetailsBuilder(),
            Entity => new EntityDetailsBuilder(),
            File => new FileDetailsBuilder(),
            Blob => new BlobDetailsBuilder(),
            Range => new RangeDetailsBuilder(),
            Operation => new OperationDetailsBuilder(),
            AnnotatedRelationshipElement => new AnnotatedRelationshipElementDetailsBuilder(),
            RelationshipElement => new RelationshipElementDetailsBuilder(),
            ReferenceElement => new ReferenceElementDetailsBuilder(),
            MultiLanguageProperty => new MultiLanguagePropertyDetailsBuilder(),
            ISubmodelElement => new SubmodelElementDetailsBuilder(),
            _ => new EmptyTreeDetailsBuilder()
        };
    }
}