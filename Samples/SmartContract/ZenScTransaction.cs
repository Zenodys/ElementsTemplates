using CommonInterfaces;
using System.Linq;
using Nethereum.Web3;
using System;
using System.Collections;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.KeyStore;
using Nethereum.Hex.HexConvertors.Extensions;

namespace ZenScTransaction
{
    /**
    *
    * Zenodys Visual element that sends transaction to deployed smart contract.
    * For more information about Zenodys elements for managed engine implementation refer to /DotNet/Action/ZenAction.cs
    * Element code is for demonstrating purposes and will be upgraded for production use.
    * Integration between Zenodys platform and Ethereum was done via great Nethereum library (https://github.com/Nethereum/Nethereum)
    */
    public class ZenScTransaction : IZenAction, IZenNodeInit
    {
        #region Fields
        string _abi;
        string _contractAddress;
        string _functionName;
        string _providerUrl;
        string _defaultGas;
        string _messageValue;
        string _privateKey;
        string _address;
        ZenCsScriptData _scripts;
        object _syncCsScript = new object();
        #endregion

        #region IZenNodeInit implementations
        #region OnNodeInit
        /** 
        * Second in series of element callbacks.
        * Save visual element properties, parse and prepare dynamic calls for CONTRACT_PARAMS property
        */
        public void OnNodeInit(Hashtable elements, IPlugin element)
        {
            // Provider url ("http://localhost:8545")
            _providerUrl = element.GetElementProperty("PROVIDER_URL");

            // Message value ("0"). Number of wei sent with the message.
            _messageValue = element.GetElementProperty("MESSAGE_VALUE");

            // Default gas value. ("290000") 
            _defaultGas = string.IsNullOrEmpty(element.GetElementProperty("DEFAUT_GAS")) ? 
                        "290000" : 
                        element.GetElementProperty("DEFAUT_GAS");

            // Smart contract address ("0x97a93e68fa58513facb2b702a97597cab97afd6f") 
            _contractAddress = element.GetElementProperty("SMART_CONTRACT_ADDRESS");

            // Smart contract ABI ([{""constant"":false,""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""name"":""saveConsumptions"",""outputs"":[],
            //                      ""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":false,""inputs"":[],
            //                      ""name"":""getConsumptions"",""outputs"":[{""name"":""sumConsumption"",""type"":""int256""}],""payable"":false,
            //                      ""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""payable"":false,""stateMutability"":""nonpayable"",
            //                      ""type"":""constructor""}])
            _abi = ZenCsScriptCore.Decode(element.GetElementProperty("ABI"));

            // Name of smart contract function to be called ("saveConsumptions")
            _functionName = element.GetElementProperty("FUNCTION_NAME");

            // Get private key from keystore file
            var json = File.OpenText(element.GetElementProperty("KEYSTORE_FILE")).ReadToEnd();
            var service = new KeyStoreService();
            _privateKey = service.DecryptKeyStoreFromJson(element.GetElementProperty("KEYSTORE_FILE_PASSWORD"), json).ToHex();
            _address = service.GetAddressFromKeyStore(json);

            // Parse result tags (<result>Element Id</result>) defined by user and dynamically create assembly that will query element results.
            InitializeScript(elements, element);
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
        /**
        * Last in series of element callbacks. For more information refer to /DotNet/Action/ZenAction.cs.
        * Here goes main element logic.
        */
        public void ExecuteAction(Hashtable elements, IPlugin element, IPlugin iAmStartedYou)
        {
            // Get current element results that are then passed as arguments to smart contract transaction
            object[] args = GetSmartContractArgs();

            Task<string> task = Task.Run(async () => await SendTransactionToContract(args).ConfigureAwait(false));
            task.Wait();

            // Result of the task is transaction hash  ("0x184c1faa9f3e11d56243f8aaaeb2238c4861f5049293ddeb675b20eaaf4fdffb")
            // Store it to the element so that can be used by other visual elements inside project
            element.LastResultBoxed = task.Result;
            element.IsConditionMet = true;
        }
        #endregion
        #endregion
        #endregion

        #region Private functions
        #region SendTransactionToContract
        async Task<string> SendTransactionToContract(params object[] values)
        {
            Web3 web3 = new Nethereum.Geth.Web3Geth(new Account(_privateKey), _providerUrl);
            var transactionHash = await web3.Eth.GetContract(_abi, _contractAddress).GetFunction(_functionName)
                                        .SendTransactionAsync(_address, new HexBigInteger(BigInteger.Parse(_defaultGas)),
                                        new HexBigInteger(BigInteger.Parse(_messageValue)), values);

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }
            return receipt.TransactionHash;
        }
        #endregion

        #region GetSmartContractArgs
        object[] GetSmartContractArgs()
        {
            object[] args = new object[_scripts.ScriptDoc.DocumentNode.Descendants("code").Count()];
            for (int i = 0; i < _scripts.ScriptDoc.DocumentNode.Descendants("code").Count(); i++)
                args[i] = _scripts.ZenCsScript.RunCustomCode(_scripts.ScriptDoc.DocumentNode
                                              .Descendants("code").ElementAt(i).Attributes["id"].Value);

            return args;
        }
        #endregion

        #region InitializeScript
        // Parse "result" tags and create assembly for dynamically getting results from elements, that are then input args to smart contract function 
        void InitializeScript(Hashtable elements, IPlugin element)
        {
            lock (_syncCsScript)
            {
                if (_scripts == null)
                {
                    string sFunctions = string.Empty;
                    foreach (string args in Regex.Split(ZenCsScriptCore.Decode(element.GetElementProperty("SCTRANSACTION_PARAMETERS")), "¨"))
                    {
                        if (!string.IsNullOrEmpty(args))
                            sFunctions += ZenCsScriptCore.GetFunction("return " + args + ";");
                    }
                    _scripts = ZenCsScriptCore.Initialize(sFunctions, elements, element,
                                                        Path.Combine("tmp", "SmartContractTransaction", element.ID + ".zen"),
                                                        ParentBoard, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }
        }
        #endregion
        #endregion
    }
}