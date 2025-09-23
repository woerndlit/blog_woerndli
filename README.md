# blog_woerndli
![CI](https://github.com/woerndlit/blog_woerndli/workflows/CI/badge.svg)

## Per-post output layout

Each blog post is now generated into its own folder with an `index.html` file. For
example, a post with the slug `my-post` will be written to:

```
output/posts/my-post/index.html
```

How the slug is determined:
- If a `Slug` front-matter field is present in the post, that value is used.
- Otherwise the source file name (without extension) is used as the slug.

Example front-matter (YAML) to set a custom slug:

```yaml
Title: My Post
Date: 2025-01-01
Slug: custom-slug
---
```

If you prefer a different slug behavior (for example using `Title` as a fallback),
edit `Program.cs` where the slug logic is implemented.
