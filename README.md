# SLang Compiler for .NET Core platform

SLang Compiler for .NET Core platform compiles SLang JSON IR (produced by core SLang compiler module) into portable .NET IL (byte code).

Compiler itself is also a .NET Core application. Use appropriate `dotnet` CLI commands to build, deploy and run. Read more:

 1. https://github.com/dotnet/cli/tree/master/Documentation
 2. https://docs.microsoft.com/en-us/dotnet/core/deploying/
 3. https://natemcmaster.com/blog/2017/12/21/netcore-primitives/

# Usage

SLang Compiler for .NET Core takes SLang JSON IR file as an input and produces corresponding `*.dll` assembly as an output. Both input and outputs may be given as command line arguments. Otherwise, standard input stream is used as an input and the name `out.dll` is used for output into current directory.

For example:

`$ SLang.NET < my_unit.json > my_unit.dll`

`$ SLang.NET my_unit.json -o my_unit.dll`

## Warning

SLang Compiler does not generate `*.runtimeconfig.json` and `*.deps.json` which are required to run unit on .NET Core. Read [this article](https://natemcmaster.com/blog/2017/12/21/netcore-primitives/) on how to make them yourself.