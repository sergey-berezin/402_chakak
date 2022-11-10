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
        const string _MODEL_NAME = "ArcFaceComponent.arcfaceresnet100-8.onnx";

        public ArcFaceComponent()
        {
            using var modelStream = typeof(ArcFaceComponent).Assembly.GetManifestResourceStream(_MODEL_NAME);
            using var memoryStream = new MemoryStream();
            modelStream.CopyTo(memoryStream);
            _session = new InferenceSession(memoryStream.ToArray());
        }

        public async IAsyncEnumerable<float[]> GetEmbeddingAsync(IEnumerable<string> imagesPaths, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var imagePath in imagesPaths)
            {
                using var curFace = Image.Load<Rgb24>(imagePath);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return await EmbeddingAsync(curFace, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task<float[]> EmbeddingAsync(Image<Rgb24> face, CancellationToken cancellationToken)
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
            var width = img.Width;
            var height = img.Height;
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

        private static float[] GetEmbeddings(Image<Rgb24> face, InferenceSession session)
        {
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




