using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    opciones.Filters.Add<FiltroErroresApi>();
    opciones.Filters.Add<FiltroCambioClave>();
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());      // protección CSRF en todos los formularios
});

// ---- Sesión del cliente web: cookie cifrada, HttpOnly, que contiene los tokens de la API
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Ingresar";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.Cookie.Name = "VillaSalud.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
    });

// Todas las páginas exigen sesión, salvo las marcadas con [AllowAnonymous]
builder.Services.AddAuthorization(opciones =>
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---- Cliente de la API REST
var urlApi = builder.Configuration["Api:UrlBase"];
if (string.IsNullOrWhiteSpace(urlApi))
    throw new InvalidOperationException("Falta la dirección de la API (Api:UrlBase) en la configuración.");
var baseApi = new Uri(urlApi.EndsWith('/') ? urlApi : urlApi + "/");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ServicioSesion>();
builder.Services.AddTransient<ManejadorToken>();
builder.Services.AddHttpClient(ServicioSesion.ClienteSinToken, cliente => cliente.BaseAddress = baseApi);
builder.Services.AddHttpClient<ApiCliente>(cliente =>
    {
        cliente.BaseAddress = baseApi;
        cliente.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ManejadorToken>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cabeceras de seguridad en todas las respuestas
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    contexto.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await siguiente();
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Inicio}/{action=Index}/{id?}");

app.Run();
