using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios básicos
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Permite crear controladores API para Android en el futuro

// 2. Autenticación por Cookies conectada a tu tabla MySQL
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Index"; // O "/Cuenta/IniciarSesion" según donde esté tu formulario
        options.AccessDeniedPath = "/Error";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// 3. Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Middlewares de seguridad
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.Run("http://0.0.0.0:5273");