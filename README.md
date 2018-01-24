# Code templates for element implementations
Element is plugin, loaded and executed at runtime by orchestration engine (Zenodys Computing engine).
Zenodys Visual Development tool is IDE that runs in browser where all application logic is created by simply drag'n'drop and connecting elements into workflows.
Zenodys Visual Development Tool supports features like visual workflow debugging, transferring project from browser to Zenodys Computing Engine...

Each element is constructed from two parts:
* UI (HTML that runs in IDE) where user can set properties and connect element into workflows.
* Element implementation that is executed in orchestration engine process.

There are two basic element types in Zenodys environment:
* Action types - elements that execute some actions (stopping workflow for specified amount of time, write or query database, turn zwave devices on or off...)
* Eventable types - elements that stop the worflow until some event happen (button is pressed, IR signal is received, data arrive on mqtt subscription...)

Computing Engine has two implementations: 
* .NET Framework  that supports elements written in .NET Framework and it's Mono compatible.
* unmanaged C that supports elements written in C/C++ and .Net Core Framework elements
