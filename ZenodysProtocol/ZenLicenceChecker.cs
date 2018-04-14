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
using System.Threading;

namespace ZenLicenceChecker
{
    public class ZenLicenceChecker : IZenEvent, IZenElementInit
    {
        #region Constants
        const string ABI = @"[{""constant"":true,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""}],""name"":""checkLicence"",""outputs"":[{""name"":""licValid"",""type"":""bool""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getPublicKey"",""outputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""}],""name"":""confirmTransaction"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""bytes32""}],""name"":""_licences"",""outputs"":[{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""licenceId"",""type"":""bytes32""},{""name"":""customer"",""type"":""address""},{""name"":""price"",""type"":""uint256""},{""name"":""quantity"",""type"":""uint256""}],""name"":""addLicence"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""publicKey"",""type"":""string""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""}]";

        #region LICENCE_CHECK_FUNCTION
        const string LICENCE_CHECK_FUNCTION = "checkLicence";
        #endregion

        #region LENGTH_INFOS_SIZE
        const int LENGTH_INFOS_SIZE = 4;
        #endregion

        #region FRAME_CNT
        const int FRAME_CNT = 2;
        #endregion
        #endregion

        #region Fields
        #region _element
        IElement _element;
        #endregion

        #region _serverPort
        int _serverPort;
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

        #region _web3
        Web3 _web3;
        #endregion

        #endregion

        #region IZenElementInit implementations
        #region OnElementInit
        public void OnElementInit(Hashtable elements, IElement element)
        {
            _serverPort = Convert.ToInt32(element.GetElementProperty("SERVER_PORT"));
            _ethProviderUrl = element.GetElementProperty("ETH_PROVIDER_URL");
            _ownerAddress = element.GetElementProperty("OWNER_ADDRESS");
            _ownerPassword = element.GetElementProperty("OWNER_PASSWORD");
            _unlockDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            _contractAddress = element.GetElementProperty("CONTRACT_ADDRESS");

            _web3 = new Web3(_ethProviderUrl);
            _element = element;
            new Thread(WaitRequest).Start();
        }
        #endregion
        #endregion

        #region IZenEvent Implementations
        #region Properties
        #region ID
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

        #region Events
        #region ModuleEvent
        public event ModuleEventHandler ModuleEvent;
        #endregion
        #endregion

        #region Functions
        #region CheckInterruptCondition
        public bool CheckInterruptCondition(ModuleEventData eventData, IElement element, Hashtable plugins)
        {
            element.LastResultBoxed = eventData.Tag;
            return true;
        }
        #endregion
        #endregion
        #endregion

        #region Private functions
        #region Decrypt
        string Decrypt(byte[] encryptedData)
        {
            string decrpytedText = string.Empty;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(File.ReadAllText("keystore/private.xml"));
                var decryptedBytes = rsa.Decrypt(encryptedData, true);
                decrpytedText = Encoding.UTF8.GetString(decryptedBytes);
            }
            return decrpytedText;
        }
        #endregion

        #region GetRequestBuffer
        byte[] GetRequestBuffer(Stream stream)
        {
            byte[] request = null;
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                request = ms.ToArray();
            }
            return request;
        }
        #endregion

        #region WaitRequest
        async void WaitRequest()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), _serverPort);
            server.Start();
            Console.WriteLine("Start listening for asset requests on port {0}.", _serverPort);

            while (true)
            {
                byte[] rawRequest = null;
                string clientIP = string.Empty;

                using (TcpClient client = server.AcceptTcpClient())
                {
                    NetworkStream stream = client.GetStream();
                    rawRequest = GetRequestBuffer(stream);
                    clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    client.Close();
                }

                int licenceIdLength = BitConverter.ToInt32(rawRequest, 0 * LENGTH_INFOS_SIZE);
                int signatureLength = BitConverter.ToInt32(rawRequest, 1 * LENGTH_INFOS_SIZE);
                int publicKeyLength = BitConverter.ToInt32(rawRequest, 2 * LENGTH_INFOS_SIZE);
                int callbackUrlLength = BitConverter.ToInt32(rawRequest, 3 * LENGTH_INFOS_SIZE);

                byte[] licenceIdRaw = new byte[licenceIdLength];
                byte[] signatureRaw = new byte[signatureLength];
                byte[] publicKeyRaw = new byte[publicKeyLength];
                byte[] callbackPortRaw = new byte[callbackUrlLength];

                Array.Copy(rawRequest, 4 * LENGTH_INFOS_SIZE, licenceIdRaw, 0, licenceIdLength);

                Array.Copy(rawRequest, 4 * LENGTH_INFOS_SIZE + licenceIdLength, signatureRaw,
                           0, signatureLength);

                Array.Copy(rawRequest, 4 * LENGTH_INFOS_SIZE + licenceIdLength + signatureLength,
                    publicKeyRaw, 0, publicKeyLength);

                Array.Copy(rawRequest, 4 * LENGTH_INFOS_SIZE + licenceIdLength + signatureLength + publicKeyLength,
                    callbackPortRaw, 0, callbackUrlLength);

                string licenceId = Encoding.Default.GetString(licenceIdRaw);
                // Decrypt signed licenceId with asset owner private key
                string signature = Decrypt(signatureRaw);
                string customerPubKey = Encoding.Default.GetString(publicKeyRaw);
                string callbackPort = Encoding.Default.GetString(callbackPortRaw);

                // Get address from validation process
                var address = new MessageSigner().HashAndEcRecover(licenceId, signature);

                var unlockAccountResult = await _web3.Personal.UnlockAccount.SendRequestAsync(_ownerAddress,
                                                _ownerPassword, _unlockDuration);

                // Validate smart contract if provided address has access to asset which belongs to current licenceId
                bool isLicenceValid = await _web3.Eth.GetContract(ABI, _contractAddress)
                                            .GetFunction(LICENCE_CHECK_FUNCTION)
                                            .CallAsync<bool>(licenceId, address);

                Console.WriteLine("Licence: {0}; valid: {1}, ip: {2}", licenceId, isLicenceValid.ToString(), clientIP);
                // If verification succed, trigger element complete event and save licenceId, 
                // customer public key and callback url
                // Parameters will be needed in other elements
                if (isLicenceValid && ModuleEvent != null)
                    ModuleEvent(this, new ModuleEventData(_element.ID, string.Empty, string.Concat(licenceId, ";",
                                                                            customerPubKey, ";", clientIP, ";", callbackPort)));
            }
        }
        #endregion
        #endregion
    }
}