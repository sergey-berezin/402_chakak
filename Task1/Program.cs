using ModelsComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());
float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();

using var embedComponent = new ArcFaceComponent();

var imagesPaths = new List<string>();

Console.WriteLine("Enter paths for face images (end of the input is an empty line):");

string line;

while ((line = Console.ReadLine()) != "")
{
    imagesPaths.Add(line);
}

var embeddings = new List<float[]>();

var cts = new CancellationTokenSource();

Console.WriteLine("Predicting contents of image...\n");

await foreach (var item in embedComponent.GetEmbeddingAsync(imagesPaths).WithCancellation(cts.Token))
{
    embeddings.Add(item);
}

cts.Cancel();

for (int idx = 0; idx < embeddings.Count; ++idx)
{
    for (int jdx = idx + 1; jdx < embeddings.Count; ++jdx)
    {
        Console.WriteLine($"Сomparison of face{idx + 1} and face{jdx + 1}:");
        Console.WriteLine($"Distance =  {(float)Math.Pow(Distance(embeddings[idx], embeddings[jdx]), 2)}");
        Console.WriteLine($"Similarity =  {Similarity(embeddings[idx], embeddings[jdx])}\n");
    }
}

