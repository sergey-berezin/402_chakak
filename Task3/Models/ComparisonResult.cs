using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace Task3.Models
{
    internal struct ComparisonResult
    {
        public float distance { get; set; }
        public float similarity { get; set; }

        public override string ToString() => $"distance: {distance:f2}; similarity: {similarity:f2}";
    }

    internal class ComparisonResultCreator
    {
        public static ComparisonResult[,] Create(IList<float[]> embeddings)
        {
            var result_matrix = new ComparisonResult[embeddings.Count, embeddings.Count];

            for (int idx = 0; idx < embeddings.Count; ++idx)
            {
                var distance = (float)Math.Pow(Distance(embeddings[idx], embeddings[idx]), 2);
                var similarity = Similarity(embeddings[idx], embeddings[idx]);

                result_matrix[idx, idx].distance = distance;
                result_matrix[idx, idx].similarity = similarity;

                for (int jdx = idx + 1; jdx < embeddings.Count; ++jdx)
                {
                    distance = (float)Math.Pow(Distance(embeddings[idx], embeddings[jdx]), 2);
                    similarity = Similarity(embeddings[idx], embeddings[jdx]);

                    result_matrix[idx, jdx].distance = distance;
                    result_matrix[idx, jdx].similarity = similarity;

                    result_matrix[jdx, idx].distance = distance;
                    result_matrix[jdx, idx].similarity = similarity;
                }
            }

            return result_matrix;
        }

        private static float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());
        private static float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
        private static float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();
    }
}
