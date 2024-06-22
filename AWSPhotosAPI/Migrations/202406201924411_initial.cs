namespace AWSPhotosAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Photos",
                c => new
                    {
                        PhotoId = c.Int(nullable: false, identity: true),
                        PhotoName = c.String(nullable: false, maxLength: 255),
                        UploadDate = c.DateTime(nullable: false),
                        UserNameUp = c.String(nullable: false),
                        BucketName = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => t.PhotoId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Photos");
        }
    }
}
