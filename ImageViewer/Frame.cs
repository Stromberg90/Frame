using System;

namespace Frame
{
    public class FrameApp
    {
        [STAThread]
        static void Main(string[] args)
        {
            var manager = new SingleAppMangager();
            manager.Run(args);
        }
    }
}