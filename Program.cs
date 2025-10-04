using ASP_421.Data;
using ASP_421.Middleware;
using ASP_421.Services.Kdf;
using ASP_421.Services.Random;
using Microsoft.EntityFrameworkCore;
using ASP_421.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IRandomService, DefaultRandomService>();
builder.Services.AddSingleton<IKdfService, PbKdf1Service>();
builder.Services.AddSingleton<IStorageService, DiskStorageService>();

builder.Services.AddDbContext<DataContext>(options => 
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DataContext"))
);
builder.Services.AddScoped<DataAccessor>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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
app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapStaticAssets();

app.UseAuthSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

/* Д.З. Реалізувати валідацію моделі форми товару (адмінка),
 * що передається на додавання до БД. 
 * За відсутності помилок очищувати форму від введених даних.
 * 
 * Додати до форми створення нової групи поле з введенням
 * батьківської групи (для створення підгруп), доповнити валідацію
 * моделі групи.
 * 
 * ** За зразком сайту Amazon додати до карточки групи (на домашній
 * сторінці) відомості про підгрупи (або виводити текстом їх кількість
 * або формувати зображення з зображень перших підгруп)
 */
