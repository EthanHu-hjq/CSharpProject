using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore;

namespace QueueClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //MessageBox.Show(string.Join(" ", args));
            NamedPipeQueueClient queue = new NamedPipeQueueClient(args[0]);

            try
            {
                queue.Initialize();
            }
            catch
            { }

            try
            {
                Console.WriteLine(queue.Dequeue());

                queue.Close();
                queue.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{queue.Name} --> {queue.IsConnected}: Err {ex}", "Warning");
            }
        }
    }
}
