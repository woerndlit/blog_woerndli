# Copilot instructions for blog_woerndli

This file contains concise, repository-specific guidance for AI coding agents (Copilot-style) to be productive quickly.

- Big picture: This repo is a Statiq-based static site generator (see `README.md`). Source content lives in `input/` and the generator writes a ready-to-serve static site into `output/`.

- Entry points and pipeline: The generator's logic and Statiq pipeline customizations are in `Program.cs`.
  - `Program.cs` sets per-post output destinations to `output/posts/{slug}/index.html` and overrides Archives pipeline to write `tags/index.html` for the root tags page.
  - Media handling: In DEBUG builds, `Program.cs` copies any `input/posts/<post>/.media` folders into `output/posts/<slug>/.media` after generation so local servers can resolve `.media/...` references.

- Build & run (local): Use the .NET SDK (project targets net8.0). Common commands:
  - dotnet build
  - dotnet run --project .
  - Preview output with a static server: `dotnet tool install --global dotnet-serve` then `dotnet-serve --directory output --port 5080` (or use any static server).

- Conventions and patterns specific to this project:
  - Content: `input/posts/*.md` with YAML front-matter. Post `Slug` front-matter overrides the filename-based slug. See `Program.cs:GetSlug` for exact normalization rules (unicode normalization, non-spacing mark removal, allowed chars `a-z0-9`, spaces -> `-`).
  - Output layout: every post is emitted to `output/posts/<slug>/index.html`. Assets referenced using `.media/` are expected under `output/posts/<slug>/.media/`.
  - Archives/Tags: the Archives pipeline produces both a root tags page and per-tag pages; `Program.cs` forces the root tags archive to `tags/index.html` when `ArchiveKey == "Tags"` and no `GroupKey`.
  - Do not change destination earlier in the pipeline; the code sets destinations in PostProcess modules so earlier pipeline stages won't be overridden.

- Files & folders to inspect for changes:
  - `Program.cs` — pipeline changes, slug/media behavior, and destination logic.
  - `input/posts/` — add or edit posts and `.media` folders here.
  - `theme/` — Razor layout and partials that affect rendering (`theme/input/*.cshtml`).
  - `output/` — generated site; useful for preview and troubleshooting.

- Testing, debugging and quick checks:
  - After edits run `dotnet run` to regenerate `output/` and then preview with `dotnet-serve`.
  - When troubleshooting media not appearing, verify `input/posts/<post>/.media` exists and that `output/posts/<slug>/.media` is created in DEBUG runs.
  - Use the repository's `cache/razorcache.json` and `cache/writecache.json` presence as indicators of prior successful runs; removing them forces full re-generation.

- Integration points / dependencies:
  - NuGet package: `Statiq.Web` (see `blog_woerndli.csproj`). No external web services are required for generation.

- Examples to follow when contributing:
  - To add a new post: create `input/posts/my-post/index.md` (or `my-post.md`) with YAML front-matter. Use `Slug:` only if you need a different URL segment. Put images in `input/posts/my-post/.media/` and reference them as `.media/<image>` in the post markdown.
  - To adjust tag root behavior, modify the Archives pipeline override in `Program.cs` (look for the comment about root tags archive).

If anything in these instructions is unclear or you want more detail (examples of front-matter, sample posts, or CI build steps), say which area to expand and I'll iterate.

## Troubleshooting & expected log noise

- Razor compilation/debug output often contains benign warnings. Common lines you may see when running with DEBUG or detailed logging:
  - CS8019 "Unnecessary using directive" in generated Razor views. These are harmless and come from the Razor-generated code.
  - "Requested Razor view ... does not exist" and view lookup cache misses. Razor tries multiple view locations; missing lookups are normal if a view is resolved from `theme/input` or embedded resources.
  - DataProtection key repository INFO messages (about the user profile path) — normal for ASP.NET DataProtection when running locally.

  These messages are debug/noise and usually not actionable. Look for ERROR or EXCEPTION lines in the logs when debugging a real failure. If you see ReflectionTypeLoadException or a stack trace, capture the full log and search for the first ERROR entry — that's typically the root cause.

## Cold-run checklist (what to do for a true clean build)

1. Remove generated caches and output to force a full regeneration:
   - Delete `output/`, `cache/`, and `temp/`.
2. Rebuild and run:
   - `dotnet build`
   - `dotnet run --project .`
3. Inspect logs (use `--verbosity detailed` or collect `debug_*.log` if you enabled debug logging):
   - Search for ERROR or FATAL entries first.
   - Confirm that `output/posts/<slug>/index.html` files were produced for posts you expect.
   - Verify `.media` assets: in DEBUG builds the generator copies `input/posts/<post>/.media` into `output/posts/<slug>/.media`. If media is missing, check the source `.media` folder and rerun.

## When to open an issue

- If the generator fails with an unhandled exception (ERROR/FATAL in logs) or a ReflectionTypeLoadException that cannot be resolved by restoring NuGet packages, open an issue and include:
  - The full `debug_cold_run.log` (or the terminal output if you didn't enable debug logging).
  - A short description of the steps you ran (clean, build, run) and the exact command used.
  - A list of the `input/posts/...` files you were editing, and whether they include a `.media` folder.

These additions should help AI agents and contributors quickly distinguish expected debug noise from actual failures and confirm the generator's media-copy behavior.