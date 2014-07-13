using System.Data.Entity.Migrations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ng.Net1.Models
{
    public class Cash
    {
        public Decimal? StockPur { get; set; }
        public Decimal? StockSold { get; set; }
        public Decimal? Deposited { get; set; }
        public Decimal? Withdrawn { get; set; }
        public Decimal? InHand { get; set; }
    }
    public class Account
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int Type { get; set; }
        [Required]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime TranDt { get; set; }
        [Required]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal Amount { get; set; }
    }
    public  class Trade
    {
        [Key]
        public int ID { get; set; }
        [StringLength(5)] [Required]
        public string Sym{get;set;}
        [Required]
        public int Type{get;set;}
        [Required]
        public int Qty{get;set;}
        [Required] [DisplayFormat(DataFormatString = "{0:c}")]
        public Decimal Price { get; set; }
        public bool DCash{get;set;}
        [Required]
        public Decimal Cmsn { get; set; }
        [Required] [DisplayFormat(DataFormatString="{0:d}")]
        public DateTime Date { get; set; }

    }
    public class User : IdentityUser
    {
        //Validation on the UserName field is done by the CustomUserValidator, the CustomerUserValidator must be initialized and is done so in the AccountController contructor.
        public string phone { get; set; }
        public string zip { get; set; }
        public string firstName { get; set; }
        public string lastname { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<User> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }

        public virtual List<todoItem> todoItems { get; set; }
    }

    public class todoItem
    {
        [Key]
        public int id { get; set; }
        public string task { get; set; }
        public bool completed { get; set; }
    }

    public class DBContext : IdentityDbContext<User>
    {
        public DBContext()
            : base("applicationDB")
        {

        }
        //Override default table names
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new DBInitializer());
            base.OnModelCreating(modelBuilder);

            //When the Model/Database are created, the default user and roles tables will be mapped to different names. EX: IdentityUser -> Users.
            modelBuilder.Entity<IdentityUser>().ToTable("Users");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("UserRoles");
            modelBuilder.Entity<Trade>().ToTable("Trades");
            modelBuilder.Entity<Account>().ToTable("Account");
        }

        public static DBContext Create()
        {
            return new DBContext();
        }

        public DbSet<todoItem> todos { get; set; }
        public DbSet<Trade> trades { get; set; }
        public DbSet<Account> account { get; set; }

    }

    //This function will ensure the database is created and seeded with any default data.
    public class DBInitializer : DropCreateDatabaseIfModelChanges<DBContext>
    {
        protected override void Seed(DBContext context)
        {
            context.Set<Account>().AddOrUpdate(new Account() { Type = 0, Amount = 100, TranDt =  DateTime.Now });
            context.SaveChanges();

            //The UserManager and RoleManager is great for creating default admin users and putting them into the necessary roles.
            var UserManager = new UserManager<User>(new UserStore<User>(context));
            var RoleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

           // Create Role Test and User Test
            List<string> roles = new List<string>() { "Active","Admin" };
            foreach (string role in roles)
            {
                if (!RoleManager.RoleExists(role))
                {
                    var roleresult = RoleManager.Create(new IdentityRole(role));
                }
            }

            //Create User=Admin with password=P@ssword123
            User user = new User();
            user.Email = "someemail@somedomain.com";
            user.UserName = "someemail@somedomain.com";
            var adminresult = UserManager.Create(user, "P@ssword123");

            //Add User Admin to Role Admin
            if (adminresult.Succeeded)
            {
                var result = UserManager.AddToRole(user.Id, "Active");
                result = UserManager.AddToRole(user.Id, "Admin");
            }
        }
    }
}

