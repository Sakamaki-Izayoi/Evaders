namespace Evaders
{
    using System;
    using Data;
    using Game;
    using Game.Servers;
    using Game.Supervisors;
    using JetBrains.Annotations;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Server;
    using Server.Integration;
    using Services;
    using Services.Factories;
    using Services.Providers;

    [UsedImplicitly]
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public IGameServer GameServer { get; set; }


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
            services.Configure<GameServerSettings>(e =>
            {
                e.MatchmakingProviderId = "default";
                e.ServerConfigurationProviderId = "default";
                e.SupervisorProviderId = "default";
            });
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

            ConfigureGameServer(services);
        }

        public void ConfigureGameServer(IServiceProvider services)
        {
            var settings = services.GetService<IOptions<GameServerSettings>>().Value;

            services.GetService<IProviderFactory<IServerSupervisor>>().AddProvider(new DefaultProvider<IServerSupervisor>("default", () => new DefaultServerSupervisor(services.GetService<ILogger<DefaultServerSupervisor>>())));
            services.GetService<IProviderFactory<IMatchmaking>>().AddProvider(new DefaultProvider<IMatchmaking>("default", () => new Matchmaking("default", 15f, services.GetService<ILogger<Matchmaking>>())));
            services.GetService<IProviderFactory<ServerSettings>>().AddProvider(new DefaultProvider<ServerSettings>("default", () => new ServerSettings()));
            // todo load ServerSettings from gamsettings.json


            var gameServer = services.GetService<IGameServer>();
            GameServer = gameServer;

            gameServer.Start();

            //services.GetService<IProviderFactory<IServerSupervisor>>().AddProvider(new DefaultProvider<IServerSupervisor>("default", () => ));
        }
    }
}