DESCRIPTION:

A simple HTTP server and Issue Tracking webpage written in C#.
Microsoft's DotNet libraries are required for use.





BUILDING:

Run without compiling: from the source code extracted folder "SimpleHttp",

>	dotnet run {port}

*port defaults to 8080.


To compile: from the source code extracted folder "SimpleHttp",

>	dotnet publish -c release

Built files are in the "/bin/release/netcoreapp2.0/publish/" folder.





RUNNING:

From the compiled files folder (seeing "Building" above),

>	dotnet SimpleHttp.dll {port}

*port defaults to 8080.





OPTIONAL:

You can supposedly supply your own html template, though full funtionality is untested.
Since this project is specifically an issue tracker, it will supply the tracking functionality regardless;
  include "<ISSUES_SECTION>" in your HTML code to specify tracker placement.