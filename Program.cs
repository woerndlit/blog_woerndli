using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using Statiq.App;
using Statiq.Web;
using Statiq.Common;
using Statiq.Core;
// using Statiq.Core.Modules; // not available in this Statiq build

namespace MySite
{
  public class Program
  {
    private static readonly Regex MediaRegex = new(@"\.media/(?<path>[A-Za-z0-9_\-./]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<int> Main(string[] args)
    {
      var bootstrapper = Bootstrapper
        .Factory
        .CreateWeb(args);

      // Configure the web pipelines to output each post into its own folder
      // with an index.html (e.g. /posts/my-post/index.html).
      // We add a module to the Posts pipeline that sets the destination
      // based on the document's source file name (or slug/title if provided).
      bootstrapper.ConfigureEngine(engine =>
      {
        // Modify the Content pipeline so that post documents are written to their own folder
        if (engine.Pipelines.TryGetValue("Content", out var contentPipeline))
        {
          // Set the final destination in PostProcess so it isn't overridden earlier.
          contentPipeline.PostProcessModules.Insert(0, new SetDestination(Config.FromDocument(doc =>
          {
            // Only change destination for documents marked as posts
            if (!doc.GetBool("IsPost"))
            {
              return doc.Destination;
            }

            // NOTE: Archives pipeline destination override moved below (outside this lambda)

            // Derive a slug: OPTION 1 -> prefer "Slug" metadata, otherwise use the source filename
            string source = doc.Source.ToString() ?? string.Empty;
            string slug = doc.GetString("Slug");
            if (string.IsNullOrWhiteSpace(slug))
            {
              slug = Path.GetFileNameWithoutExtension(source);
            }

            if (string.IsNullOrWhiteSpace(slug)) slug = "post";

            return new NormalizedPath($"posts/{GetSlug(doc)}/index.html");
          })));

          // Add a module to copy referenced .media files into each post's output folder
          // The module scans the document content for ".media/..." references and copies
          // those files from the input posts .media folder into the post's output .media folder.
          // It returns the original document to avoid replacing its content.
          // Add this to the end of PostProcessModules so it runs after destinations are set
          // and content has been rendered. Append rather than insert at a fixed index.
          contentPipeline.PostProcessModules.Add(new ExecuteConfig(Config.FromDocument((doc, ctx) =>
          {
            try
            {
              if (doc == null || !doc.GetBool("IsPost"))
              {
                return doc;
              }

              // Read the rendered content as text when available
              string content = null;
              if (doc.ContentProvider?.GetStream() != null)
              {
                using (var sr = new StreamReader(doc.ContentProvider.GetStream(), Encoding.UTF8, true, 4096, true))
                {
                  sr.BaseStream.Seek(0, SeekOrigin.Begin);
                  content = sr.ReadToEnd();
                }
              }

              if (string.IsNullOrEmpty(content))
              {
                // If rendered content isn't available in the document, fall back to the
                // source markdown so we can still discover .media references.
                try
                {
                  string sourcePath = null;
                  if (doc.Source != null)
                  {
                    sourcePath = doc.Source.ToString();
                  }
                  if (!string.IsNullOrWhiteSpace(sourcePath))
                  {
                    if (File.Exists(sourcePath))
                    {
                      content = File.ReadAllText(sourcePath, Encoding.UTF8);
                    }
                    else
                    {
                      // Try relative to repo root
                      var alt = Path.Combine(ctx.FileSystem.RootPath.FullPath, sourcePath.Replace('/', Path.DirectorySeparatorChar));
                      if (File.Exists(alt))
                      {
                        content = File.ReadAllText(alt, Encoding.UTF8);
                      }
                    }
                  }
                }
                catch (Exception ex)
                {
                  ctx.LogWarning(doc, $"Failed reading source file for media scan: {ex.Message}");
                }

                if (string.IsNullOrEmpty(content))
                {
                  return doc;
                }
              }

              // Find .media references anywhere in the content (capture into named group 'path')
              var matches = MediaRegex.Matches(content);
              if (matches.Count == 0)
              {
                return doc;
              }

              // Determine output media folder from the document destination (set by SetDestination)
              // Fallback to the slug-derived output path.
              string destPath = doc.Destination.ToString() ?? string.Empty;
              string slug = GetSlug(doc);
              string outputPostMediaBase;
              if (!string.IsNullOrWhiteSpace(destPath))
              {
                var destDir = destPath.Replace('/', Path.DirectorySeparatorChar);
                destDir = Path.GetDirectoryName(destDir) ?? "posts";
                // Ensure we write into the generator output folder (output/...) not repository root
                outputPostMediaBase = Path.Combine(ctx.FileSystem.RootPath.FullPath, "output", destDir, ".media");
              }
              else
              {
                outputPostMediaBase = Path.Combine(ctx.FileSystem.RootPath.FullPath, "output", "posts", slug, ".media");
              }
              Directory.CreateDirectory(outputPostMediaBase);

              foreach (Match m in matches)
              {
                var rel = m.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar).TrimStart('.', Path.DirectorySeparatorChar);

                // Candidate source locations in order of preference:
                // 1) per-post media: input/posts/{slug}/.media/{rel}
                // 2) global posts media: input/posts/.media/{rel}
                // 3) general input media: input/.media/{rel}
                var candidates = new[]
                {
                  Path.Combine(ctx.FileSystem.RootPath.FullPath, "input", "posts", slug, ".media", rel),
                  Path.Combine(ctx.FileSystem.RootPath.FullPath, "input", "posts", ".media", rel),
                  Path.Combine(ctx.FileSystem.RootPath.FullPath, "input", ".media", rel)
                };

                string found = null;
                foreach (var candidate in candidates)
                {
                  if (File.Exists(candidate))
                  {
                    found = candidate;
                    break;
                  }
                }

                if (found != null)
                {
                  try
                  {
                    var dest = Path.Combine(outputPostMediaBase, Path.GetFileName(found));
                    File.Copy(found, dest, true);
                    ctx.LogInformation(doc, $"Copied media for post '{slug}': {Path.GetFileName(found)}");

                    // Also remove any global copy that the Assets pipeline may have created
                    try
                    {
                      var globalOutputMedia = Path.Combine(ctx.FileSystem.RootPath.FullPath, "output", "posts", ".media", Path.GetFileName(found));
                      if (File.Exists(globalOutputMedia))
                      {
                        File.Delete(globalOutputMedia);
                      }
                    }
                    catch (Exception ex)
                    {
                      ctx.LogWarning(doc, $"Failed removing global media copy for '{found}': {ex.Message}");
                    }
                  }
                  catch (Exception ex)
                  {
                    ctx.LogWarning(doc, $"Failed copying media file '{found}' for post '{slug}': {ex.Message}");
                  }
                }
                else
                {
                  ctx.LogWarning(doc, $"Referenced media file not found for post '{slug}': .media/{rel}");
                }
              }
            }
            catch (Exception ex)
            {
              // Log but never throw — we must not break site generation
              ctx.LogWarning(doc, $"Media copy module error: {ex.Message}");
            }

            return doc;
          })));
        }
        // Ensure the Archives pipeline writes the root tags page into a folder
        // (e.g. output/tags/index.html) instead of a flat tags.html file.
        if (engine.Pipelines.TryGetValue("Archives", out var archivesPipeline))
        {
          // EXPLANATION:
          // The Statiq Archives pipeline generates both the overall archive index
          // (e.g. /tags) and per-group pages (e.g. /tags/<group>/). Both are
          // rendered using the same template (`theme/input/tags.cshtml`). Relying
          // on the template source path to detect the root page isn't reliable
          // because the pipeline reuses the template for per-group pages as well.
          //
          // To ensure the root archive is written into a folder (`output/tags/index.html`)
          // we override the destination in PostProcess when the document has no
          // `GroupKey` (meaning it's the root) and the `Index` is 1 (or less). This
          // keeps per-group pages (where `GroupKey` is set) writing to
          // `tags/<group>/index.html` while preventing a flat `tags.html` file.
          archivesPipeline.PostProcessModules.Insert(0, new SetDestination(Config.FromDocument<NormalizedPath>(doc =>
          {
            try
            {
              // For archive root pages the GroupKey is null/empty and the Index is 1 (or <=1).
              // For per-group pages GroupKey is set. Use these to detect the root tags page.
              var groupKey = doc?.GetString(Keys.GroupKey);
              var index = doc?.GetInt(Keys.Index) ?? 1;

              if (string.IsNullOrWhiteSpace(groupKey) && index <= 1)
              {
                return new NormalizedPath("tags/index.html");
              }
            }
            catch
            {
              // ignore and fall back to existing destination
            }

            return doc?.Destination ?? new NormalizedPath("tags/index.html");
          })));
        }
      });

      var result = await bootstrapper.RunAsync();
      return result;
    }

    private static string GetSlug(IDocument doc)
    {
      string source = string.Empty;
      if (doc?.Source != null)
      {
        source = doc.Source.ToString();
      }
      string slug = doc?.GetString("Slug");
      if (string.IsNullOrWhiteSpace(slug))
      {
        slug = Path.GetFileNameWithoutExtension(source);
      }
      if (string.IsNullOrWhiteSpace(slug)) slug = "post";

      slug = slug.ToLowerInvariant();
      slug = slug.Normalize(NormalizationForm.FormD);
      var sb = new StringBuilder();
      foreach (var ch in slug)
      {
        var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (cat != UnicodeCategory.NonSpacingMark)
        {
          sb.Append(ch);
        }
      }
      slug = sb.ToString().Normalize(NormalizationForm.FormC);
      slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
      slug = Regex.Replace(slug, @"\s+", "-");
      slug = Regex.Replace(slug, "-+", "-");
      slug = slug.Trim('-');
      if (string.IsNullOrWhiteSpace(slug)) slug = "post";

      return slug;
    }
  }
}