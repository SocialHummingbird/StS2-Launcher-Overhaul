using System.IO;
using System.Net.Http;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private sealed class DeleteOnDisposeFileContent : StreamContent
    {
        private readonly string _path;
        private readonly long _length;

        private DeleteOnDisposeFileContent(string path)
            : this(path, OpenResponseFile(path)) { }

        private DeleteOnDisposeFileContent(string path, FileStream stream)
            : base(stream)
        {
            _path = path;
            _length = new FileInfo(path).Length;
            Headers.ContentLength = _length;
        }

        private static FileStream OpenResponseFile(string path)
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024
            );
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DeleteFileQuietly(_path);
        }
    }
}
