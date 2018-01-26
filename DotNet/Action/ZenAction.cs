/**
*                           ZENODYS ACTION ELEMENT
*
* Zenodys is Industry 4.0 platform that contains two parts:
*     1) Workflow Builder for creating projects by simply drag'n'drop and connecting 
*     visual elements into workflows. It is IDE  that runs in browser 
*     2) Computing Engine, orchestration tool that runs on edge and executes visual scripts 
*     created in Workflow Builder
*
* Workflow Builder features:
*     1) Visual step by step remote workflow debugging and values inspection
*     2) Remote project deployment to Zenodys Computing Engine
*     3) Multiuser project development
*     4) And many more
*
* Element is plugin, loaded and executed at runtime by orchestration engine (Zenodys Computing engine).
*
* Each element is constructed from two parts:
*     1) UI (HTML that runs in IDE) where user can set properties and connect element into workflows.
*     2) Element implementation that is executed in orchestration engine process.
*
* This document is code template for creating element implementations
*   
* There are two basic element types in Zenodys environment:
*     1) Action types - elements that execute some actions (stopping workflow for specified amount of time,
*        write or query database, turn zwave devices on or off...)
*     2) Eventable types - elements that stop the worflow until some event happen (button is pressed,
*        IR signal is received, data arrive on mqtt subscription...)
*
* Computing Engine has two implementations: 
*     1) .NET Framework  that supports elements written in .NET Framework and it's Mono compatible.
*     2) Unmanaged C with unbeatable performance and portability in mind. It supports elements written in C/C++. 
*        Unmanaged engine also supports .Net Core Framework elements, but performance and portability is 
*        decreased when those are used in project.
*
* This is template for creating action element types that are running on managed Computing Engine implementation.
*
* It demonstrates sleep element that stops the worflow loop for given amount of time.
*
* See also:
*   -  Template for managed eventable element  /DotNet/Eventable/ZenEvent.cs
*   -  Template for unmanaged eventable element  /Native/Eventable/ZenEvent.c
*   -  Template for unmanaged action element  /Native/Action/ZenAction.c
*
*/

using CommonInterfaces;
using System;
using System.Collections;
using System.Threading;

namespace ZenAction
{
    /**
    * This is action element type so IZenAction interface must be implemented.
    * For eventable elements IZenEvent is required instead of IZenAction.
    * IZenNodePreInit is not required and here just for illustration purposes
    */
    public class ZenAtion : IZenAction, IZenNodePreInit, IZenNodeInit
    {
        #region Fields
        int _sleepTime = 0;
        #endregion

        #region IZenPreInit implementations
        /**
        * First in series of element callbacks. 
        * Executed when orchestration engine is finished with elements loading.
        
        * It's called for each element sequentially and it's thread safe. It's useful for cases that must be done 
        * on main thread (initializing RPI digital IO's, executing python global process instance...)
        *
        * @param    element  : contains all necessary information about current element (property values, 
        *                      last executed time, last error....)
        *
        * @param    elements : all elements. Useful for getting results, errors, states... from other elements 
        *                      or executing them dynamically.
        *
        * @return	void
        */
        #region OnNodePreInit
        public void OnNodePreInit(IPlugin element, Hashtable elements)
        {}
        #endregion
        #endregion

        #region IZenNodeInit implementations
        #region OnNodeInit
        /**
        * Second in series of element callbacks. 
        * Executed on first element run.
        *
        * Execution is not thread safe and it must be handled by programmer if needed.
        *
        * For example, there can be five visual loops that are executing simultaneously, and each of 
        * them contains element that read or write same file.
        *
        * Orchestration engine provides global flags that can be locked to ensure thread safety.
        *
        * @param    elements : all elements. Useful for getting results, errors, states... from other elements 
        *                      or executing them dynamically.
        *
        * @param    element : contains all necessary information about current element (property values, 
        *                     last executed time, last error....)
        *
        * @return	void
        */
        public void OnNodeInit(Hashtable elements, IPlugin element)
        {
             // SLEEP_TIME is property defined in UI part of element.
            _sleepTime = Convert.ToInt32(element.GetElementProperty("SLEEP_TIME"));
        }
        #endregion
        #endregion

        #region IZenAction Implementations
        #region Properties
        #region ID
        /** 
        * Element id that user defines in Workflow Builder
        */
        public string ID { get; set; }
        #endregion

        #region ParentBoard
        /**
        * Zenodys Computing Engine instance that provides to each element:
        *   - events (pre-reboot event...) 
        *   - infos (elements directory....)
        *   - actions (debug prints in Workflow Builder debug console...)
        */
        public IGadgeteerBoard ParentBoard { get; set; }
        #endregion
        #endregion

        #region Functions
        #region ExecuteAction
        /** 
        * Last in series of element callbacks, repeatable executed when element connected in loop.
        * Here goes main element logic.
        *
        * @param    elements: all elements. Useful for getting results, errors, states... from other elements
        *                     or executing them dynamically.
        *
        * @param    element : contains all necessary information about current element (property values,
        *                     last executed time, last error....)
        *
        * @param    iAmStartedYou : contains id of the element, that started current one.
        *                           This can be elemented that precede current element,or any other element,
        *                           not directly connected to current one, but is dynamically execute it.
        */
        public void ExecuteAction(Hashtable elements, IPlugin element, IPlugin iAmStartedYou)
        {
            // Stop the loop for given amount of time.
            Thread.Sleep(_sleepTime);

            // Element has outputs, true (green connection) and false (red connection), which can be
            // connected to other elements inputs.
            
            // Zenodys Computing Engine proceeds on green connection if IsConditionMet property is true 
            // and on red connection otherwise.
            
            // Some elements have just true connections. If you don't set IsConditionMet property,
            // workflow will hang because initial value is false.
            element.IsConditionMet = true;

            // Elements can hold primitive or complex results that are visible to all other elements inside project:
            //      element.LastResultBoxed = 12;
            //      element.LastResultBoxed = "THIS IS RESULT";
            //      element.LastResultBoxed = myDataTable;
        }
        #endregion
        #endregion
        #endregion
    }
}
