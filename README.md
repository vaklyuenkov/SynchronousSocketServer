# SynchronousSocketServer
Simple Synchronous Socket Server help to go through dirs and watch sizes of files.

Known problems:
1. If the address in url parameters contains special characters, for instance: "http://localhost:9999/?adress=/home/ww/practice_c#" server don't get correct address (without "#" in this case).

Next steps to upgrade server:
1. Encode address in url parameters (it’s not good to show the address in them)
2. Catch more specific exeptions
  2.1 If we haven't enough permission to start liscening with specific socket.
3. Make server asynchronous

If you will get message `ConfigurationManager can't exist in such context` you should add a Reference to the `System.Configuration assembly` for the project, for me it was solved by adding in `server.csproj`:
```
  <ItemGroup>
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="4.5.0"/>
  </ItemGroup>
```

![](/img/example.PNG)
