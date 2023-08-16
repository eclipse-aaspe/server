namespace AasSecurity.Models
{
    internal class PermissionsPerObject
    {
        internal IClass _Object { get; set; } //Can be reference or Property

        internal Permission Permission { get; set; }

        internal ISubmodelElementCollection Usage { get; set; } //Refactor in future

    }
}