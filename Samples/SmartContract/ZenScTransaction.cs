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
        string _senderAddress;
        string _password;
        string _abi;
        string _contractAddress;
        string _functionName;
        string _providerUrl;
        string _defaultGas;
        int _unlockAccountDuration;
        string _byteCode;
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

            // Default gas value. ("290000") 
            _defaultGas = element.GetElementProperty("DEFAUT_GAS"); 
            
            // Transaction sender address. ("0x9812db3e6c072a9731267485bd1ce075ae11e6a8")
            _senderAddress = element.GetElementProperty("SENDER_ADDRESS");
            
            // Transaction sender password. ("password")
            _password = element.GetElementProperty("SENDER_PASSWORD");

            // How much time will account be unlocked in seconds. (120)
            _unlockAccountDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            
            // Smart contract address ("0x97a93e68fa58513facb2b702a97597cab97afd6f") 
            _contractAddress = element.GetElementProperty("SMART_CONTRACT_ADDRESS");
            
            // Smart contract byte code. ("0x6060604052341561000f57600080fd5b6040516040806101e083398101604052808051906020019091908051906020
            //                             019091905050336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffff
            //                             ffffffffffffffffffffffffffffffff1602179055508160018190555080600281905550505061014d80610093600039
            //                             6000f30060606040526004361061004c576000357c010000000000000000000000000000000000000000000000000000
            //                             0000900463ffffffff16806316772aff14610051578063b11ed2521461007d575b600080fd5b341561005c57600080fd
            //                             5b61007b60048080359060200190919080359060200190919050506100a6565b005b341561008857600080fd5b610090
            //                             610113565b6040518082815260200191505060405180910390f35b6000809054906101000a900473ffffffffffffffff
            //                             ffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffff
            //                             ffffffffffffffffff1614151561010157600080fd5b81600181905550806002819055505050565b6000600254600154
            //                             019050905600a165627a7a72305820b12ed026398b4a1bfb98e3b878bd9a65d065b5a9d76466f8e9b08d3205909ad80029")
             _byteCode = element.GetElementProperty("BYTE_CODE"); 
            
            // Smart contract ABI ([{""constant"":false,""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""name"":""saveConsumptions"",""outputs"":[],
            //                      ""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":false,""inputs"":[],
            //                      ""name"":""getConsumptions"",""outputs"":[{""name"":""sumConsumption"",""type"":""int256""}],""payable"":false,
            //                      ""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},
            //                      {""name"":""washingMachineConsumption"",""type"":""int256""}],""payable"":false,""stateMutability"":""nonpayable"",
            //                      ""type"":""constructor""}])
            _abi = element.GetElementProperty("ABI");
            
            // Name of smart contract function to be called ("saveConsumptions")
            _functionName = element.GetElementProperty("FUNCTION_NAME");

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
            var web3 = new Web3.Web3(_providerUrl);
            
            web3.TransactionManager.DefaultGas =  BigInteger.Parse(_defaultGas);
            web3.TransactionManager.DefaultGasPrice = Nethereum.Signer.Transaction.DEFAULT_GAS_PRICE;
            
            var unlockAccountResult = await web3.Personal.UnlockAccount
                                                         .SendRequestAsync(_senderAddress, _password, _unlockAccountDuration);
            
            var transactionHash = await web3.Eth.GetContract(_abi, _contractAddress)
                                                .GetFunction(_functionName)
                                                .SendTransactionAsync(_senderAddress, values);
            
            // Developed on private Geth network where we were only miners.... 
            var receipt = await MineAndGetReceiptAsync(web3, transactionHash);

            return transactionHash;
        }
        #endregion

        #region MineAndGetReceiptAsync
        async Task<TransactionReceipt> MineAndGetReceiptAsync(Web3.Web3 web3, string transactionHash)
        {
            var web3Geth = new Web3Geth(web3.Client);

            var miningResult = await web3Geth.Miner.Start.SendRequestAsync(6);
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            miningResult = await web3Geth.Miner.Stop.SendRequestAsync();
            return receipt;
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
                    foreach (string args in Regex.Split(element.GetElementProperty("CONTRACT_PARAMS"), "#100#"))
                        sFunctions += ZenCsScriptCore.GetFunction("return " + elements + ";");

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