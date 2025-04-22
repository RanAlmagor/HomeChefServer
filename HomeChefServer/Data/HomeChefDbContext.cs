using HomeChefServer.Controllers;
using Microsoft.EntityFrameworkCore;

namespace HomeChefServer.Data
{
    public class HomeChefDbContext : DbContext
    {
        public HomeChefDbContext(DbContextOptions<HomeChefDbContext> options) : base(options)
        {
        }

    
    }
}
