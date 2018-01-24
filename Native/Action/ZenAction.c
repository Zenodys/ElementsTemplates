/**
*                           ZENODYS ACTION ELEMENT
*
* Element is plugin, loaded and executed at runtime by orchestration engine (Zenodys Computing engine).
* Zenodys Visual Development tool is IDE that runs in browser where all application logic is created by simply drag'n'drop and
* connecting elements into workflows.
* Zenodys Visual Development Tool supports features like visual workflow debugging, transferring project from browser to Zenodys Computing Engine...
*
* Each element is constructed from two parts:
*     1) UI (HTML that runs in IDE) where user can set properties and connect element into workflows. Template for creating UI part will be described
*        in another tutorial
*     2) Element implementation that is executed in orchestration engine process. Template for element is described here 
*   
* There are two basic element types in Zenodys environment:
*     1) Action types - elements that execute some actions (stopping workflow for specified amount of time,
*        write or query database, turn zwave devices on or off...)
*     2) Eventable types - elements that stop the worflow until some event happen (button is pressed,
*        IR signal is received, data arrive on mqtt subscription...)
*
* Computing Engine has two implementations: 
*     1) .NET Framework  that supports elements written in .NET Framework and it's Mono compatible.
*     2) unmanaged C with unbeatable performance and portability in mind. It supports elements written in C/C++. 
*        Unmanaged engine also supports .Net Core Framework elements, but performance and portability is decreased when those are used in project.
*
* This is template of action type element for unmanaged Computing Engine implementation that can be used when creating new elements.
*
* It's a sleep element, that stops the worflow loop for given amount of time.
*
* See also:
*   -  template for unmanaged eventable element  /Native/Eventable/ZenEvent.c
*   -  template for managed action element  /DotNet/Action/ZenAction.cs
*   -  template for managed eventable element  /DotNet/Eventable/ZenEvent.cs
*
*/

#include "ZenSleep.h"
#include "ZenCommon.h"
#include <stdlib.h>
#include <stdio.h>

#if !defined(_WIN32) && (defined(__unix__) || defined(__unix) || (defined(__APPLE__) && defined(__MACH__)))
#include <unistd.h>
#endif

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
* It's useful for some cases that must be done on main thread (initializing RPI digital IO's; executing python global process instance...)
*
* @param    node  : contains all necessary information about current element (property values...)
*
* @return	int
*/
EXTERN_DLL_EXPORT int onNodePreInit(Node* node)
{
	return 0;
}

/**
* Executed on first element run.
* Execution is not thread safe and it must be handled by programmer if needed.
* For example, there can be five visual loops that are executing simultaneously, and each of them contains element that
* read or write same file.
* Orchestration engine provides global flags that can be locked to ensure thread safety.
*
* @param    node : contains all necessary information about current element (property values, last executed time, last error....)
*
* @return	int
*/
EXTERN_DLL_EXPORT int onNodeInit(Node* node)
{
	return 0;
}

/** 
* Executed every time when loop passes element.
*
* @param    node : contains all necessary information about current element (property values, last executed time, last error....)
*
* @return int
*/
EXTERN_DLL_EXPORT int executeAction(Node *node)
{
	int i = atoi(common_get_node_arg(node, "SLEEP_TIME"));
#if !defined(_WIN32) && (defined(__unix__) || defined(__unix) || (defined(__APPLE__) && defined(__MACH__)))
	usleep(i * 1000);
#else
	Sleep(i);
#endif

	// Element has outputs, 1 (green connection) and 0 (red connection), which can be connected to other elements inputs.
    // Zenodys Computing Engine proceeds on green connection if IsConditionMet property is 1 and on red connection otherwise.
    // Some elements have just true connections. If you don't set IsConditionMet property, workflow will hang because initial value is 0.
	node->isConditionMet = 1;
	return 0;

	/**
	 ***************
	// Elements can store results of different types that are visible to all other elements inside project
	// lastResult field is declared as pointer to pointer to void  (void** lastResult)
	
	// Storing int value:
	// node->lastResult = malloc(1 * sizeof(int*));
	// node->lastResult[0] = malloc(sizeof(int));
	// *((int*)node->lastResult[0]) = atoi(common_get_node_arg(node, "INITIAL_VALUE"));
	// node->lastResultType = RESULT_TYPE_INT;

	// Getting int value:
	// int currentCounterValue = *(int*)node->lastResult[0]

	***************

	// Access to all elements inside project is provided with  COMMON_NODE_LIST alias
	//#define COMMON_NODE_LIST  GetNodeList()
    */
}