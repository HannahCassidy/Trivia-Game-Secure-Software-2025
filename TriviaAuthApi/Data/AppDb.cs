using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDb : IdentityDbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
}
