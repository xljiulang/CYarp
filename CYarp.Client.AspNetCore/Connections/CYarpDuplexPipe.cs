using System.IO;
using System.IO.Pipelines;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed class CYarpDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public CYarpDuplexPipe(Stream stream)
        {
            Input = PipeReader.Create(stream);
            Output = PipeWriter.Create(stream);
        }
    }
}
