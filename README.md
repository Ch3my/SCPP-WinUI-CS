# SCPP-WinUI-CS

To build your C# WinUI Visual Studio project for Linux using .NET 6, you can follow these steps:

Install the .NET 6 SDK on your Linux machine. You can download the appropriate package for your Linux distribution from the .NET download page: https://dotnet.microsoft.com/download/dotnet/6.0

Open a terminal and navigate to the directory where your WinUI project is located.

Run the dotnet build command to build your project. For example, if your project file is named MyProject.csproj, you can run the following command:

Copy code
dotnet build MyProject.csproj -r linux-x64

This command will build your project and produce a binary that is compatible with Linux x64 architecture. You can replace linux-x64 with other target runtimes if you need to build for a different architecture.

Once the build process completes successfully, you can find the binary in the bin directory of your project. You can then copy this binary to your Linux machine and run it using the dotnet MyProject.dll command.
Keep in mind that while .NET 6 is a cross-platform framework, your WinUI application may still have Windows-specific dependencies or features that may not work on Linux. You may need to make some modifications to your code or UI to ensure that it runs smoothly on Linux.

Install mono

sudo apt install mono