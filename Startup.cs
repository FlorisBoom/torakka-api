using System;
using System.Text;
using MangaAlert.Repositories;
using MangaAlert.Scheduler;
using MangaAlert.Services;
using MangaAlert.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MangaAlert
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
      BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

      services.Configure<MongoDbSettings>(Configuration.GetSection(nameof(MongoDbSettings)));
      services.AddSingleton<IMongoDbSettings>(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

      services.AddSingleton<IUserRepository, UserRepository>();
      services.AddSingleton<IAlertRepository, AlertRepository>();
      services.AddSingleton<IPasswordHash, PasswordHash>();
      services.AddSingleton<IJwtManagerService, JwtManagerService>();

      services.Configure<JwtSettings>(Configuration.GetSection(nameof(JwtSettings)));
      services.AddSingleton<IJwtSettings>(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

      var jwtSettings = Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
      services.AddAuthentication(jwt =>
      {
        jwt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        jwt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(bearer =>
      {
        bearer.RequireHttpsMetadata = true;
        bearer.SaveToken = true;
        bearer.TokenValidationParameters = new TokenValidationParameters {
          ValidateIssuer = true,
          ValidIssuer = jwtSettings.Issuer,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
          ValidAudience = jwtSettings.Audience,
          ValidateAudience = true,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromMinutes(1)
        };
      });

      services.AddHostedService<JwtRefreshTokenCleanupJob>();
      services.AddHostedService<AlertScrapperJob>();

      services.AddControllers();
      services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "Alert", Version = "v1"}); });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alert v1"));
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
  }
}
