using AutoUpdaterDotNET;

namespace Frame
{
    public static class UpdateManager
    {
        public static void CheckForUpdate()
        {
            AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");
        }
    }
}