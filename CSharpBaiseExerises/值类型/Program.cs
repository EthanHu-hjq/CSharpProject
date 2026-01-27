using System;

namespace ValueType
{
    public struct MutablePoint
    {
        public int X;
        public int Y;
        public MutablePoint(int x, int y) => (X, Y) = (x, y);

        //重写ToString方法,当用Console.WriteLine打印时,会调用ToString方法
        public override string ToString() => $"({X}, {Y})";
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            var p1 = new MutablePoint(1, 2);
            var p2 = p1;
            p2.X = 200;
            Console.WriteLine($"{nameof(p1)} after {nameof(p2)} is modified: {p1}");
            Console.WriteLine($"{nameof(p2)}:{p2}");
            MutateAndDisplay(p2);
            Console.WriteLine($"{nameof(p2)} after mutation: {p2}");

        }

        private static void MutateAndDisplay(MutablePoint p)
        {
            p.X = 100;
            Console.WriteLine($"Point after mutation: {p}");
        }
    }
}
