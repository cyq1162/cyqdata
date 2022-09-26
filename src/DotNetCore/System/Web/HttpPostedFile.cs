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
    public class HttpPostedFile
    {
        IFormFile file;
        public HttpPostedFile(IFormFile file)
        {
            this.file = file;
        }
        public HttpPostedFile(string path)
        {
            file = new FormFile(path);
        }
        // 摘要:
        //     获取上载文件的大小（以字节为单位）。
        //
        // 返回结果:
        //     文件长度（以字节为单位）。
        public long ContentLength => file.Length;
        //
        // 摘要:
        //     获取客户端发送的文件的 MIME 内容类型。
        //
        // 返回结果:
        //     上载文件的 MIME 内容类型。
        public string ContentType => file.ContentType;
        //
        // 摘要:
        //     获取客户端上的文件的完全限定名称。
        //
        // 返回结果:
        //     客户端的文件的名称，包含目录路径。
        public string FileName => file.FileName;
        //
        // 摘要:
        //     获取一个 System.IO.Stream 对象，该对象指向一个上载文件，以准备读取该文件的内容。
        //
        // 返回结果:
        //     指向文件的 System.IO.Stream。
        public Stream InputStream => file.OpenReadStream();

        // 摘要:
        //     保存上载文件的内容。
        //
        // 参数:
        //   filename:
        //     保存的文件的名称。
        //
        // 异常:
        //   System.Web.HttpException:
        //     System.Web.Configuration.HttpRuntimeSection 对象的 System.Web.Configuration.HttpRuntimeSection.RequireRootedSaveAsPath
        //     属性设置为 true，但 fileName 不是绝对路径。
        public void SaveAs(string fileName)
        {
            using (FileStream fs = System.IO.File.Create(fileName))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
        }
    }
}
