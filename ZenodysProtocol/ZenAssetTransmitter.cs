using CommonInterfaces;
using Nethereum.Web3;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ZenAssetTransmitter
{
    public class ZenAssetTransmitter : IZenAction, IZenElementInit
    {
        #region Constants
        #region ABI
        const string ABI = @"[{""constant"":true,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""}],""name"":""checkLicence"",""outputs"":[{""name"":""licValid"",""type"":""bool""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getPublicKey"",""outputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""}],""name"":""confirmTransaction"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""bytes32""}],""name"":""_licences"",""outputs"":[{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""name"":""addLicence"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""}]";
        #endregion

        #region METADATA
        const string METADATA =
                "<AssetInfo>" +
                    "<Cryptography>" +
                        "<Encrypted>True</Encrypted>" +
                        "<KeyEncryption algorithm='RSA2048'></KeyEncryption>" +
                        "<DataEncryption algorithm='AES128'>" +
                            "<AESEncryptedKeyValue>" +
                                "<Key/>" +
                                "<IV/>" +
                            "</AESEncryptedKeyValue>" +
                        "</DataEncryption>" +
                        "<DataSignature algorithm='HMACSHA256'>" +
                            "<Value />" +
                            "<EncryptedKey />" +
                        "</DataSignature>" +
                    "</Cryptography>" +
                    "<DataSource />" +
                    "<ResultUnit />" +
                    "<SystemType />" +
                "</AssetInfo>";
        #endregion

        #region LICENCE_POSITION
        const int LICENCE_POSITION = 0;
        #endregion

        #region PUB_KEY_POSITION
        const int PUB_KEY_POSITION = 1;
        #endregion

        #region CALLBACK_IP_POSITION
        const int CALLBACK_IP_POSITION = 2;
        #endregion  

        #region CALLBACK_PORT_POSITION
        const int CALLBACK_PORT_POSITION = 3;
        #endregion

        #region CONFIRM_TRANSACTION_FUNCTION
        const string CONFIRM_TRANSACTION_FUNCTION = "confirmTransaction";
        #endregion
        #endregion

        #region Fields
        #region _web3
        Web3 _web3;
        #endregion

        #region _contractAddress
        string _contractAddress;
        #endregion

        #region _ethProviderUrl
        string _ethProviderUrl;
        #endregion

        #region _ownerAddress
        string _ownerAddress;
        #endregion

        #region _ownerPassword
        string _ownerPassword;
        #endregion

        #region _unlockDuration
        int _unlockDuration;
        #endregion
        #endregion

        #region IZenElementInit implementations
        #region OnElementInit
        public void OnElementInit(Hashtable elements, IElement element)
        {
            _ethProviderUrl = element.GetElementProperty("ETH_PROVIDER_URL");
            _ownerAddress = element.GetElementProperty("OWNER_ADDRESS");
            _ownerPassword = element.GetElementProperty("OWNER_PASSWORD");
            _unlockDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            _contractAddress = element.GetElementProperty("CONTRACT_ADDRESS");
            _web3 = new Web3(_ethProviderUrl);
        }
        #endregion
        #endregion

        #region IZenAction Implementations
        #region Properties
        #region ID
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

        #region Functions
        #region ExecuteAction
        public void ExecuteAction(Hashtable elements, IElement element, IElement iAmStartedYou)
        {
            EncryptAndTransmit(element, elements);
            //ConfirmTransaction(element, elements);
        }
        #endregion
        #endregion
        #endregion

        #region Functions
        #region GenerateRandom
        byte[] GenerateRandom(int length)
        {
            byte[] bytes = new byte[length];
            using (RNGCryptoServiceProvider random = new RNGCryptoServiceProvider())
            {
                random.GetBytes(bytes);
            }
            return bytes;
        }
        #endregion

        #region RSAEncryptBytes
        byte[] RSAEncryptBytes(byte[] data, string keyXml)
        {
            byte[] encrypted = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(keyXml);
                encrypted = rsa.Encrypt(data, true);
            }
            return encrypted;
        }
        #endregion

        #region GetMetadata
        string GetMetadata(IElement element, string systemType, string signature, byte[] signatureKey, byte[] encryptionKey, byte[] encryptionIv, string rsaKey)
        {
            XDocument doc = XDocument.Parse(METADATA);

            doc.Descendants("AssetInfo").Single()
                .Descendants("Cryptography").Single()
                .Descendants("DataEncryption").Single()
                .Descendants("AESEncryptedKeyValue").Single()
                .Descendants("Key").Single().Value = Convert.ToBase64String(RSAEncryptBytes(encryptionKey, rsaKey));

            doc.Descendants("AssetInfo").Single()
                .Descendants("Cryptography").Single()
                .Descendants("DataEncryption").Single()
                .Descendants("AESEncryptedKeyValue").Single()
                .Descendants("IV").Single().Value = Convert.ToBase64String(RSAEncryptBytes(encryptionIv, rsaKey));

            doc.Descendants("AssetInfo").Single()
                .Descendants("Cryptography").Single()
                .Descendants("DataSignature").Single()
                .Descendants("Value").Single().Value = signature;

            doc.Descendants("AssetInfo").Single()
               .Descendants("Cryptography").Single()
               .Descendants("DataSignature").Single()
               .Descendants("EncryptedKey").Single().Value = Convert.ToBase64String(RSAEncryptBytes(signatureKey, rsaKey));

            doc.Descendants("AssetInfo").Single()
               .Descendants("DataSource").Single().Value = element.ResultSource == null ? "N/A" : element.ResultSource;

            doc.Descendants("AssetInfo").Single()
               .Descendants("ResultUnit").Single().Value = element.ResultUnit == null ? "N/A" : element.ResultUnit;

            doc.Descendants("AssetInfo").Single()
               .Descendants("SystemType").Single().Value = systemType;

            return doc.ToString();
        }
        #endregion

        #region CalculateSignature
        byte[] CalculateSignature(MemoryStream ms, byte[] key)
        {
            byte[] sig = null;
            using (HMACSHA256 sha = new HMACSHA256(key))
            {
                sig = sha.ComputeHash(ms);
            }

            return sig;
        }
        #endregion

        #region SetTypeAndResult
        void SetTypeAndResult(IElement element, Hashtable elements, ref byte[] plainResult, ref string systemType)
        {
            object result = (elements[element.GetElementProperty("DATA_SOURCE_ELEMENT")] as IElement).LastResultBoxed;
            systemType = result.GetType().Name;
            switch (systemType.ToLower())
            {
                case "bool":
                    plainResult = BitConverter.GetBytes(Convert.ToBoolean(result));
                    break;

                case "ushort":
                case "uint16":
                    plainResult = BitConverter.GetBytes(Convert.ToUInt16(result));
                    break;

                case "int":
                case "int32":
                    plainResult = BitConverter.GetBytes(Convert.ToInt32(result));
                    break;

                case "uint":
                    plainResult = BitConverter.GetBytes(Convert.ToUInt32(result));
                    break;

                case "string":
                    plainResult = Encoding.UTF8.GetBytes(result.ToString());
                    break;

                case "double":
                    plainResult = BitConverter.GetBytes(Convert.ToDouble(result));
                    break;
            }
        }
        #endregion

        #region GetLicenceCheckResult
        string GetLicenceCheckResult(IElement element, Hashtable elements, int position)
        {
            return (elements[element.GetElementProperty("LICENCE_VERIFY_ELEMENT")] as IElement)
                                        .LastResultBoxed.ToString().Split(';')[position];
        }
        #endregion

        #region Encrypt
        void Encrypt(IElement element, Hashtable elements, ref byte[] metadata, ref byte[] cipherBytes)
        {
            byte[] plainResult = null;
            string systemType = string.Empty;
            byte[] encryptionKey = GenerateRandom(16);
            byte[] encryptionIV = GenerateRandom(16);
            byte[] signatureKey = GenerateRandom(64);
            string customerPublicKey = GetLicenceCheckResult(element, elements, PUB_KEY_POSITION);

            SetTypeAndResult(element, elements, ref plainResult, ref systemType);
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                //aes.KeySize = 128;
                aes.Key = encryptionKey;
                aes.IV = encryptionIV;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream encrypted = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(encrypted, encryptor, CryptoStreamMode.Write))
                    {
                        // Encrypt the input plaintext string
                        cs.Write(plainResult, 0, plainResult.Length);

                        // Complete the encryption process
                        cs.FlushFinalBlock();

                        // Convert the encrypted data from a MemoryStream to a byte array
                        cipherBytes = encrypted.ToArray();

                        //Calculate signature
                        byte[] signature = CalculateSignature(encrypted, signatureKey);

                        //Create manifest
                        metadata = Encoding.UTF8.GetBytes(GetMetadata(element, systemType, Convert.ToBase64String(signature), signatureKey, encryptionKey, encryptionIV, customerPublicKey));
                    }
                }
            }
        }
        #endregion

        #region EncryptAndTransmit
        void EncryptAndTransmit(IElement element, Hashtable elements)
        {
            byte[] metadata = null;
            byte[] cipherBytes = null;
            Encrypt(element, elements, ref metadata, ref cipherBytes);

            byte[] metadataLength = BitConverter.GetBytes(metadata.Length);
            byte[] final = new byte[metadataLength.Length + metadata.Length + cipherBytes.Length];

            // Add metadata length info
            Buffer.BlockCopy(metadataLength,
                0,
                final,
                0,
                metadataLength.Length * sizeof(byte));

            // Add metadata
            Buffer.BlockCopy(metadata,
                0,
                final,
                metadataLength.Length * sizeof(byte),
                metadata.Length * sizeof(byte));

            // Add asset
            Buffer.BlockCopy(cipherBytes,
                0,
                final,
                metadataLength.Length * sizeof(byte) + metadata.Length * sizeof(byte),
                cipherBytes.Length * sizeof(byte));

            try
            {
                using (TcpClient client = new TcpClient(GetLicenceCheckResult(element, elements, CALLBACK_IP_POSITION),
                                                        Convert.ToInt32(GetLicenceCheckResult(element, elements, CALLBACK_PORT_POSITION))))
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(final, 0, final.Length);
                    client.Close();
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }
        #endregion

        #region ConfirmTransaction
        async void ConfirmTransaction(IElement element, Hashtable elements)
        {
            var unlockAccountResult = await _web3.Personal.UnlockAccount.SendRequestAsync(_ownerAddress,
                                                        _ownerPassword, _unlockDuration);

            bool transactionConfirmStatus = await _web3.Eth.GetContract(ABI, _contractAddress)
                                        .GetFunction(CONFIRM_TRANSACTION_FUNCTION)
                                        .CallAsync<bool>(GetLicenceCheckResult(element, elements, LICENCE_POSITION));
        }
        #endregion
        #endregion
    }
}

