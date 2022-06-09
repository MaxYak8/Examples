// From startup.cs

services.AddDatabaseContext(Configuration);

// AddDatabaseContext

public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                context.Database.Migrate();
            }

            return services;
        }
		
// ApplicationDbContext.cs

public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Region> Regions { get; set; }

        public DbSet<Schedule> Schedules { get; set; }
		
// ... other tables

protected override void OnModelCreating(ModelBuilder builder)
        {
            UpdateStructure(builder);
        }
		
// PK, FK, indexes settings

private void UpdateStructure(ModelBuilder builder)
        {
			builder.Entity<ScheduleSetting>()
                .HasKey(p => p.Id);
				
			builder.Entity<Schedule>()
             .HasIndex(p => new { p.HomeTeamId, p.AwayTeamId, p.TournamentId, p.GameDate, p.Round })
             .IsUnique();
			 
			 builder.Entity<Region>()
                .HasMany(c => c.Teams)
                .WithOne(e => e.Region)
            .HasForeignKey(u => u.RegionId);
		}
		
// BaseRepository.cs

public async Task<TEntity> InsertAsync(TEntity entity)
        {
            await context.Set<TEntity>().AddAsync(entity);
            await context.SaveChangesAsync();

            return entity;
        }

public async Task UpdateRangeAsync(List<TEntity> entities)
        {
            foreach (var entityToUpdate in entities)
            {

                if (context.Entry(entityToUpdate).State == EntityState.Detached)
                {
                    dbSet.Attach(entityToUpdate);
                }
                context.Entry(entityToUpdate).State = EntityState.Modified;
                context.ChangeTracker.AutoDetectChangesEnabled = false;
            }
            await context.SaveChangesAsync();
        }
		
public async Task<List<TEntity>> FindNumberByWhereOrderedAscendingAsync(Expression<Func<TEntity, bool>> match, Expression<Func<TEntity, object>> orderBy, int number)
        {
            return await context.Set<TEntity>().Where(match).OrderBy(orderBy).Take(number).ToListAsync();
        }
		

// From custom repository

public async Task<Team> FindTeamByNameAsync(string name, Guid regionId)
      {
         var nameWithoutDiacritic = RemoveDiacritics(name);

         nameWithoutDiacritic = nameWithoutDiacritic.Replace('ø', 'o');
         nameWithoutDiacritic = nameWithoutDiacritic.Replace('ı', 'i');
         nameWithoutDiacritic = nameWithoutDiacritic.Replace("æ", "ae");
         nameWithoutDiacritic = nameWithoutDiacritic.Replace("ł", "l");
         nameWithoutDiacritic = nameWithoutDiacritic.Replace("đ", "d");

         var teams = await FindAllByWhereAsync(q => EF.Functions.Like(q.Name, name)  &&
                                                    q.RegionId == regionId);

         if (teams == null || !teams.Any())
         {
            throw new Exception($"{name} team does not exist");
         }

         if (teams.Count > 1)
         {
            return teams.OrderBy(q => q.ExternalId).First();
         }

         return teams.First();
      }
	  
private string RemoveDiacritics(string text)
      {
         return String.Concat(text
            .Normalize(NormalizationForm.FormD)
             .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
           ).Normalize(NormalizationForm.FormC);
      }
	  

	  

