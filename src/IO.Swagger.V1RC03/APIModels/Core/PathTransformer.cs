using static AasCore.Aas3_0_RC02.Visitation;
using System.Text.Json.Nodes;
using AasCore.Aas3_0_RC02;
using System.Collections.Generic;
using System;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class PathTransformer : ITransformerWithContext<OutputModifierContext, List<string>>
    {
        public List<string> Transform(IClass that, OutputModifierContext context)
        {
            return that.Transform(this, context);
        }

        public List<string> Transform(Extension that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(AdministrativeInformation that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Qualifier that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(AssetAdministrationShell that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(AssetInformation that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Resource that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(SpecificAssetId that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Submodel that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(that.IdShort);

            if (that.SubmodelElements != null)
            {
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;    //This will be set for indirect children
                    }
                    foreach (ISubmodelElement item in that.SubmodelElements)
                    {
                        context.ParentPath = that.IdShort;
                        Transform(item, context);
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> Transform(RelationshipElement that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(SubmodelElementList that, OutputModifierContext context)
        {
            if (that.Value != null)
            {
                if (context.IncludeChildren)
                {
                    var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                    for (int i = 0; i < that.Value.Count; i++)
                    {
                        context.ParentPath = currentParentPath;
                        context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort + $"[{i}]");
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> Transform(SubmodelElementCollection that, OutputModifierContext context)
        {
            if (context.IdShortPaths.Count == 0)
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            }

            if (that.Value != null)
            {
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;    //This will be set for indirect children
                    }
                    var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                    foreach (ISubmodelElement item in that.Value)
                    {
                        context.ParentPath = currentParentPath;
                        Transform(item, context);
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> Transform(Property that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(MultiLanguageProperty that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(AasCore.Aas3_0_RC02.Range that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(ReferenceElement that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(Blob that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(File that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(AnnotatedRelationshipElement that, OutputModifierContext context)
        {
            if (context.IdShortPaths.Count == 0)
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            }

            if (that.Annotations != null)
            {
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;    //This will be set for indirect children
                    }
                    var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                    foreach (ISubmodelElement item in that.Annotations)
                    {
                        context.ParentPath = currentParentPath;
                        Transform(item, context);
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> Transform(Entity that, OutputModifierContext context)
        {
            if (context.IdShortPaths.Count == 0)
            {
                context.IdShortPaths.Add(that.IdShort);
            }
            else
            {
                context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            }

            if (that.Statements != null)
            {
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;    //This will be set for indirect children
                    }
                    var currentParentPath = string.IsNullOrEmpty(context.ParentPath) ? that.IdShort : $"{context.ParentPath}.{that.IdShort}";
                    foreach (ISubmodelElement item in that.Statements)
                    {
                        context.ParentPath = currentParentPath;
                        Transform(item, context);
                    }
                }
            }

            return context.IdShortPaths;
        }

        public List<string> Transform(EventPayload that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(BasicEventElement that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(Operation that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(OperationVariable that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Capability that, OutputModifierContext context)
        {
            context.IdShortPaths.Add(context.ParentPath + "." + that.IdShort);
            return context.IdShortPaths;
        }

        public List<string> Transform(ConceptDescription that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Reference that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(Key that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(LangString that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(LangStringSet that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(DataSpecificationContent that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(DataSpecification that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public List<string> Transform(AasCore.Aas3_0_RC02.Environment that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}