# Rage Quality Assurance Tool #

RQAT is a prototypical tool that allows RCSAA component developers to self assess their code for common issues.

Its purpose is to perform various automated checks on RCSAA components (components using the RAGE Client Side Asset Architecture). 

RQAT is a technology preview of an extensible  plugin based tool able to perform multi-level checks.

Results will be presented in an Excel Speadsheet format.

**RQAT can start analysis from several starting points:** 

- [x] A Git repository URL.
- [x] A local Git repository 
- [x] A C# Solution file (*.sln)
- [x] A C# Project file (*.csproj)
- [x] An Assembly (*.dll)

**Depending on the starting point, RQAT will be able to perform various checks:**

- [x] Solution and project structure and layout.
- [x] Detect common mistakes reducing reuse.
- [x] Check for correctly linked sources between projects.
- [x] References to non-portable assemblies.
- [x] Calculate metrics.
- [x] Detect interfaces usage.
- [x] Detect public API.
- [x] Checks if a nuGet package can be created including metadata, assemblies for all supported frameworks, sources and support materials.
- [x] Detect and execute available unit tests.
- [x] Detect mismatches between present sources, solutions and reporsitories.

