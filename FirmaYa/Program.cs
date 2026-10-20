using System.Text.Encodings.Web;
using System.Text.Unicode;
using FirmaYa.BLL;
using FirmaYa.Data;
using FirmaYa.Data.Memory;
using FirmaYa.Models.Entidades;
using FirmaYa.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.WebEncoders;
using Npgsql;

const string PoliticaPanelContratos = "PanelContratos";
const string PoliticaAdministrador = "Administrador";

var builder = WebApplication.CreateBuilder(args);

// Textos en español: Razor no convierte acentos ni eñes en entidades (&#xF3;). La codificación HTML se mantiene.
builder.Services.Configure<WebEncoderOptions>(opciones =>
    opciones.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddRazorPages(opciones =>
{
    opciones.Conventions.AuthorizeFolder("/");
    opciones.Conventions.AllowAnonymousToPage("/Index");
    opciones.Conventions.AllowAnonymousToPage("/Error");
    opciones.Conventions.AllowAnonymousToFolder("/Login");

    opciones.Conventions.AuthorizeFolder("/Dashboard", PoliticaPanelContratos);
    opciones.Conventions.AuthorizeFolder("/Contratos", PoliticaPanelContratos);
    opciones.Conventions.AuthorizeFolder("/Invitaciones", PoliticaPanelContratos);
    opciones.Conventions.AuthorizeFolder("/Firmas", PoliticaPanelContratos);
    opciones.Conventions.AuthorizeFolder("/Administrador", PoliticaAdministrador);

    opciones.Conventions.AllowAnonymousToPage("/Invitaciones/VerContratoForm");
    opciones.Conventions.AllowAnonymousToPage("/Firmas/FirmarContratoForm");
    opciones.Conventions.AllowAnonymousToPage("/Contratos/DescargarPDFForm");
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Login";
        opciones.LogoutPath = "/Login/Salir";
        opciones.AccessDeniedPath = "/";
        opciones.Cookie.Name = "FirmaYa.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        // La expiración la fija AccesoBLL.GenerarSesion en cada inicio de sesión.
        opciones.SlidingExpiration = false;
    });

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy(PoliticaPanelContratos, p => p.RequireRole(Roles.Abogado, Roles.AgenteInmobiliario));
    opciones.AddPolicy(PoliticaAdministrador, p => p.RequireRole(Roles.Administrador));
});

builder.Services.Configure<AplicacionOptions>(builder.Configuration.GetSection("Aplicacion"));
builder.Services.Configure<CorreoOptions>(builder.Configuration.GetSection("Correo"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<HoraLocal>();
builder.Services.AddSingleton<CorreoService>();
builder.Services.AddScoped<Enlaces>();

builder.Services.Configure<AccesoOptions>(builder.Configuration.GetSection("Acceso"));
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AccesoBLL>();
builder.Services.AddScoped<PlantillaBLL>();
builder.Services.AddScoped<ContratoBLL>();
builder.Services.AddScoped<NotificacionBLL>();
builder.Services.AddScoped<PDFBLL>();
builder.Services.AddScoped<DashboardBLL>();

// Supabase o memoria según Persistence:Mode; no hay cambio automático entre modos.
var modoPersistencia = builder.Configuration["Persistence:Mode"];
if (string.Equals(modoPersistencia, "Supabase", StringComparison.OrdinalIgnoreCase))
{
    var cadena = builder.Configuration.GetConnectionString("Supabase");
    if (string.IsNullOrWhiteSpace(cadena))
        throw new InvalidOperationException(
            "Falta ConnectionStrings:Supabase. Configurala con 'dotnet user-secrets' o la variable de entorno ConnectionStrings__Supabase.");

    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(cadena));
    builder.Services.AddSingleton<IDAL, DAL>();
}
else if (string.Equals(modoPersistencia, "Memory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IDAL, DALMemoria>();
}
else
{
    throw new InvalidOperationException($"Persistence:Mode '{modoPersistencia}' no es válido. Usar 'Supabase' o 'Memory'.");
}

var app = builder.Build();

app.Logger.LogInformation("Persistencia: {Modo}", modoPersistencia);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Error", "?codigo={0}");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Un usuario sin permisos sobre un contrato recibe 403, sin revelar si el contrato existe.
app.Use(async (contexto, siguiente) =>
{
    try
    {
        await siguiente(contexto);
    }
    catch (AccesoDenegadoException) when (!contexto.Response.HasStarted)
    {
        contexto.Response.Clear();
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
});

app.MapRazorPages();

app.Run();
