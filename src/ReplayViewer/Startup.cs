using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace ReplayViewer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }
            Configuration = builder.Build();

            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Configuration["StorageConnectionString"]);            
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.SetServicePropertiesAsync(
                new ServiceProperties
                {
                    Cors = new CorsProperties()
                    {
                        CorsRules = {
                            new CorsRule()
                            {
                                AllowedOrigins = { "*" },
                                AllowedMethods =  CorsHttpMethods.Get | CorsHttpMethods.Options,
                                AllowedHeaders = { "*" },
                                ExposedHeaders = { "*" },
                                MaxAgeInSeconds = 31536000,
                            }
                        }
                    }
                });

            CloudBlobContainer container = blobClient.GetContainerReference("replays");
            container.CreateIfNotExistsAsync().Wait();
            container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob }).Wait();
            
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<CloudStorageAccount>(s => CloudStorageAccount.Parse(Configuration["StorageConnectionString"]));
            // Add framework services.
            services.AddMvc();

            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            DefaultFilesOptions options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
