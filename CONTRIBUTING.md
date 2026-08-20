# Development conventions

## Project structure

- `Models` contains application data and state objects.
- `Services` contains storage, integration, and application-level operations.
- `Views` contains screens and dialogs together with their XAML code-behind files.
- `Controls` contains reusable WPF controls.
- `Themes` contains theme resources and theme selection types.
- `Styles` contains shared control styles.
- `Assets` contains application resources such as icons.
- `Properties` contains assembly metadata and generated resources/settings.

Keep `App.xaml`, `MainWindow.xaml`, and their code-behind files at the project root. Place new files in the narrowest matching directory and keep each XAML file beside its code-behind file.

## Formatting

The repository `.editorconfig` defines the baseline rules. Use four spaces in C#, two spaces in XML and XAML, UTF-8, CRLF line endings, and a final newline. CSharpier defines the canonical C# and XML/XAML layout. Do not align code manually with extra spaces.

Restore the pinned formatter once after cloning:

```powershell
dotnet tool restore
```

Before committing, format the project and verify the result:

```powershell
dotnet csharpier format .
dotnet csharpier check .
```

Generated `*.Designer.cs` files are not maintained by hand.

## Naming and comments

Follow standard .NET naming: PascalCase for types and public members, camelCase for parameters and local variables, and `_camelCase` for private instance fields.

Comments should explain intent, constraints, or non-obvious decisions. Do not use decorative separator banners or narrate what the next statement already says. Write comments as short sentences, starting with a capital letter and ending with punctuation. Use XML documentation only for public APIs whose contract is not clear from their signature.

