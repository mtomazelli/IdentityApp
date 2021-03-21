using System;
using IdentityApp.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using IdentityApp.Models;
using IdentityApp.Helpers;
using IdentityApp.Services;
using System.Reflection;

namespace IdentityApp
{
    public class Startup
    {

        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("BenefitsIdentity");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            
            // DbContext do Identity
            services.AddDbContext<ApplicationDbContext>(options => {
                options.UseSqlServer(connectionString);   
            });

            // Configuração do Identity
            services.AddIdentity<AppUser, AppRole>(options => {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true; // Usuário só poderá logar depois da confirmação por email
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
                .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configuração do Identity Server 4
            services.AddIdentityServer()
                .AddAspNetIdentity<AppUser>()
                .AddConfigurationStore(options => {
                    options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString, opt => {
                        opt.MigrationsAssembly(migrationsAssembly);
                    });
                    options.DefaultSchema = "IdentityServer";
                })
                .AddOperationalStore(options => {
                    options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString, opt => {
                        opt.MigrationsAssembly(migrationsAssembly);
                    });
                    options.DefaultSchema = "IdentityServer";
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30;
                })
                .AddDeveloperSigningCredential(); // Isso é para DEV somente

            // Configuração do SMTP
            services.Configure<SmtpOptions>(Configuration.GetSection("Smtp")); // Permite injetar IOptions com SmtpOptions

            // Injeção do serviço de email
            services.AddSingleton<IEmailSender, SmtpEmailSender>();

            //services.AddControllers();
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles(); // Posso usar os arquivos ´de páginas estáticas de confirmação de email

            app.UseRouting();

            app.UseIdentityServer();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
