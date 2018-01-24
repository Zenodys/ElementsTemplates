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

namespace ZenSmFunction
{
    /**
    *
    * Zenodys Visual element that calls smart contract function.
    * For more information about Zenodys elements for managed engine implementation refer to /DotNet/Action/ZenAction.cs
    * Element code is for demonstrating purposes and will be upgraded for production use.
    * Integration between Zenodys platform and Ethereum was done via great Nethereum library (https://github.com/Nethereum/Nethereum)
    */
    public class ZenSmFunction : IZenAction, IZenNodeInit
    {
        #region Fields
        string _senderAddress;
        string _password;
        string _abi;
        string _contractAddress;
        string _functionName;
        string _functionResultType;
        string _providerUrl;
        int _unlockAccountDuration;
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

            // Sender address. ("0x9812db3e6c072a9731267485bd1ce075ae11e6a8")
            _senderAddress = element.GetElementProperty("SENDER_ADDRESS");
            
            // Sender password. ("password")
            _password = element.GetElementProperty("SENDER_PASSWORD");

            // How much time will account be unlocked in seconds. (120)
            _unlockAccountDuration = Convert.ToInt32(element.GetElementProperty("UNLOCK_DURATION"));
            
            // Smart contract address ("0x97a93e68fa58513facb2b702a97597cab97afd6f") 
            _contractAddress = element.GetElementProperty("SMART_CONTRACT_ADDRESS");
            
            // Smart contract ABI ("[{""constant"":false,""inputs"":[{""name"":""tvConsumption"",""type"":""int256""},{""name"":""washingMachineConsumption"",""type"":""int256""}],""name"":""saveConsumptions"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getConsumptions"",""outputs"":[{""name"":""sumConsumption"",""type"":""int256""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""inputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""}]")
            _abi = element.GetElementProperty("ABI");
            
            // Name of smart contract function to be called ("getConsumptions")
            _functionName = element.GetElementProperty("FUNCTION_NAME");

            // Type that contract function returns ("int")
            _functionResultType= element.GetElementProperty("FUNCTION_RESULT_TYPE");
            
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
            // Get current element results that are then passed as arguments to smart contract function
            object[] args = GetSmartContractArgs();

            // Store smart contract result to the element so that can be used by other visual elements inside project
            switch (_functionResultType)
            {
                case "bool":
                    element.LastResultBoxed = ExecuteContractWrapper<bool>(args);
                    break;

                case "uint":
                    element.LastResultBoxed = ExecuteContractWrapper<uint>(args);
                    break;

                case "int16":
                    element.LastResultBoxed = ExecuteContractWrapper<Int16>(args);
                    break;

                case "float":
                    element.LastResultBoxed = ExecuteContractWrapper<float>(args);
                    break;

                case "double":
                    element.LastResultBoxed = ExecuteContractWrapper<double>(args);
                    break;

                case "byte":
                    element.LastResultBoxed = ExecuteContractWrapper<byte>(args);
                    break;

                case "uint16":
                    element.LastResultBoxed = ExecuteContractWrapper<UInt16>(args);
                    break;

                case "int64":
                    element.LastResultBoxed = ExecuteContractWrapper<Int64>(args);
                    break;

                case "int":
                    element.LastResultBoxed = ExecuteContractWrapper<int>(args);
                    break;

                case "string":
                    element.LastResultBoxed = ExecuteContractWrapper<string>(args);
                    break;
            }

            element.IsConditionMet = true;
        }
        #endregion
        #endregion
        #endregion

        #region Private functions
        #region ExecuteContract
        T ExecuteContractWrapper<T>(params object[] args)
        {
            Task<T> task = Task.Run(async () => await ExecuteContract<T>(args).ConfigureAwait(false));
            task.Wait();
            return task.Result;
        }

        async Task<T> ExecuteContract<T>(params object[] values)
        {
            var web3 = new Web3.Web3(_providerUrl);
            var unlockAccountResult = await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, _unlockAccountDuration);
            return await web3.Eth.GetContract(_abi, _contractAddress).GetFunction(_functionName).CallAsync<T>(values);
        }
        #endregion
       
        #region GetSmartContractArgs
        object[] GetSmartContractArgs()
        {
            object[] args = new object[_scripts.ScriptDoc.DocumentNode.Descendants("code").Count()];
            for (int i = 0; i < _scripts.ScriptDoc.DocumentNode.Descendants("code").Count(); i++)
                args[i] = _scripts.ZenCsScript.RunCustomCode(_scripts.ScriptDoc.DocumentNode.Descendants("code").ElementAt(i).Attributes["id"].Value);
            
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

                    _scripts = ZenCsScriptCore.Initialize(sFunctions, elements, element, Path.Combine("tmp", "SmartContractFunction", element.ID + ".zen"), null, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }
        }
        #endregion
        #endregion
    }
}