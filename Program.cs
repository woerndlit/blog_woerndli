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
          contentPipeline.PostProcessModules.Insert(0, new ExecuteConfig(Config.FromDocument((doc, ctx) =>
          {
            try
            {
              if (!doc.GetBool("IsPost"))
              {
                return (object)0;
              }

              // Determine slug same as destination logic
              string source = doc.Source.ToString() ?? string.Empty;
              string slug = doc.GetString("Slug");
              if (string.IsNullOrWhiteSpace(slug))
              {
                slug = Path.GetFileNameWithoutExtension(source);
              }
              if (string.IsNullOrWhiteSpace(slug)) slug = "post";

              // Find .media references in the document content (markdown image/link patterns)
              string content = doc.GetContentStringAsync().Result.ToString();
              var mediaRegex = new Regex("!\\[[^\\]]*\\]\\(\\.media/(?<file>[^\\)\\s'\"]+)\\)", RegexOptions.Compiled);
              var matches = mediaRegex.Matches(content);
              if (matches.Count == 0) return (object)0;

              // Source and destination directories
              var inputPostsMedia = ctx.FileSystem.GetInputFile("posts/.media");
              if (!inputPostsMedia.Exists)
              {
                return (object)0;
              }

              var destDir = ctx.FileSystem.GetOutputDirectory($"posts/{slug}/.media");
              if (!destDir.Exists)
              {
                destDir.Create();
              }

              foreach (Match m in matches)
              {
                var fileName = m.Groups["file"].Value;
                var src = ctx.FileSystem.GetInputFile(Path.Combine("posts", ".media", fileName));
                if (!src.Exists) continue;
                var dst = ctx.FileSystem.GetOutputFile(Path.Combine("posts", slug, ".media", fileName));
                using (var inStream = src.OpenRead())
                using (var outStream = dst.OpenWrite())
                {
                  inStream.CopyTo(outStream);
                }
              }

              return (object)0;
            }
            catch { /* swallow errors to not fail the build */ return (object)0; }
          })));
        }
      });

      var result = await bootstrapper.RunAsync();
      return result;
    }
  }
}