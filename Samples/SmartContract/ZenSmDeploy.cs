using CommonInterfaces;
using Nethereum.Geth;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZenSmDeploy
{
    /**
    *
    * Zenodys Visual element that deploys smart contract.
    * For more information about Zenodys elements for managed engine implementation refer to /DotNet/Action/ZenAction.cs
    * Element code is for demonstrating purposes and will be upgraded for production use.
    * Integration between Zenodys platform and Ethereum was done via great Nethereum library (https://github.com/Nethereum/Nethereum)
    */
    public class ZenSmDeploy : IZenAction, IZenNodeInit
    {
        #region Fields
        string _senderAddress;
        string _password;
        string _abi;
        string _defaultGas;
        string _providerUrl;
        int _unlockAccountDuration;
        ZenCsScriptData _scripts;
        object _syncCsScript = new object();
        #endregion

        #region IZenNodeInit implementations
        #region OnNodeInit
        /** 
        * Second in series of element callbacks.
        * Save visual element properties, parse and prepare dynamic calls for CONTRACT_CONSTRUCTOR_PARAMS property
        */
        public void OnNodeInit(Hashtable elements, IPlugin element)
        {
            // Provider url ("http://localhost:8545")
            _providerUrl = element.GetElementProperty("PROVIDER_URL");

            // Sender address. ("0x9812db3e6c072a9731267485bd1ce075ae11e6a8")
            _senderAddress = element.GetElementProperty("SENDER_ADDRESS");
            
            // Sender password. ("password")
            _password = element.GetElementProperty("SENDER_PASSWORD");

            // How much time will account be unlocked in seconds. (120)
            _unlockAccountDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            
            // Default gas value. ("290000") 
            _defaultGas = element.GetElementProperty("DEFAUT_GAS"); 

            // Smart contract ABI ([{""constant"":false,""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""name"":""saveConsumptions"",""outputs"":[],
            //                      ""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":false,""inputs"":[],
            //                      ""name"":""getConsumptions"",""outputs"":[{""name"":""sumConsumption"",""type"":""int256""}],""payable"":false,
            //                      ""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""payable"":false,""stateMutability"":""nonpayable"",
            //                      ""type"":""constructor""}])
            _abi = element.GetElementProperty("ABI");
            
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
            Task<string> deployTask = Task.Run(async () => await DeployContract().ConfigureAwait(false));
            deployTask.Wait();

            element.LastResultBoxed = deployTask.Result;
            element.IsConditionMet = true;
        }
        #endregion
        #endregion
        #endregion

        #region Private functions
        #region DeployContract
        async Task<string> DeployContract()
        {
            var web3 = new Web3.Web3(_providerUrl);
            web3.TransactionManager.DefaultGas =  BigInteger.Parse(_defaultGas);
            web3.TransactionManager.DefaultGasPrice = Nethereum.Signer.Transaction.DEFAULT_GAS_PRICE;

            var unlockAccountResult = await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, _unlockAccountDuration);
            var web3Geth = new Web3Geth(web3.Client);
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, _senderAddress, GetSmartContractArgs());
            var mineResult = await web3Geth.Miner.Start.SendRequestAsync(6);
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            while (receipt == null)
            {
                Thread.Sleep(2000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            mineResult = await web3Geth.Miner.Stop.SendRequestAsync();
            return receipt.ContractAddress;
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
        // Parse "result" tags and create assembly for dynamically getting results from elements, that are then input args to smart contract constructor
        void InitializeScript(Hashtable elements, IPlugin element)
        {
            lock (_syncCsScript)
            {
                if (_scripts == null)
                {
                    string sFunctions = string.Empty;
                    foreach (string args in Regex.Split(element.GetElementProperty("CONTRACT_CONSTRUCTOR_PARAMS"), "#100#"))
                        sFunctions += ZenCsScriptCore.GetFunction("return " + elements + ";");

                    _scripts = ZenCsScriptCore.Initialize(sFunctions, elements, element, 
                               Path.Combine("tmp", "SmartContractDeploy", element.ID + ".zen"), null, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }
        }
        #endregion
        #endregion
    }
}