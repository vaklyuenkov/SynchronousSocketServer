# SynchronousSocketServer
Simple Synchronous Socket Server help to go through dirs and watch size of files.

Next steps to upgrade server:

1. Catch more specific exeptions
2. Make server asynchronous
3. Make the links safe (it’s not good to show the address in them)

If you will get message `ConfigurationManager can't exist in such context` you should add a Reference to the `System.Configuration assembly` for the project, for me it was solved by adding in `server.csproj`:
```
  <ItemGroup>
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="4.5.0"/>
  </ItemGroup>
```
