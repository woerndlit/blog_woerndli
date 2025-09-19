using System.Threading.Tasks;
using Statiq.App;
using Statiq.Web;

namespace MySite
{
  public class Program
  {
    public static async Task<int> Main(string[] args)
    {
      var result = await Bootstrapper
        .Factory
        .CreateWeb(args)
        .RunAsync();
      return result;
    }
  }
}