using System;
using System.Threading.Tasks;

namespace Radarsbutt.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tts = await Bot.Init();
            Console.ReadLine();
        }
    }
}
