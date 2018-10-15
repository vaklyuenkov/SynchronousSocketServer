# SynchronousSocketServer
Simple Synchronous Socket Server help to go through dirs and watch sizes of files.

Next steps to upgrade server:
1. Make server asynchronous (in work)
2. Catch more specific exeptions


If you will get message `ConfigurationManager can't exist in such context` you should add a Reference to the `System.Configuration assembly` for the project, for me it was solved by adding in `server.csproj`:
```
  <ItemGroup>
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="4.5.0"/>
  </ItemGroup>
```

![](/img/example.PNG)
