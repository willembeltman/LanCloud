# Repository Guidelines

## Project Structure & Module Organization
LanCloud.sln wires 6 C# projects targeting .NET Framework 4.8. 

`LanCloud.Shared/` is the shared project of LanCloud containing all interfaces and enums. 
This project basically defines the interfacing between all project modules of the `LanCloud` solution.

`LanCloud/` is the startup project with a StatusForm, backed by service and domain layers. 
Multiple LanCloud instances are running on multiple machines (aka nodes), each exposing storage containers called `Shares` that hold cloud data.
A `Share` has a specific place inside the cloud data replication. 
You can assign a file-part index each share will save, or assign multiple to store the XOR compulation of those file parts of data.
The infrastructure setup for the cloud nodes is saved in the `LanCloud.json` configuration file inside the `bin` folder.

`LanCloud.Servers.Ftp/` exposes a FTP server implementation to access the cloud files for the user, so users can upload and download files.
The FTP server uses the service interface implementation supplied by the `LanCloud` project starting with IFtp.

`LanCloud.Servers.Rpc/` exposes a RPC server implementation for communication between LanCloud nodes.
The RPC calls are now used to request share information for each node. 
This part is still under construction, for now I've hardcoded everything to just use the local shares only (I think).

`LanCloud.Servers.VirtualDrive/` new virtual drive implementation using Dokan to allow the user to access the cloud files from windows explorer.
I haven't succesfully tested it due to missing Dokan2.dll files, which I now located and are copyed in to the bin directory.

`LanCloud.Log/` centralises logging abstractions

`bin/` and `obj/`; keep configuration assets such as `bin/Debug/LanCloud.json` versioned and editable.

## Build, Test, and Development Commands
Run from the repository root:
```bash
dotnet restore LanCloud.sln
dotnet build LanCloud.sln
dotnet run --project LanCloud/LanCloud.csproj
msbuild LanCloud.sln /p:Configuration=Release
```
Use `dotnet run` while iterating on the UI; launch from Visual Studio if you need the designer. 
Regenerate NuGet packages with `nuget restore` only if `dotnet restore` fails (packages.config projects are supported).

## Coding Style & Naming Conventions
Use four-space indentation and brace-on-new-line layout as in `LanCloud/Program.cs`. 
Keep namespaces aligned with folder paths, prefer `PascalCase` for types, `camelCase` for locals, and prefix interfaces with `I`.
Group `using` statements alphabetically, and prefer dependency injection through constructor parameters over statics. 
Run `dotnet format` or Visual Studio "Format Document" before committing, and retain existing logging idioms through `LogService`.

## Testing Guidelines
No automated tests ship yet; favour xUnit or MSTest when adding the first suite. 
Place new test projects under `tests/` or alongside the code (`LanCloud.Tests/`) and add them to the solution.
Name fixtures after the class under test and methods in `MethodName_Should_DoExpectedThing` form. 
Once tests exist, wire them into CI and run `dotnet test` before pushing.

## Commit & Pull Request Guidelines
History favours short, imperative messages ("Readme", "Update LICENSE");
continue with present-tense summaries under 72 characters, e.g., `Storage: Fix parity slice sync`.
For pull requests, include a concise description, linked issues, manual/FTP test evidence, and screenshots for UI tweaks (`StatusForm`).
Request review when builds succeed and note any configuration steps.

## Configuration & Environment
Local runs expect application settings in `LanCloud/App.config`.
Update sample entries when protocols change, and never commit machine-specific secrets.
Ensure all developers target .NET Framework 4.8 and keep FTP ports configurable through the config service.

## Todo for now:
	1.	Complete virtual drive support through Dokan. 
		I've now tidied up the program and added a Shared project for easy interfacing, but it's not working.
		The problem is: The drive show's up, but disappears a few seconds later. I don't get any feedback in debug.
		Please complete the virtual drive integration first.
	2.	Complete support for appending files. 
		I don't know how much this will be used, but I understand this will need to work.
		Please complete support for appending files, try to create a new seperate flow like the Reader and the Writer.

## Please skip/withhold from:
	1.	Do not complete the inter-node synchronisation systems and usage of remote shares.
	2.	Any user authentication stuff, implement it so it always works, but I can update it later.