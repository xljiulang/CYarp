using System.IO;
using System.IO.Pipelines;

namespace CYarp.Client.AspNetCore
{
    sealed class CYarpDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public CYarpDuplexPipe(Stream stream)
        {
            this.Input = PipeReader.Create(stream);
            this.Output = PipeWriter.Create(stream);
        }
    }
}
