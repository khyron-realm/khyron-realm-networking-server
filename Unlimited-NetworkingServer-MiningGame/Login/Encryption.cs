using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Unlimited_NetworkingServer_MiningGame.Login
{
    internal static class Encryption
    {
        /// <summary>
        /// Decrypts data stream with the selected private key
        /// </summary>
        /// <param name="input">Input byte stream</param>
        /// <param name="key">Private decryption key</param>
        /// <returns></returns>
        public static string Decrypt(byte[] input, string key)
        {
            byte[] decrypted;
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                FromXmlString(rsa, key);
                decrypted = rsa.Decrypt(input, true);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
        
        /// <summary>
        /// Initializes an RSA object from the key information from an XML string
        /// </summary>
        /// <param name="rsa">RSACryptoServiceProvider object</param>
        /// <param name="xmlString">The XML string containing RSA key information</param>
        /// <exception cref="Exception">Invalid RSA key exception</exception>
        public static void FromXmlString(this RSACryptoServiceProvider rsa, string xmlString)
        {
            var parameters = new RSAParameters();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement != null && xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus":     parameters.Modulus =    Convert.FromBase64String(node.InnerText); break;
                        case "Exponent":    parameters.Exponent =   Convert.FromBase64String(node.InnerText); break;
                        case "P":           parameters.P =          Convert.FromBase64String(node.InnerText); break;
                        case "Q":           parameters.Q =          Convert.FromBase64String(node.InnerText); break;
                        case "DP":          parameters.DP =         Convert.FromBase64String(node.InnerText); break;
                        case "DQ":          parameters.DQ =         Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ":    parameters.InverseQ =   Convert.FromBase64String(node.InnerText); break;
                        case "D":           parameters.D =          Convert.FromBase64String(node.InnerText); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        /// <summary>
        /// Creates and returns an XML string containing the key of the current RSA object
        /// </summary>
        /// <param name="rsa">RSACryptoServiceProvider object</param>
        /// <returns>An XML string containing the key of the current RSA object</returns>
        public static string ToXmlString(this RSACryptoServiceProvider rsa)
        {
            var parameters = rsa.ExportParameters(true);

            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                Convert.ToBase64String(parameters.Modulus),
                Convert.ToBase64String(parameters.Exponent),
                Convert.ToBase64String(parameters.P),
                Convert.ToBase64String(parameters.Q),
                Convert.ToBase64String(parameters.DP),
                Convert.ToBase64String(parameters.DQ),
                Convert.ToBase64String(parameters.InverseQ),
                Convert.ToBase64String(parameters.D));
        }
    }
}