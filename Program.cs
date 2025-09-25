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
              var groupKey = doc?.GetString(Keys.GroupKey);
              var index = doc?.GetInt(Keys.Index) ?? 1;
              var archiveKey = doc?.GetString("ArchiveKey");

              // Only override the destination when this is the root of the Tags archive.
              if (string.Equals(archiveKey, "Tags", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(groupKey) && index <= 1)
              {
                return Task.FromResult(new NormalizedPath("tags/index.html"));
              }
            }
            catch
            {
              // ignore and fall back to existing destination
            }

            // Only override when root tags archive detected. Otherwise return the
            // existing destination (or null to let Statiq decide) as a completed task.
            var existing = doc?.Destination ?? (NormalizedPath?)null;
            return Task.FromResult(existing ?? new NormalizedPath(""));
          })));
        }
      });

      var result = await bootstrapper.RunAsync();

#if DEBUG
      // In Debug builds copy any per-post ".media" folders from input/posts
      // into the generated output/posts/<slug>/.media so local servers can
      // resolve media referenced as ".media/...". We do this after Statiq
      // finishes so the output structure already exists.
      try
      {
        var cwd = Directory.GetCurrentDirectory();
        var inputPostsDir = Path.Combine(cwd, "input", "posts");
        var outputPostsDir = Path.Combine(cwd, "output", "posts");

        if (Directory.Exists(inputPostsDir) && Directory.Exists(outputPostsDir))
        {
          foreach (var postDir in Directory.GetDirectories(inputPostsDir))
          {
            var mediaSrc = Path.Combine(postDir, ".media");
            if (!Directory.Exists(mediaSrc))
            {
              continue;
            }

            var postName = Path.GetFileName(postDir);
            var destMediaDir = Path.Combine(outputPostsDir, postName, ".media");
            Directory.CreateDirectory(destMediaDir);

            foreach (var srcFile in Directory.GetFiles(mediaSrc, "*", SearchOption.AllDirectories))
            {
              var relative = Path.GetRelativePath(mediaSrc, srcFile);
              var destPath = Path.Combine(destMediaDir, relative);
              Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? destMediaDir);
              File.Copy(srcFile, destPath, true);
            }
          }
        }
      }
      catch (Exception ex)
      {
        // Don't fail the build/run if copying media fails; just log in debug output.
        Console.WriteLine($"[DEBUG] Copying .media failed: {ex.Message}");
      }
#endif

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