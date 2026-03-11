namespace AasSecurity
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Xml;
    using System.Xml;

    internal class TrustListVerifier
    {
        // Verify the signature of an XML file against an asymmetric
        // algorithm and return the result.
        public static bool VerifyXmlSignature(XmlDocument xmlDoc, RSA key)
        {
            // Check arguments.
            if (xmlDoc == null)
                throw new ArgumentException(null, nameof(xmlDoc));
            if (key == null)
                throw new ArgumentException(null, nameof(key));

            // Create a new SignedXml object and pass it
            // the XML document class.
            SignedXml signedXml = new(xmlDoc);

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Signature");

            // Throw an exception if no signature was found.
            if (nodeList.Count <= 0)
            {
                throw new CryptographicException("Verification failed: No Signature was found in the document.");
            }

            // This example only supports one signature for
            // the entire XML document.  Throw an exception
            // if more than one signature was found.
            if (nodeList.Count >= 2)
            {
                throw new CryptographicException("Verification failed: More that one signature was found for the document.");
            }

            // Load the first <signature> node.
            signedXml.LoadXml((XmlElement?)nodeList[0]);

            // Check the signature and return the result.
            var isSigned = signedXml.CheckSignature(key);
            return isSigned;
        }
    }
}
