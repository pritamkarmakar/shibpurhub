namespace ShibpurConnectWebApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                 "dbo.Questions",
                 c => new
                 {
                     QuestionId = c.String(nullable: false, maxLength: 128),
                     Title = c.String(nullable: false, maxLength: 128),
                     Description = c.String(nullable: false, maxLength: 128),
                     HasAnswered = c.Boolean(nullable: true),
                     PostedOnUtc = c.DateTime(nullable: false),
                     UserId = c.String(nullable: false, maxLength: 128),
                 })
                 .PrimaryKey(t => t.QuestionId)
                 .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: false)
                 .Index(t => t.Title);

            CreateTable(
                 "dbo.Comments",
                 c => new
                 {
                     CommentId = c.String(nullable: false, maxLength: 128),
                     Description = c.String(nullable: false, maxLength: 128),
                     PostedOnUtc = c.DateTime(nullable: false),
                     QuestionId = c.String(nullable: false, maxLength: 128),
                     UserId = c.String(nullable: false, maxLength: 128),
                 })
                 .PrimaryKey(t => t.CommentId)
                 .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: false)
                 .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: false)
                 .Index(t => t.Description);
           
            CreateTable(
                 "dbo.Categories",
                 c => new
                 {
                     CategoryId = c.String(nullable: false, maxLength: 128),
                     Name = c.String(nullable: false, maxLength: 128),
                 })
                 .PrimaryKey(t => t.CategoryId)
                 .Index(t => t.Name);

            CreateTable(
                 "dbo.CategoryTaggings",
                 c => new
                 {
                     Id = c.String(nullable: false, maxLength: 128),
                     QuestionId = c.String(nullable: false, maxLength: 128),
                     CategoryId = c.String(nullable: false, maxLength: 128),
                 })
                 .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: false)
                 .ForeignKey("dbo.Categories", t => t.CategoryId, cascadeDelete: false)
                 .Index(t => t.Id);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetRoles");
            //DropTable("dbo.Comments");
        }
    }
}
