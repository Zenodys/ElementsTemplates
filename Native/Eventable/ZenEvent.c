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
*        Unmanaged engine also supports .Net Core Framework elements, but performance and portability is
*        decreased when those are used in project.
*
* This is template for creating eventable element types that are running on unmanaged Computing Engine implementation.
*
* See also:
*   -  Template for unmanaged action element  /Native/Action/ZenAction.c
*   -  Template for managed action element  /DotNet/Action/ZenAction.cs
*   -  Template for managed eventable element  /DotNet/Eventable/ZenEvent.cs
*
*/

# include <string.h>
# include <stdlib.h>
# include <stdio.h>
# include "ZenCommon.h"

Node* _nodeIds[100];
int _nodeListCnt = 0;
struct eventContextParamsStruct event_context;

/**
* Executed when shared object is loaded into memory and before element is created.
* It's thread safe.
*
* @param    params : implementation parameters 
*
* @return	int
*/
EXTERN_DLL_EXPORT int onImplementationInit(char *params)
{
    return 0;
}

/**
* Executed when orchestration engine is finished with elements loading.
* It's called for each element sequentially and it's thread safe.
* It's useful for some cases that must be done on main thread (initializing RPI digital IO's;
* executing python global process instance...)
*
* @param    node  : contains all necessary information about current element (property values...)
*
* @return	int
*/
EXTERN_DLL_EXPORT int onNodePreInit(Node* node)
{
    // Add element to collection 
    _nodeIds[_nodeListCnt] = malloc(sizeof(Node));
    _nodeIds[_nodeListCnt] = node;
	
    // Create fake event.
    // First set condition met and result fields
    _nodeIds[_nodeListCnt]->isConditionMet = 1;
    _nodeIds[_nodeListCnt]->lastResultType = RESULT_TYPE_INT;

    // Then save element context
    event_context.data = &_nodeListCnt;
    event_context.node = _nodeIds[_nodeListCnt];
		
    // And inform Computing Engine about event.
    // Event is processed immediately if event buffer is empty.
    
    // Otherwise, if event generator frequency is higher then event handling,
    // engine stores event in buffer.
    
    // Event are fetched from buffer on lower generator frequency.
    // If buffer becames full, older events are discarded.
    common_push_event_to_buffer(&event_context);
    
    _nodeListCnt++;
    return 0;
}

/**
* Executed on first element run.
*
* Execution is not thread safe and it must be handled by programmer if needed.

* For example, there can be five visual loops that are executing simultaneously,
* and each of them contains element that read or write same file.
*
* Orchestration engine provides global flags that can be locked to ensure thread safety.
*
* @param    node : contains all necessary information about current element (property values,
*                  last executed time, last error....)
*
* @return	int
*/
EXTERN_DLL_EXPORT int onNodeInit(Node* node)
{
    return 0;
}
