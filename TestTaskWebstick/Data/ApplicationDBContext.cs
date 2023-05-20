﻿using Microsoft.EntityFrameworkCore;
using TestTaskWebstick.Models;

namespace TestTaskWebstick.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }
        public DbSet<Image> Images { get; set; }
    }
}
