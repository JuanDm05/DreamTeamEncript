using HasheoProyecto.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Data;

namespace DreamTeamEncript
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración de la cadena de conexión (coloca tu cadena aquí)
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                                      ?? throw new InvalidOperationException("La variable de entorno CONNECTION_STRING no está configurada.");

            // Registrar la cadena de conexión
            builder.Services.AddSingleton(connectionString);
            builder.Services.AddScoped<IDbConnection>(sp => new Npgsql.NpgsqlConnection(sp.GetRequiredService<string>()));

            // Registrar Dapper con una fábrica de conexiones

            // Registrar clases de ayuda como CustomEncryptionStrings
            builder.Services.AddScoped<CustomEncryptionStrings>();

            // Agregar servicios del controlador con vistas
            builder.Services.AddControllersWithViews();

            // Configurar autenticación con cookies
            builder.Services.AddAuthentication("Cookies")
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(120); // Tiempo de expiración
                    options.SlidingExpiration = true;
                });

            // Agregar autorización
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Authenticated", policy =>
                    policy.RequireAuthenticatedUser()); // Solo permite acceso a usuarios autenticados
            });

            var app = builder.Build();

            // Configurar el pipeline de solicitud HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.Use(async (context, next) =>
            {
                // Aplica las cabeceras solo a las rutas protegidas
                if (context.User.Identity?.IsAuthenticated ?? false)
                {
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                }
                await next();
            });


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Usa la autenticación y autorización
            app.UseAuthentication();
            app.UseAuthorization();

            // Mapea la ruta predeterminada
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // Aquí puedes mapear cualquier ruta protegida con el atributo [Authorize]
            app.MapControllerRoute(
                name: "haseador",
                pattern: "Haseador/Index",
                defaults: new { controller = "Haseador", action = "Index" });

            app.Run();
        }
    }
}
