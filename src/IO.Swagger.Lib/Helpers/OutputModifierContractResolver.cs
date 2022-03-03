using System;
using System.Collections.Generic;
using System.Reflection;
using AdminShellNS;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static AdminShellNS.AdminShellV20;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// This converter / contract resolver for Json.NET adaptively filters different levels of depth
    /// of nested AASX structures.
    /// </summary>
    public class OutputModifierContractResolver : DefaultContractResolver
    {
        private bool deep = true;
        private string content = "normal";
        private string extent = "withoutBlobValue";

        /// <summary>
        /// string: PropertyName to be excluded
        /// Type: Class from which property needs to be excluded
        /// </summary>
        private Dictionary<Type, List<string>> excludingProperties;


        /// <summary>
        /// Constructor
        /// </summary>
        public OutputModifierContractResolver()
        {
            this.excludingProperties = new Dictionary<Type, List<string>>();
            AddToExcludingProperties(typeof(Blob), "value"); // By Default withoutBLOB 
        }





        /// <summary>
        /// Default level of the json response.  
        /// </summary>
        public bool Deep
        {
            get => deep;
            set
            {
                //Core
                if (value == false)
                {
                    AddToExcludingProperties(typeof(AdministrationShell), "views");
                    AddToExcludingProperties(typeof(Blob), "value");
                    AddToExcludingProperties(typeof(SubmodelElementCollection), "value");
                    AddToExcludingProperties(typeof(Operation), "in");
                    AddToExcludingProperties(typeof(Operation), "out");

                }
            }
        }


        private void AddToExcludingProperties(Type type, string propName)
        {
            if (excludingProperties.ContainsKey(type))
            {
                excludingProperties.TryGetValue(type, out List<string> propNames);
                if (!propNames.Contains(propName))
                {
                    propNames.Add(propName);
                }
            }
            else
            {
                excludingProperties.Add(type, new List<string> { propName });
            }
        }

        /// <summary>
        /// The enumeration Content indicates the kind of the response content’s serialization.
        /// </summary>
        public string Content
        {
            get => content;
            set
            {
                content = value;
                if (value.Equals("trimmed", StringComparison.OrdinalIgnoreCase))
                {
                    AddToExcludingProperties(typeof(AdministrationShell), "security");
                    AddToExcludingProperties(typeof(AdministrationShell), "views");
                    AddToExcludingProperties(typeof(AdministrationShell), "asset");
                    AddToExcludingProperties(typeof(AdministrationShell), "submodels");
                    AddToExcludingProperties(typeof(Submodel), "submodelElements");
                    AddToExcludingProperties(typeof(SubmodelElementCollection), "value"); // TODO: Later as per data V3 
                    AddToExcludingProperties(typeof(Entity), "statements");
                    AddToExcludingProperties(typeof(Entity), "asset");  // TODO: Later as per data V3
                    AddToExcludingProperties(typeof(BasicEvent), "observed");
                    AddToExcludingProperties(typeof(Property), "value");
                    AddToExcludingProperties(typeof(Property), "valueId");
                    AddToExcludingProperties(typeof(MultiLanguageProperty), "value");
                    AddToExcludingProperties(typeof(MultiLanguageProperty), "valueId");
                    AddToExcludingProperties(typeof(AdminShellV20.Range), "min");
                    AddToExcludingProperties(typeof(AdminShellV20.Range), "max");
                    AddToExcludingProperties(typeof(RelationshipElement), "first");
                    AddToExcludingProperties(typeof(RelationshipElement), "second");
                    AddToExcludingProperties(typeof(AnnotatedRelationshipElement), "first");
                    AddToExcludingProperties(typeof(AnnotatedRelationshipElement), "second");
                    AddToExcludingProperties(typeof(AnnotatedRelationshipElement), "annotations");
                    AddToExcludingProperties(typeof(Blob), "mimeType");
                    AddToExcludingProperties(typeof(Blob), "value");
                    AddToExcludingProperties(typeof(File), "mimeType");
                    AddToExcludingProperties(typeof(File), "value");

                }
            }
        }

        /// <summary>
        /// The enumeration Extent indicates whether to include BLOB or not.
        /// </summary>
        public string Extent
        {
            get => extent;
            set
            {
                extent = value;
                if (value.Equals("WithBLOBValue", StringComparison.OrdinalIgnoreCase))
                {
                    if (excludingProperties.ContainsKey(typeof(Blob)))
                    {
                        excludingProperties.Remove(typeof(Blob));
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Type, List<string>> ExcludingProperties { get => excludingProperties; set => excludingProperties = value; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            bool excluded = false;
            if (excludingProperties.ContainsKey(property.DeclaringType))
            {
                excludingProperties.TryGetValue(property.DeclaringType, out List<string> propNames);
                foreach (string propName in propNames)
                {
                    if (property.PropertyName == propName)
                    {
                        excluded = true;
                        property.ShouldSerialize =
                            instance =>
                            {
                                return false;
                            };
                    }
                }
            }

            //Encode the value
            if (!excluded && (property.DeclaringType == typeof(Blob)) && property.PropertyName.Equals("value"))
            {
                property.ShouldSerialize =
                            instance =>
                            {
                                var value = Base64UrlEncoder.Encode(((Blob)instance).value);
                                ((Blob)instance).value = value;
                                return true;
                            };
            }

            return property;
        }

    }

}
