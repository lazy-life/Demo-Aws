using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AWSPhotosAPI.Models
{
    [Table("Photos")]
    public class Photo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhotoId { get; set; }
        [Required]
        [MaxLength(255)]
        public string PhotoName { get; set; }
        [Required]
        public DateTime UploadDate { get; set; }
        [Required]
        public string UserNameUp { get; set; }
        [Required, MaxLength(255)]
        public string BucketName { get; set; }
    }
}
