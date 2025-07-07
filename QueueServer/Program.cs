using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TestCore;

namespace QueueServer
{
    class Program
    {
        static void Main(string[] args)
        {
            NamedPipeQueueServer queue = new NamedPipeQueueServer(args[0]);

            queue.Initialize();

            while (!queue.IsConnected)
            {
                Console.ReadLine();
            }

            int i = 0;
            try
            {
                while (queue.IsConnected)
                {
                    queue.Enqueue($"==== {i} ====");

                    Thread.Sleep(200);
                }
            }
            catch
            { }

            queue.Initialize();

            while (!queue.IsConnected)
            {
                Console.ReadLine();
            }

            i = 0;
            while (queue.IsConnected)
            {
                queue.Enqueue($"==== {i} ====");

                Thread.Sleep(200);
            }
        }
    }
}
