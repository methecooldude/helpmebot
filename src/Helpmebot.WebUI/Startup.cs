using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Helpmebot.WebUI
{
    using Helpmebot.WebApi.Services.Interfaces;
    using Helpmebot.WebUI.Models;
    using Helpmebot.WebUI.Services;
    using Helpmebot.WebUI.Services.Api;
    using Microsoft.AspNetCore.Identity;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var siteConfig = this.Configuration.GetSection("SiteConfig").Get<SiteConfiguration>();
            services.AddSingleton(siteConfig);
            
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddSingleton<IUserStore<User>, UserStore>();
            services.AddSingleton<IRoleStore<Role>, RoleStore>();

            services.AddControllersWithViews();

            if (siteConfig.ApiPath.StartsWith("fake://"))
            {
                services.AddSingleton<IApiService, FakeApiService>();
            }
            else
            {
                services.AddSingleton<IApiService, ApiFrontendService>();
            }

            services.AddSingleton<IApiFrontendTransportService, ApiFrontendTransportService>();

            services.AddIdentity<User,Role>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(
                options =>
                {
                    options.LogoutPath = "/logout";
                    options.LoginPath = "/login";
                });

            // services.AddControllersWithViews(
            //     options =>
            //     {
            //         var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            //         options.Filters.Add(new AuthorizeFilter(policy));
            //     });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                });
        }
    }
}