using System;

namespace AasxServerStandardBib.Exceptions
{
    public class InvalidIdShortPathException : Exception
    {
        public InvalidIdShortPathException(string idShortPath) : base($"Invalid segment {idShortPath} in IdShortPath.")
        {
        }
    }
}