using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using MaintenanceChronicle.Data;
using MaintenanceChronicle.Api.Utils;
using MaintenanceChronicle.Application;
using MaintenanceChronicle.Application.Validators;
using MaintenanceChronicle.BackgroundServices.BackgroundWorkers;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Data.Entities.Business;
using MaintenanceChronicle.Utilities.Error;
using MaintenanceChronicle.Utilities.Helpers;
using Microsoft.OpenApi.Models;
using MaintenanceChronicle.Utilities.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MaintenanceChronicle.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.ConfigureKestrel(serverOptions => {
//    serverOptions.ListenAnyIP(7290); // Change the port number here
//});
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://*:80");
}

builder.Services.InstallServices(
    builder.Configuration,
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

//app.UseHttpsRedirection();

app.MapControllers();

//Testing purposes
app.MapGet("/", () => "This page wooooooorks");

//Apply the newest migrations to db
await app.ApplyMigrations();

app.Run();
