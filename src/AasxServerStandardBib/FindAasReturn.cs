using AdminShellNS;

namespace AasxRestServerLibrary
{
    public class FindAasReturn
    {
        public AdminShell.AdministrationShell aas { get; set; } = null;
        public int iPackage { get; set; } = -1;
    }
}
