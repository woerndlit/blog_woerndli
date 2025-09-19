# Changelog

All notable changes to this project are documented in this file.

## [Unreleased] - 2025-09-19

- Upgrade Statiq.Web to `1.0.0-beta.60` and ensure restore/build success. (commit: `7db480a`)
- Bump project `TargetFramework` to `net8.0` to avoid EOL warnings and use modern SDK. (commit: `7db480a`)
- Bump theme's minimum Statiq Web version in `theme/settings.yml` to `1.0.0-beta.60`. (commit: `7db480a`)
- Replace obsolete Razor usage `Statiq.Html.HtmlKeys.Excerpt` with `Statiq.Common.Keys.Excerpt` in `theme/input/_post.cshtml`. (commit: `7db480a`)
- Run full site generation and validate output (results: 144 files seen, 2 written).
- Start and stop a local static file server (`dotnet-serve`) for the `output/` folder on port `5080`.
- Remove temporary bootstrapper debug logs from `Program.cs`. (commit: `b6bb69e`)

## History

- 2025-07-28: Various backup workflow updates and deletions (older commits)
