using System.Threading.Tasks;
using Statiq.App;
using Statiq.Web;

namespace MySite
{
  public class Program
  {
    public static async Task<int> Main(string[] args)
    {
      System.Console.WriteLine("Starting Statiq Bootstrapper...");
      var result = await Bootstrapper
        .Factory
        .CreateWeb(args)
        .RunAsync();
      System.Console.WriteLine($"Bootstrapper finished with exit code {result}");
      return result;
    }
  }
}