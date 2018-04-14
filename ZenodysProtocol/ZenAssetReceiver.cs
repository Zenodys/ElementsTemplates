using CommonInterfaces;
using Nethereum.Signer;
using Nethereum.Web3;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ZenAssetReceiver
{
    public class ZenAssetReceiver : IZenAction, IZenElementInit
    {
        #region Constants
        #region ABI
        const string ABI = @"[{""constant"":true,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""}],""name"":""checkLicence"",""outputs"":[{""name"":""licValid"",""type"":""bool""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getPublicKey"",""outputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""}],""name"":""confirmTransaction"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""bytes32""}],""name"":""_licences"",""outputs"":[{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""name"":""addLicence"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""}]";
        #endregion

        #region AES_KEY_XML_PATH
        const string AES_KEY_XML_PATH = "/AssetInfo/Cryptography/DataEncryption/AESEncryptedKeyValue/Key";
        #endregion

        #region AES_IV_XML_PATH
        const string AES_IV_XML_PATH = "/AssetInfo/Cryptography/DataEncryption/AESEncryptedKeyValue/IV";
        #endregion

        #region ASSET_SYSTEM_TYPE_PATH
        const string ASSET_SYSTEM_TYPE_PATH = "/AssetInfo/SystemType";
        #endregion

        #region GET_PUBLIC_KEY_FUNCTION
        const string GET_PUBLIC_KEY_FUNCTION = "getPublicKey";
        #endregion

        #region ASSET_ENVELOPE_POSITION
        const int ASSET_ENVELOPE_POSITION = 1;
        #endregion

        #region METADATA_LENGTH_INFO_SIZE
        const int METADATA_LENGTH_INFO_SIZE = 4;
        #endregion
        #endregion

        #region Fields
        #region _web3
        Web3 _web3;
        #endregion

        #region _localPort
        int _localPort;
        #endregion

        #region _licenceId
        string _licenceId;
        #endregion

        #region _contractAddress
        string _contractAddress;
        #endregion

        #region _ethProviderUrl
        string _ethProviderUrl;
        #endregion

        #region _serverIp
        string _serverIp;
        #endregion

        #region _serverPort
        int _serverPort;
        #endregion

        #region _ownerAddress
        string _ownerAddress;
        #endregion

        #region _privateKey
        string _privateKey;
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
        public void OnElementInit(Hashtable eleemnts, IElement element)
        {
            _serverIp = element.GetElementProperty("SERVER_IP");
            _serverPort = Convert.ToInt32(element.GetElementProperty("SERVER_PORT"));
            _privateKey = element.GetElementProperty("PRIVATE_KEY");
            _localPort = Convert.ToInt32(element.GetElementProperty("LOCAL_PORT"));
            _ethProviderUrl = element.GetElementProperty("ETH_PROVIDER_URL");
            _ownerAddress = element.GetElementProperty("OWNER_ADDRESS");
            _ownerPassword = element.GetElementProperty("OWNER_PASSWORD");
            _unlockDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            _contractAddress = element.GetElementProperty("CONTRACT_ADDRESS");
            _licenceId = element.GetElementProperty("LICENCE_ID");
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
            SendAssetRequest();
            WaitAssetResponse(element);
            element.IsConditionMet = true;
        }
        #endregion
        #endregion
        #endregion

        #region Functions
        #region SetElementResult
        void SetElementResult(IElement element, string systemType, byte[] plainAsset)
        {
            //TODO: handle more types
            //TODO: save also metadata info 
            switch (systemType.ToLower())
            {
                case "int":
                case "int32":
                    element.LastResultBoxed = BitConverter.ToInt32(plainAsset, 0);
                    break;

                case "double":
                    element.LastResultBoxed = BitConverter.ToDouble(plainAsset, 0);
                    break;

                case "string":
                    element.LastResultBoxed = Encoding.UTF8.GetString(plainAsset);
                    break;
            }
        }
        #endregion

        #region ExtractMetadataAndAsset
        void ExtractMetadataAndAsset(byte[] rawAsset, ref string metadata, ref byte[] cyperBytes)
        {
            int metadataLength = BitConverter.ToInt32(rawAsset, 0);
            byte[] rawMetadata = new byte[metadataLength];
            Array.Copy(rawAsset, METADATA_LENGTH_INFO_SIZE, rawMetadata, 0, metadataLength);
            metadata = Encoding.Default.GetString(rawMetadata);

            cyperBytes = new byte[rawAsset.Length - METADATA_LENGTH_INFO_SIZE - metadataLength];
            Array.Copy(rawAsset, METADATA_LENGTH_INFO_SIZE + metadataLength, cyperBytes, 0, cyperBytes.Length);
        }
        #endregion

        #region DecryptAssetAndSetElementResult
        void DecryptAssetAndSetElementResult(string metadata, byte[] cyperBytes, IElement element)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                // Read private key from local keystore
                string rsaPrivateKey = File.ReadAllText(@"keystore/private.xml");

                // Decrypt aes key and IV
                XDocument doc = XDocument.Parse(metadata);
                XElement aesKeyElement = doc.Root.XPathSelectElement(AES_KEY_XML_PATH);
                byte[] aesKey = RSADescryptBytes(Convert.FromBase64String(aesKeyElement.Value), rsaPrivateKey);

                XElement aesIvElement = doc.Root.XPathSelectElement(AES_IV_XML_PATH);
                byte[] aesIv = RSADescryptBytes(Convert.FromBase64String(aesIvElement.Value), rsaPrivateKey);

                // Decrypt asset
                var decryptor = aes.CreateDecryptor(aesKey, aesIv);
                byte[] plainAsset = decryptor.TransformFinalBlock(cyperBytes, 0, cyperBytes.Length);

                // Get asset system type and set result to element
                XElement assetSystemType = doc.Root.XPathSelectElement(ASSET_SYSTEM_TYPE_PATH);
                SetElementResult(element, assetSystemType.Value, plainAsset);
            }
        }
        #endregion

        #region WaitAssetResponse()
        void WaitAssetResponse(IElement element)
        {
            string metadata = string.Empty;
            byte[] cyperBytes = null;
            byte[] rawAsset = null;
            try
            {
                TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), _localPort);
                server.Start();

                using (TcpClient client = server.AcceptTcpClient())
                {
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[16 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        rawAsset = ms.ToArray();
                    }
                    client.Close();
                }
                server.Stop();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            ExtractMetadataAndAsset(rawAsset, ref metadata, ref cyperBytes);
            DecryptAssetAndSetElementResult(metadata, cyperBytes, element);
        }
        #endregion

        #region SendAssetRequest
        void SendAssetRequest()
        {
            try
            {
                TcpClient client = new TcpClient(_serverIp, _serverPort);
                NetworkStream stream = client.GetStream();
                byte[] assetBuffer = AssetBuffer;
                stream.Write(assetBuffer, 0, assetBuffer.Length);
                client.Close();
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

        #region GetSellerPublicKey
        async Task<string> GetSellerPublicKey()
        {
            var unlockAccountResult = await _web3.Personal.UnlockAccount.SendRequestAsync(_ownerAddress,
                                                            _ownerPassword, _unlockDuration);

            return await _web3.Eth.GetContract(ABI, _contractAddress)
                                        .GetFunction(GET_PUBLIC_KEY_FUNCTION)
                                        .CallAsync<string>();
        }
        #endregion

        #region RSADescryptBytes
        byte[] RSADescryptBytes(byte[] datas, string keyXml)
        {
            byte[] decrypted = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(keyXml);
                decrypted = rsa.Decrypt(datas, true);
            }

            return decrypted;
        }
        #endregion
        #endregion

        #region Properties
        #region AssetBuffer
        byte[] AssetBuffer
        {
            get
            {
                byte[] licenceId = Encoding.UTF8.GetBytes(_licenceId);
                byte[] licenceIdLength = BitConverter.GetBytes(licenceId.Length);

                byte[] signature = EncryptedSignature;
                byte[] signatureLength = BitConverter.GetBytes(signature.Length);

                byte[] publicKey = File.ReadAllBytes(@"keystore/public.xml");
                byte[] publicKeyLength = BitConverter.GetBytes(publicKey.Length);

                byte[] callbackPort = Encoding.UTF8.GetBytes(_localPort.ToString());
                byte[] callbackPortLength = BitConverter.GetBytes(callbackPort.Length);

                byte[] assetBuffer = new byte[licenceId.Length + signature.Length + publicKey.Length + callbackPort.Length + 16];

                Buffer.BlockCopy(licenceIdLength,
                    0,
                    assetBuffer,
                    0,
                    licenceIdLength.Length * sizeof(byte));

                Buffer.BlockCopy(signatureLength,
                    0,
                    assetBuffer,
                    4,
                    signatureLength.Length * sizeof(byte));

                Buffer.BlockCopy(publicKeyLength,
                    0,
                    assetBuffer,
                    8,
                    signatureLength.Length * sizeof(byte));

                Buffer.BlockCopy(callbackPortLength,
                    0,
                    assetBuffer,
                    12,
                    publicKeyLength.Length * sizeof(byte));

                Buffer.BlockCopy(licenceId,
                    0,
                    assetBuffer,
                    16,
                    licenceId.Length * sizeof(byte));

                Buffer.BlockCopy(signature,
                    0,
                    assetBuffer,
                    16 + licenceId.Length * sizeof(byte),
                    signature.Length * sizeof(byte));

                Buffer.BlockCopy(publicKey,
                    0,
                    assetBuffer,
                    16 + licenceId.Length * sizeof(byte) + signature.Length * sizeof(byte),
                    publicKey.Length * sizeof(byte));

                Buffer.BlockCopy(callbackPort,
                    0,
                    assetBuffer,
                    16 + licenceId.Length * sizeof(byte) + signature.Length * sizeof(byte) + publicKey.Length * sizeof(byte),
                    callbackPort.Length * sizeof(byte));

                return assetBuffer;
            }
        }
        #endregion

        #region EncryptedSignature
        byte[] EncryptedSignature
        {
            get
            {
                byte[] encryptedSignature;

                // Sign licenceId with RSA private key
                var signer = new MessageSigner();
                var signature = signer.HashAndSign(_licenceId, _privateKey);

                // Encrypt signature with seller's public key
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    try
                    {
                        rsa.FromXmlString(GetSellerPublicKey().Result);
                        encryptedSignature = rsa.Encrypt(Encoding.ASCII.GetBytes(signature), true);
                    }
                    finally
                    {
                        rsa.PersistKeyInCsp = false;
                    }
                }
                return encryptedSignature;
            }
        }
        #endregion
        #endregion
    }
}