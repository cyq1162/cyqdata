using System.IO;
namespace CYQ.Data.Tool
{
    internal class IOInfo : FileSystemInfo
    {
        public IOInfo(string fileName)
        {
            base.FullPath = fileName;
        }
        public override void Delete()
        {
        }

        public override bool Exists
        {
            get
            {
                return File.Exists(base.FullPath);
            }
        }

        public override string Name
        {
            get
            {
                return null;
            }
        }
    }
}
