using Owin;
using Microsoft.Owin;
using LibraryApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Hubs;
using LibraryApp.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Connections;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();


builder.Services.AddSingleton(new DBContext1(builder.Configuration.GetConnectionString("conexion")));

builder.Services.AddDbContext<Library7Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion") ??
    throw new InvalidOperationException("Connection string 'Library3Context' not found.")));

//*
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
{
    // al usar [Authenticate] si no esta logueado se redirige hacia esta ruta
    option.LoginPath = "/Account/Login";
    option.AccessDeniedPath = "/Home/Error";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
//*
app.UseAuthentication();
//*
app.UseAuthorization();

app.MapHub<SignalRHub>("/signalrhub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
