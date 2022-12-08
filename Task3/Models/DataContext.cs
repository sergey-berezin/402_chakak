using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Task3.Models
{
    internal class DataContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }
        public DbSet<byteImage> bytes { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"DataSource=C:\\Users\\Ирина\\Desktop\\2811\\Task3\\Database.db");
    }
    public class Image
    {
        public int ImageId { get; set; }
        public string Name { get; set; }
        public string Hashcode { get; set; }
    }
    public class Embedding
    {
        public int EmbeddingId { get; set; }
        public float distance { get; set; }

        public float similarity { get; set; }

        public int ImageId { get; set; }

    }
    public class byteImage
    {

        public byte[] BLOB { get; set; }
        [Key]
        [ForeignKey(nameof(Image))]
        public int ImageId { get; set; }
    }
}