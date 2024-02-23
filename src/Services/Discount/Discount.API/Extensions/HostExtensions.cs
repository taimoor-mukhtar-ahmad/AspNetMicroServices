using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discount.API.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0) { 
        int retryForAvailablility= retry.Value;
            using (var scope=host.Services.CreateScope()){
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();


                try
                {
                    logger.LogInformation("Migrating PostGreSQL DataBase");
                    using var connection = new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand { Connection = connection };
                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"Create Table Coupon(
                                          ID SERIAL PRIMARY KEY NOT NULL,
                                          ProductName VARCHAR(24) NOT NULL,
                                          Description TEXT,
                                      	  Amount INT); ";
                    command.ExecuteNonQuery();

                    //filling sample records
                    command.CommandText = "INSERT INTO Coupon (ProductName,Description,Amount) VALUES ('LIPTON','a tea brand',12)";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO Coupon (ProductName,Description,Amount) VALUES ('TAPAL','an older tea brand',102)";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migrated Postgresql Database Succesfully");

                }
                catch(NpgsqlException ex)
                {
                    logger.LogError(ex,"Error occured Migrating PostGreSql.");
                    if (retryForAvailablility < 50)
                    {
                        retryForAvailablility++;
                        System.Threading.Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host, retryForAvailablility);
                    }
                }

              
            }
            return host;
        }

    }
}
