using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1
{
    public class DataContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }
        public DbSet<byteImage> bytes { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"DataSource=Database.db");
        public DataContext(DbContextOptions<DataContext> options): base(options){ }
    }
    public class Image
    {
        public int ImageId { get; set; }
        public string Hashcode { get; set; }
    }
    public class Embedding
    {
        public int EmbeddingId { get; set; }

        public float distance { get; set; }
        public float similarity { get; set; }

        public int PairImage1 { get; set; }
        public int PairImage2 { get; set; }

        public int ImageId { get; set; }
        public Image Image { get; set; }

    }
    public class byteImage
    {
        [Key]
        [ForeignKey(nameof(Image))]
        public int ImageId { get; set; }
        public string Name { get; set; }
        public byte[] BLOB { get; set; }
    }
}
