using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Web
{
    /// <summary>
    /// 新增（为了Taurus.MVC 动态创建HttpPostedFile参数）
    /// </summary>
    internal class FormFile : IFormFile
    {
        string path;
        public FormFile(string path)
        {
            this.path = path;
        }
        public string ContentType
        {
            get
            {
                return "image/" + Path.GetExtension(path).ToLower().Substring(1);
            }
        }

        public string ContentDisposition => null;

        public IHeaderDictionary Headers => null;

        private long length = -1;
        public long Length
        {
            get
            {
                if (length == -1)
                {
                    Stream stream = OpenReadStream();
                    length = stream.Length;
                    stream.Close();
                }
                return length;
            }
        }

        public string Name => FileName;

        public string FileName
        {
            get
            {
                return Path.GetFileName(path);
            }
        }

        public void CopyTo(Stream target)
        {
            Stream stream = OpenReadStream();
            stream.CopyTo(target);
            stream.Close();
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Stream OpenReadStream()
        {
            return File.OpenRead(path);
        }
    }
}
