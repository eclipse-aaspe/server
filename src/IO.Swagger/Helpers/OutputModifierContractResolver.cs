using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// This converter / contract resolver for Json.NET adaptively filters different levels of depth
    /// of nested AASX structures.
    /// </summary>
    public class OutputModifierContractResolver : DefaultContractResolver
    {
        public bool AasHasViews = true;
        public bool BlobHasValue = false; // only true, if extent modifier is withBLOBValue
        public bool SubmodelHasElements = true;
        public bool SmcHasValue = true;
        public bool OpHasVariables = true;
        private bool deep = true;
        private string content = "normal";





        /// <summary>
        /// Default level of the json response.  
        /// </summary>
        public bool Deep
        {
            get => deep;
            set
            {
                if (value == false)
                {
                    this.SubmodelHasElements = false;
                    this.SmcHasValue = false;
                    this.OpHasVariables = false;
                    this.BlobHasValue = false;
                    this.AasHasViews = false;
                }
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
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (!BlobHasValue && property.DeclaringType == typeof(AdminShell.Blob) && property.PropertyName == "value")
                property.ShouldSerialize = instance => { return false; };

            if (!SubmodelHasElements && property.DeclaringType == typeof(AdminShell.Submodel) && property.PropertyName == "submodelElements")
                property.ShouldSerialize = instance => { return false; };

            if (!SmcHasValue && property.DeclaringType == typeof(AdminShell.SubmodelElementCollection) && property.PropertyName == "value")
                property.ShouldSerialize = instance => { return false; };

            if (!OpHasVariables && property.DeclaringType == typeof(AdminShell.Operation) && (property.PropertyName == "in" || property.PropertyName == "out"))
                property.ShouldSerialize = instance => { return false; };

            if (!AasHasViews && property.DeclaringType == typeof(AdminShell.AdministrationShell) && property.PropertyName == "views")
                property.ShouldSerialize = instance => { return false; };

            return property;
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            return base.CreateMemberValueProvider(member);
        }
    }
}
