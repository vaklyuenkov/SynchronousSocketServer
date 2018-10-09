# SynchronousSocketServer
Simple Synchronous Socket Server help to go through dirs and watch size of files.

If you will get message `ConfigurationManager can't exist in such context` you should add a Reference to the `System.Configuration assembly` for the project, for me it was solved by adding in `server.csproj`:
```
  <ItemGroup>
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="4.5.0"/>
  </ItemGroup>
```
Next steps to upgrade:

1. Make server asynchronous
2. 
