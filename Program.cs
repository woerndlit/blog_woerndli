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

            // Derive a slug: OPTION 1 -> prefer "Slug" metadata, otherwise use the source filename
            string source = doc.Source.ToString() ?? string.Empty;
            string slug = doc.GetString("Slug");
            if (string.IsNullOrWhiteSpace(slug))
            {
              slug = Path.GetFileNameWithoutExtension(source);
            }

            if (string.IsNullOrWhiteSpace(slug)) slug = "post";

            // Simple slugify: lowercase, remove diacritics, keep alphanumerics, spaces -> '-', collapse dashes
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

            return new NormalizedPath($"posts/{slug}/index.html");
          })));

          // Add a module to copy referenced .media files into each post's output folder
          // The module scans the document content for ".media/..." references and copies
          // those files from the input posts .media folder into the post's output .media folder.
          // It returns the original document to avoid replacing its content.
          // Insert this after the SetDestination module so we can derive the output folder
          // from the document's destination reliably.
          contentPipeline.PostProcessModules.Insert(1, new ExecuteConfig(Config.FromDocument((doc, ctx) =>
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
                return doc;
              }

              // Find .media references anywhere in the content (capture into named group 'path')
              var mediaRegex = new Regex(@"\.media/(?<path>[A-Za-z0-9_\-./]+)", RegexOptions.IgnoreCase);
              var matches = mediaRegex.Matches(content);
              if (matches.Count == 0)
              {
                return doc;
              }

              // Compute a normalized slug from metadata or source for logging/fallback
              string sourceForSlug = doc.Source.ToString() ?? string.Empty;
              string slug = doc.GetString("Slug");
              if (string.IsNullOrWhiteSpace(slug)) slug = Path.GetFileNameWithoutExtension(sourceForSlug);
              if (string.IsNullOrWhiteSpace(slug)) slug = "post";

              slug = slug.ToLowerInvariant();
              slug = slug.Normalize(NormalizationForm.FormD);
              var sbSlug = new StringBuilder();
              foreach (var ch in slug)
              {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                  sbSlug.Append(ch);
                }
              }
              slug = sbSlug.ToString().Normalize(NormalizationForm.FormC);
              slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
              slug = Regex.Replace(slug, @"\s+", "-");
              slug = Regex.Replace(slug, "-+", "-");
              slug = slug.Trim('-');
              if (string.IsNullOrWhiteSpace(slug)) slug = "post";

              // Determine output media folder from the document destination (set by SetDestination)
              // Fallback to the slug-derived output path.
              string destPath = doc.Destination.ToString() ?? string.Empty;
              string outputPostMediaBase;
              if (!string.IsNullOrWhiteSpace(destPath))
              {
                var destDir = destPath.Replace('/', Path.DirectorySeparatorChar);
                destDir = Path.GetDirectoryName(destDir) ?? "posts";
                outputPostMediaBase = Path.Combine(ctx.FileSystem.RootPath.FullPath, destDir, ".media");
              }
              else
              {
                outputPostMediaBase = Path.Combine(ctx.FileSystem.RootPath.FullPath, "output", "posts", slug, ".media");
              }
              Directory.CreateDirectory(outputPostMediaBase);

              foreach (Match m in matches)
              {
                var rel = m.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar).TrimStart('.', Path.DirectorySeparatorChar);
                var inputPath = Path.Combine(ctx.FileSystem.RootPath.FullPath, "input", "posts", ".media", rel);
                if (File.Exists(inputPath))
                {
                  try
                  {
                    var dest = Path.Combine(outputPostMediaBase, Path.GetFileName(inputPath));
                    File.Copy(inputPath, dest, true);
                  }
                  catch (Exception ex)
                  {
                    ctx.LogWarning(doc, $"Failed copying media file '{inputPath}' for post '{slug}': {ex.Message}");
                  }
                }
                else
                {
                  ctx.LogWarning(doc, $"Referenced media file not found: {inputPath} for post '{slug}'");
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
      });

      var result = await bootstrapper.RunAsync();
      return result;
    }
  }
}