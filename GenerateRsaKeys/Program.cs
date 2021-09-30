﻿using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;

namespace GenerateRsaKeys
{
    internal class Program
    {
        private const string PrivateKeyPath = @"PrivateKey.xml";
        private const string PublicKeyPath = @"PublicKey.xml";
        
        public static void Main(string[] args)
        {
            GenerateKeys();
            Console.WriteLine("New key files generated successfully.");
            Console.ReadLine();
        }

        public static void GenerateKeys()
        {
            if (File.Exists(PrivateKeyPath))
            {
                File.Delete(PrivateKeyPath);
                Console.WriteLine("Deleting the old private key file");
            }

            if (File.Exists(PublicKeyPath))
            {
                File.Delete(PublicKeyPath);
                Console.WriteLine("Deleting the old public key file");
            }
            
            Console.WriteLine("Generating keys ...");

            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                
                var publicKey = rsa.ToXmlString(false);
                var privateKey = rsa.ToXmlString(true);
                
                File.WriteAllText(PublicKeyPath, publicKey);
                File.WriteAllText(PrivateKeyPath, privateKey);
            }
        }
    }
}