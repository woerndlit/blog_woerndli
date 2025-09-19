Local changelog (workspace-local, created because git not available in terminal)

Changes performed locally:

- Updated `blog_woerndli.csproj`:
  - `Statiq.Web` -> `1.0.0-beta.60`
  - `TargetFramework` -> `net8.0`
- Updated `theme/settings.yml` minimum Statiq Web version -> `1.0.0-beta.60`
- Replaced obsolete `Statiq.Html.HtmlKeys.Excerpt` with `Statiq.Common.Keys.Excerpt` in `theme/input/_post.cshtml`
- Added temporary console logging around bootstrapper in `Program.cs` for debugging (you may remove it later)
- Ran full site generation and started a local static server serving `output/` on port 5080 (using `dotnet-serve` global tool)

This is a local commit alternative; please run `git add`/`git commit` manually if you want a VCS commit.
