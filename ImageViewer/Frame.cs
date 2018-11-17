using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;

namespace Frame
{
  public static class FrameApp
  {
    [STAThread]
    static void Main(string[] args)
    {
      ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
      ProfileOptimization.StartProfile("Startup.Profile");
      new SingleAppMangager().Run(args);
    }
  }
}