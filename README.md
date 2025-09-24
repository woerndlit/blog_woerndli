# blog_woerndli
![CI](https://github.com/woerndlit/blog_woerndli/workflows/CI/badge.svg)

## Per-post output layout

Each blog post is now generated into its own folder with an `index.html` file. For
```
```

This repository contains the source and generator for the blog available at
https://www.woernd.li.

The site is produced by a small Statiq-based static site generator. Source
content (posts, images, and metadata) lives in `input/`. The generator reads
that content, applies Razor templates and layouts, and writes the ready-to-serve
site into `output/`.

Main frameworks and tools used
- **Statiq.Web**: pipeline-based static site generator used to process content
	and templates.
- **.NET (6/7/8 compatible)**: runtime for the generator and tooling (project
	targets .NET and is tested with recent SDKs).
- **Razor**: templating engine used by Statiq for layouts and partial views.
- **dotnet-serve**: lightweight static file server used for local previews.
- **YAML + Markdown**: content format and front-matter used for posts under
	`input/posts`.

Quick workflow
- Edit or add content under `input/` and `input/posts`.
- Run the generator (see `Program.cs`) to produce `output/`.
- Preview locally with `dotnet-serve --directory output --port 5080`.

Important files and folders
- `input/`: source content and assets (posts, post `.media` images, site-level
	resources).
- `Program.cs`: entry point and the Statiq pipeline configuration (contains the
	logic that copies referenced post media into per-post `.media` output
	folders).
- `output/`: generated static site — this is what's deployed and served.

Per-post output layout

Each blog post is generated into its own folder with an `index.html`. For
example, a post with the slug `my-post` is written to:

```
output/posts/my-post/index.html
```

Slug behavior
- If a `Slug` front-matter field is present it is used.
- Otherwise the source filename (without extension) is used as the slug.

If you need to change slug determination or media handling, update
`Program.cs` where the relevant Statiq pipeline logic is implemented.
