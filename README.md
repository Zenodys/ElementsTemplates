# Code templates for element implementations

This repository contains informations on how to write elements for Zenodys Computing Engine.

Zenodys is Industry 4.0 platform that contains two parts:
* Workflow Builder for creating projects by simply drag'n'drop and connecting visual elements into workflows. It is IDE  that runs in browser 
* Computing Engine, orchestration tool that runs on edge and executes visual scripts created in Workflow Builder

Workflow Builder features:
* Visual step by step remote workflow debugging and values inspection
* Remote project deployment to Zenodys Computing Engine
* Multiuser project development
* And many more

Element is plugin, loaded and executed at runtime by Zenodys Computing Engine.

Each element is constructed from two parts:
* UI (HTML that runs in IDE) where user can set properties and connect element into workflows.
* Element implementation that is executed in orchestration engine process.

There are two basic element types in Zenodys environment:
* Action types - elements that execute some actions (stopping workflow for specified amount of time, write or query database, turn zwave devices on or off...)
* Eventable types - elements that stop the worflow until some event happen (button is pressed, IR signal is received, data arrive on mqtt subscription...)

Computing Engine has two implementations: 
* .NET Framework  that supports elements written in .NET Framework and it's Mono compatible.
* Unmanaged C that supports elements written in C/C++ and .Net Core Framework elements