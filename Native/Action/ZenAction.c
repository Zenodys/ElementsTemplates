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
* Computing Engine has two implementations: 
*     1) .NET Framework  that supports elements written in .NET Framework and it's Mono compatible.
*     2) Unmanaged C with unbeatable performance and portability in mind. It supports elements written in C/C++. 
*        Unmanaged engine also supports .Net Core Framework elements, but performance and portability 
*        is decreased when those are used in project.
*
* This is template of action type element for unmanaged Computing Engine implementation.
*
* It demonstrates sleep element that stops the worflow loop for given amount of time.
*
* See also:
*   -  Template for unmanaged eventable element  /Native/Eventable/ZenEvent.c
*   -  Template for managed action element  /DotNet/Action/ZenAction.cs
*   -  Template for managed eventable element  /DotNet/Eventable/ZenEvent.cs
*
*/

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
* It's useful for some cases that must be done on main thread (initializing RPI digital IO's;
* executing python global process instance...)
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
* For example, there can be five visual loops that are executing simultaneously, and each of them 
* contains element that read or write same file.
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

/** 
* Executed every time when loop passes element.
*
* @param    node : contains all necessary information about current element (property values,
*                  last executed time, last error....)
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

	// Element has outputs, 1 (green connection) and 0 (red connection), which can be connected
	// to other elements inputs.
    
	// Zenodys Computing Engine proceeds on green connection if IsConditionMet property is 1 and 
	// on red connection otherwise.
    
	// Some elements have just true connections. If you don't set IsConditionMet property, workflow
	//  will hang because initial value is 0.
	node->isConditionMet = 1;
	return 0;

	/**
	 ***************
	// Elements can store results of different types that are visible to all other elements 
	// inside project lastResult field is declared as pointer to pointer to void  (void** lastResult)
	
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