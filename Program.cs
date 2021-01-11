using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MijnuriAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //seeding data before starting app
            var host = CreateHostBuilder(args).Build();

            //set scope that this will be executed once on request
            using ( var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    //get datacontext
                    var context = services.GetRequiredService<DataContext>();

                    //add migrations
                    context.Database.Migrate();

                    //seed data to db
                    SeedData.Seed.SeedUsers(context);
                }
                //error checking
                catch(Exception ex)
                {
                    //log program class
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    
                    //log errors in ex
                    logger.LogError(ex, "Error During Migration !!!");
                }
            }
            //end of seeding

            //run server
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
