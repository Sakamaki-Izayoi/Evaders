namespace Evaders
{
    using System;
    using System.Linq;
    using Data;
    using JetBrains.Annotations;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Models;
    using Server;
    using Services;
    using Services.Factories;

    [UsedImplicitly]
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }


        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

            if (env.IsDevelopment())
                builder.AddUserSecrets();

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }


        public void ConfigureServices(IServiceCollection services)
        {
            /*
             * Data base setup
             */
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            /*
             * Game server setup
             */
            services.AddSingleton<IGameServer, DefaultGameServer>();
            // services.Configure<GameServerSettings>(e => );
            services.AddSingleton<IGameServer, DefaultGameServer>();
            services.AddSingleton(typeof(IProviderFactory<>), typeof(DefaultFactory<>));
            
            /*
             * MVC setup
             */
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IServiceProvider services)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            /*
             * Register exception handler
             */
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            /*
             * Static files
             */
            app.UseStaticFiles();

            /*
             * Identity (auth)
             */
            app.UseIdentity();

            /*
             * MVC
             */
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });


            ConfigureProviders(services);
        }


        public void ConfigureProviders(IServiceProvider services)
        {
            
        }
    }
}