using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using ModelsComponents;
using Microsoft.Build.Framework;
using static Microsoft.CodeAnalysis.AssemblyIdentityComparer;
using NuGet.Protocol;

namespace WebApplication1
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly DataContext _context;
        ArcFaceComponent embeder = new ArcFaceComponent();
        public ImagesController(DataContext context)
        {
            _context = context;
        }


        // GET: api/Images

        [HttpGet("Res")]
        public async Task<string> GetRes()
        {
            ComparisonResult[,] ComparisonResults = new ComparisonResult[_context.Embeddings.Count(), _context.Embeddings.Count()];
            string str = "";
            foreach (var item in _context.Embeddings)
            {
                str += item.PairImage1.ToString() + " " + item.PairImage2.ToString() + " " + item.distance.ToString() + " " + item.similarity.ToString()+"\n";
            }
        
            return str;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<byteImage>>> GetImages()
        {
            return await _context.bytes.ToListAsync();
        }
        [HttpPost("PairId")]
        public async Task<string> PostPariId(int ID1,int ID2,CancellationToken Token)
        {
            string res = "";
            var q1 = _context.Embeddings.Where(e => e.ImageId == ID1);
            var q2 = _context.Embeddings.Where(e => e.ImageId == ID2);
            foreach(var item in q1)
            {
                if(q2.Any(q=>q.PairImage2==item.PairImage1))
                {
                    var q3 = q2.Where(q => q.PairImage2 == item.PairImage1).FirstOrDefault();
                    res += q3.distance + " " + q3.similarity + "\n";
                    return res;
                }
            }
            return res;
        }

        // POST: api/Images
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<List<int>> PostImage(List<byteImage> images, CancellationToken Token)
        {
            var embeddings = new List<float[]>();
            List<int> ImagesId = new List<int>();
            ComparisonResult[,] ComparisonResults;
            foreach (var image in images)
            {
                string hashcode = GetHash(image.BLOB);
                var q1 = _context.Images.Where(h => h.Hashcode == hashcode);
                if (q1.FirstOrDefault() != null)
                {
                    foreach (var item in q1)
                    {
                        var q2 = _context.bytes.Where(b => b.ImageId == item.ImageId).FirstOrDefault();
                        if (q2 != null && q2.BLOB != image.BLOB)
                        {
                            _context.Images.Add(new Image { Hashcode = hashcode });
                            _context.SaveChanges();
                            var query = _context.Images.OrderBy(d => d.ImageId).Last();
                            ImagesId.Add(query.ImageId);
                            _context.bytes.Add(new byteImage { ImageId = query.ImageId , Name=image.Name,BLOB = image.BLOB });
                            _context.SaveChanges();
                            embeddings.Add(await embeder.GetEmbeddingAsync(image.BLOB,Token));
                            
                        }
                    }
                }
                else
                {
                    _context.Images.Add(new Image { Hashcode = hashcode });
                    _context.SaveChanges();
                    var query = _context.Images.OrderBy(d => d.ImageId).Last();
                    ImagesId.Add(query.ImageId);
                    _context.bytes.Add(new byteImage { ImageId = query.ImageId, Name = image.Name, BLOB = image.BLOB });
                    _context.SaveChanges();
                    embeddings.Add(await embeder.GetEmbeddingAsync(image.BLOB,Token));
                }
              
            }
            if(embeddings.Count > 0)
            {
                ComparisonResults = ComparisonResultCreator.Create(embeddings);

                int i = 0;
                foreach (var item in _context.Images)
                {
                    for (int j = 0; j < ComparisonResults.GetLength(0); j++)
                    {
                        _context.Embeddings.Add(new Embedding { ImageId = item.ImageId, distance = ComparisonResults[i, j].distance, similarity = ComparisonResults[i, j].similarity, PairImage1 = i, PairImage2 = j });
                    }
                    i++;
                }
                _context.SaveChanges();
            }
           
            return ImagesId;
        }

        // DELETE: api/Images
        [HttpDelete]
        public async Task<IActionResult> DeleteImage()
        {
            if (_context.Images.Any())
                foreach (var item in _context.Images)
                {
                    _context.Images.Remove(item);
                }
            if (_context.Embeddings.Any())
                foreach (var item in _context.Embeddings)
                {
                    _context.Embeddings.Remove(item);
                }
            if (_context.bytes.Any())
                foreach (var item in _context.bytes)
                {
                    _context.bytes.Remove(item);
                }
            await _context.SaveChangesAsync();
            return NoContent();
        }
        public static string GetHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }
        private bool ImageExists(int id)
        {
            return _context.Images.Any(e => e.ImageId == id);
        }
    }
}
