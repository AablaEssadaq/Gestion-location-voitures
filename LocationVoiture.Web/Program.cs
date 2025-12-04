var builder = WebApplication.CreateBuilder(args);

// Ajouter les services MVC (Contrôleurs et Vues)
builder.Services.AddControllersWithViews();

// 1. CONFIGURATION DE LA SESSION (Pour rester connecté)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Déconnexion après 30min d'inactivité
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Permet d'accéder à la session depuis les pages HTML (Vues)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configuration des erreurs
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 2. ACTIVATION DE LA SESSION (Important : placer avant MapControllerRoute)
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();