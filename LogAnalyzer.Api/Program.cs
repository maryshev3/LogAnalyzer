using System.Net.Http.Headers;
using LogAnalyzer.Clients;
using Microsoft.AspNetCore.Http.Features;

namespace LogAnalyzer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 600 * 1024 * 1024;
        });
        
        builder.Services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = 600 * 1024 * 1024;
        });

        builder.Services.AddHttpClient<GigaChatHttpClient>(httpClient =>
            {
                httpClient
                    .DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}