using AkBarsTest.Command;
using System.Configuration;

namespace AkBarsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            (new SendDirectoryToYandexDiskCommand()).Execute(args[0] ?? "", args[1] ?? "");
        }
    }
}
