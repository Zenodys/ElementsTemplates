/**
*                           ZENODYS EVENTABLE ELEMENT
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
*        Unmanaged engine also supports .Net Core Framework elements, but performance and portability is decreased 
*        when those are used in project.
*
* This is template for creating eventable element types that are running on managed Computing Engine implementation
*
* It demonstrates responding to events from another process.
* 
* See also:
*   -  Template for managed action element  /DotNet/Action/ZenAction.cs
*   -  Template for unmanaged eventable element  /Native/Eventable/ZenEvent.c
*   -  Template for unmanaged action element  /Native/Action/ZenAction.c
*
*/

using CommonInterfaces;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ZenCommonNetFramework;

namespace ZenEvent
{
    /**
    * This is eventable element type so IZenEvent interface must be implemented.
    * For action elements IZenAction is required instead of IZenEvent.
    * IZenNodeInit is not required and here just for illustration purposes
    */
    public class ZenEvent : IZenEvent, IZenNodePreInit, IZenNodeInit
    {
        #region Fields
        #region _element
        IPlugin _element;
        #endregion
        #endregion

        #region IZenPreInit implementations
        /**
        * First in series of element callbacks. 
        * Executed when orchestration engine is finished with elements loading.
        *
        * It's called for each element sequentially and it's thread safe.
        *
        * It's useful for cases that must be done on main thread (initializing RPI digital IO's, 
        * executing python global process instance...)
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
        {
            _element = element;
            // Execute process and redirect standard output so we can handle events from it's output
            var startInfo = new ProcessStartInfo(Directory.GetParent(ParentBoard.TemplateRootDirectory).Parent.FullName + "/someProcess.exe");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            ZenProcessCore.StartProcess(startInfo,ParentBoard);
            ZenProcessCore.OnProcessOutputDataReceivedEvent += ZenProcessCore_OnProcessOutputDataReceivedEvent;
        }
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
        * For example, there can be five visual loops that are executing simultaneously,
        * and each of them contains element that read or write same file.
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
        {}
        #endregion
        #endregion

        #region Event Handlers
        #region ZenProcessCore_OnProcessOutputDataReceivedEvent
        /**
        * Handle output from process. Note that more "logical" elements can share same implementation.
        *
        * For example, elements with id's "ButtonOn" and "ButtonOff" that are defined in Zenodys Workflow Builder,
        * share same implementation - ZenButton.
        *
        * This event is fired for all elements that share same implementation.
        * Here we inform Zenodys Computing Engine that event occured. 
        *
        * If we pass element Id in ModuleEventData instance, then Computing Engine automatically filters elements 
        * and CheckInterruptCondition callback is called only for element with matching Id.
        *
        * Otherwise CheckInterruptCondition is called for all elements that share same implementation and 
        * decision if loop proceeds from current element is made there.
        */
        void ZenProcessCore_OnProcessOutputDataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
            if (ModuleEvent != null)
                ModuleEvent(this, new ModuleEventData(_element.ID, null, e.Data));
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
        /**
        * Callback that is called from Computing Engine.
        * If element Id is specified in ModuleEventData instance then only element with matching id is called.
        *
        * @param    eventData : information passed from original event handler (third parameter
        *                       of ModuleEventData instance)
        *
        * @param    element : contains all necessary information about current element (property values,
        *                     last executed time, last error....)
        *
        * @param    elements : all elements. Useful for getting results, errors, states... from other elements or 
        *                      executing them dynamically.
        *
        * @return   bool    if true then computing Engine resumes loop to element that proceeds current,
        *                   otherwise loop is not resumed
        */
        public bool CheckInterruptCondition(ModuleEventData eventData, IPlugin element, Hashtable elements)
        {
            element.LastResultBoxed = eventData.Tag;
            element.IsConditionMet = true;
            return true;
        }
        #endregion
        #endregion
        #endregion
    }
}