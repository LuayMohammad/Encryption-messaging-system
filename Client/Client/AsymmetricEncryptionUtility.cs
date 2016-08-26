using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

using System.IO;

namespace RSAExample
{
    class AsymmetricEncryptionUtility
    {
        public string GenerateKey(string targetFile) {
            RSACryptoServiceProvider Algorithm = new RSACryptoServiceProvider(4096);
            // Save the private key
            string CompleteKey = Algorithm.ToXmlString(true);
            Algorithm.PersistKeyInCsp = true;
            byte[] KeyBytes = Encoding.UTF8.GetBytes(CompleteKey);
           /* KeyBytes = ProtectedData.Protect(KeyBytes,
            null, DataProtectionScope.LocalMachine);*/
            using (FileStream fs = new FileStream(@"D:\SERVER_TEST\"+targetFile+"PrivateKey.xml", FileMode.Create))
            {
                fs.Write(KeyBytes, 0, KeyBytes.Length);
                fs.Close();
            }
            using (FileStream fs = new FileStream(@"D:\SERVER_TEST\"+targetFile + "PublicKey.xml", FileMode.Create))
            {
               string pk= Algorithm.ToXmlString(false);
               byte[] d = Encoding.UTF8.GetBytes(pk);
                fs.Write(d, 0, d.Length);
                fs.Close();
            }
            // Return the public key
            return Algorithm.ToXmlString(false);
        }
        private void ReadKey(RSACryptoServiceProvider algorithm, string keyFile)
        {
           byte[] KeyBytes;
        FileStream fs = new FileStream(@"D:\SERVER_TEST\"+keyFile, FileMode.Open);
 
               KeyBytes = new byte[fs.Length];
               fs.Read(KeyBytes, 0, (int)fs.Length);
               algorithm.FromXmlString(Encoding.UTF8.GetString(KeyBytes));
               fs.Close();
        }
        public byte[] EncryptData(byte[] data, string publicKey)
        {
            // Create the algorithm based on the public key
            RSACryptoServiceProvider Algorithm = new RSACryptoServiceProvider(4096);
            Algorithm.FromXmlString(publicKey);
            // Now encrypt the data
            return Algorithm.Encrypt(
            data, true);
        }
        public byte[] DecryptbyteData(byte[] data, string keyFile)
        {
            RSACryptoServiceProvider Algorithm = new RSACryptoServiceProvider(4096);
            ReadKey(Algorithm, keyFile);
            byte[] ClearData = Algorithm.Decrypt(data, true);
            return ClearData;
        }
    
    }

}
