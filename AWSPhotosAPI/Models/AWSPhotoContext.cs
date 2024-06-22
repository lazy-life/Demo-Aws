using System.Collections.Generic;
using System.Data.Entity;

namespace AWSPhotosAPI.Models
{
    public class AWSPhotoContext : DbContext
    {
        public AWSPhotoContext() : base(@"server=KIKO\SQLEXPRESS;Database=AWSPhoto;Trusted_Connection=True;")
        {
        }
        public DbSet<Photo> Photos { get; set; }
    }
}
