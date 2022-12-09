using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace ModelsComponents
{
    public class ArcFaceComponent : IDisposable
    {

        readonly InferenceSession _session;
        public ArcFaceComponent()
        {
            using var modelStream = typeof(ArcFaceComponent).Assembly.GetManifestResourceStream("ArcFace.onnx");
            using var memoryStream = new MemoryStream();
            modelStream.CopyTo(memoryStream);
            _session = new InferenceSession(memoryStream.ToArray());
        }
        public async Task<float[]> GetEmbeddingAsync(byte[] byte_img, CancellationToken cancellationToken = default)
        {
            return await EmbeddingAsync(byte_img, cancellationToken).ConfigureAwait(false);
        }

        private Task<float[]> EmbeddingAsync(byte[] face, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(result => {
                bool sessionLock = false;

                float[] embedding = null;

                try
                {
                    Monitor.Enter(_session, ref sessionLock);
                    embedding = GetEmbeddings(face, _session);
                }
                finally
                {
                    if (sessionLock)
                    {
                        Monitor.Exit(_session);
                    }
                }

                return embedding;
            },
            cancellationToken, TaskCreationOptions.LongRunning);
        }

        private static DenseTensor<float> ImageToTensor(Image<Rgb24> img)
        {
            var width = 112;
            var height = 112;
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            img.ProcessPixelRows(pa => {
                for (int y = 0; y < height; y++)
                {
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        tensor[0, 0, y, x] = pixelSpan[x].R;
                        tensor[0, 1, y, x] = pixelSpan[x].G;
                        tensor[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return tensor;
        }

        private static float[] Normalize(float[] v)
        {
            var len = (float)Math.Sqrt(v.Select(x => x * x).Sum());
            return v.Select(x => x / len).ToArray();
        }

        private static float[] GetEmbeddings(byte[] _face, InferenceSession session)
        {
            var stream1 = new MemoryStream(_face);
            var face = Image.Load<Rgb24>(stream1);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
            return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}

