# Blazor demo

[Русский](../ru/blazor-demo.md)

The application demonstrates Sitko.Core forms with FluentValidation, navigation,
and file uploads. It uses server interactivity and WebAssembly through `InteractiveAuto`.
Form data exists only on the open page; uploaded files are stored locally.

## Run

You need a .NET SDK compatible with the repository's `global.json`, a browser supporting
WebAssembly, and a trusted local ASP.NET Core development certificate. Docker,
PostgreSQL, and external services are not required.

Run from the repository root:

```bash
dotnet dev-certs https --trust
dotnet run --project apps/Blazor/Sitko.Core.Apps.Blazor --launch-profile https
```

Open [https://localhost:7163](https://localhost:7163). The home page links to the form,
uploads, and the query parameter example. On the first visit, `InteractiveAuto` uses
the server; WebAssembly downloads in the background and is used on subsequent visits.

This application is intended for local use with demonstration data. Do not upload
confidential files: uploaded content is accessible without signing in.

## Form and validation

1. Open `/Form` and submit an empty name: a required-field error should appear.
2. Enter `Анна` as the name and `Пример` as the message, then submit. The application displays the accepted values.
3. Edit the values and reset the form. The fields return to their initial state.

Submission demonstrates the form and validation; form data is not persisted.
The scenario uses `BaseMudForm<TEntity>` and a model from `Sitko.Core.Apps.Blazor.Data`.

## Files

`/Files` provides single and multiple uploads and an image preview. Prepare a small
text file, several files, and a PNG/JPEG image. Each file is limited to 2 MiB;
a multiple selection is limited to five files.

After uploading, open the link and compare the content with the original file.
Check the image preview. Selecting a file larger than 2 MiB should display a size error.

The server uses `FileSystemStorage` and `FileSystemStorageMetadata`. WebAssembly uploads
use the existing `UploadController` through `Storage.Remote`. The demo does not use
`Repository.Remote`.

Files and metadata are stored under
`apps/Blazor/Sitko.Core.Apps.Blazor/wwwroot/static/demo/`.
Stop the application and remove that directory using your file manager to clean up.
No files outside this directory need to be removed.

## Query parameter

Open `/QueryString?test=11111111-1111-1111-1111-111111111111`: the page displays this GUID.
Follow the link to change its value, navigate back, and reload. The displayed value
should match the current URL each time.

## Verify the scenarios

Repeat the form, navigation, and uploads on the first visit in a fresh browser profile,
then reopen the application after WebAssembly has downloaded. Check the browser console and server log for unexpected errors.
Rejecting an oversized file produces a size error message. Both checks matter: a successful build does not verify
the interactive UI or file transfer.

## Source locations

- [Server and local storage](../../apps/Blazor/Sitko.Core.Apps.Blazor/Program.cs).
- [WebAssembly and browser transport](../../apps/Blazor/Sitko.Core.Apps.Blazor.Client/Program.cs).
- [Scenario pages](../../apps/Blazor/Sitko.Core.Apps.Blazor.Client/Pages).
- [Models and validators](../../apps/Blazor/Sitko.Core.Apps.Blazor.Data).
