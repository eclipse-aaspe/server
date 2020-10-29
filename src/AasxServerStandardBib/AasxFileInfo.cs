namespace AasxRestServerLibrary
{
    public class AasxFileInfo
    {
        public string path { get; set; } = null;
        public bool instantiateTemplate { get; set; } = false;
        public string identificationSuffix { get; set; } = null;
        public bool instantiateSubmodelsIdShort { get; set; } = false;
    }
}
