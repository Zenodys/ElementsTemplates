using Neo.JsonRpc.Client;
using Neo.RPC.DTOs;
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
using System.Collections.Generic;
using System.Text;

namespace ZenNeoSmFunction
{
    /**
    *
    * Zenodys Visual element that calls NEO smart contract function.
    * For more information about Zenodys elements for managed engine implementation refer to /DotNet/Action/ZenAction.cs
    * Element code is for demonstrating purposes and will be upgraded for production use.
    * Integration between Zenodys platform and NEO was done via great Neo RPC library (https://seattle.github.com/CityOfZion/Neo-RPC-SharpClient)
    */
    public class ZenNeoSmFunction : IZenAction, IZenNodeInit
    {
        #region Fields
        string _rpcClientUri;
        string _scriptHash;
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
            // Rpc client url ("http://localhost:30333")
            _rpcClientUri = element.GetElementProperty("RPC_CLIENT_URI");

            // Smart contract script hash. ("0x9a7eab74e3578976a0c62f7e5387022a99994e96")
            _scriptHash = element.GetElementProperty("SCRIPT_HASH");

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
            // Get current element results that are then passed as arguments to neo smart contract function
            object[] args = GetSmartContractArgs();

            List<InvokeParameter> neoParameters = new List<InvokeParameter>();
            for (int i = 0; i < args.Length; i++)
            {
                InvokeParameter neoParameter = new InvokeParameter();
                //TO DO : read type from property
                //http://docs.neo.org/en-us/sc/tutorial/Parameter.html
                neoParameter.Type = "02";
                neoParameter.Value = args[i].ToString();
                neoParameters.Add(neoParameter);
            }

            var client = new RpcClient(new Uri(_rpcClientUri));
            
            var result = await new RPC.NeoApiService(client).Contracts.
                                    InvokeContract.SendRequestAsync(_scriptHash, neoParameters);
            
            Hashtable htResult = new Hashtable();
            htResult.Add("TYPE", result.Stack[0].Type);
            htResult.Add("VALUE", result.Stack[0].Value);
            element.LastResultBoxed = htResult;
            element.IsConditionMet = true;
        }
        #endregion
        #endregion
        #endregion

        #region Private functions
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
                               Path.Combine("tmp", "NeoSmartContractFunction", element.ID + ".zen"), null, element.GetElementProperty("PRINT_CODE") == "1");
                }
            }
        }
        #endregion
        #endregion
    }
}