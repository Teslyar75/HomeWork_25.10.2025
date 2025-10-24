# 📚 Учебный отчет: Разработка интернет-магазина на ASP.NET Core MVC

## 🎯 Цель проекта
Разработка полнофункционального интернет-магазина с использованием ASP.NET Core MVC, Entity Framework Core, и современных веб-технологий.

---

## 🏗️ Архитектура проекта

### 📁 Структура проекта
```
ASP-421/
├── Controllers/           # Контроллеры MVC и API
├── Data/                 # Слой доступа к данным
├── Models/               # Модели представления
├── Views/                # Razor представления
├── Services/             # Бизнес-логика
├── wwwroot/             # Статические файлы
└── Migrations/          # Миграции БД
```

### 🔧 Технологический стек
- **Backend:** ASP.NET Core 9.0 MVC
- **Database:** SQLite с Entity Framework Core
- **Frontend:** Bootstrap 5, JavaScript ES6+
- **Authentication:** Cookie-based с Claims
- **Session Management:** ASP.NET Core Session

---

## 📊 База данных (Entity Framework Core)

### 🗃️ Модели данных

#### 1. **User** - Пользователи системы
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public DateTime RegisterDt { get; set; }
    public DateTime? DeleteDt { get; set; }
    
    // Навигационные свойства
    public ICollection<UserAccess> Accesses { get; set; } = new List<UserAccess>();
}
```

**Объяснение:**
- `Guid Id` - уникальный идентификатор пользователя
- `PasswordHash` - хешированный пароль для безопасности
- `DeleteDt` - мягкое удаление (nullable DateTime)
- `ICollection<UserAccess>` - связь один-ко-многим с доступом

#### 2. **ProductGroup** - Группы товаров
```csharp
public class ProductGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
    
    // Навигационные свойства
    public ProductGroup? Parent { get; set; }
    public ICollection<ProductGroup> Children { get; set; } = new List<ProductGroup>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

**Объяснение:**
- `ParentId` - для создания иерархии групп (подгруппы)
- `Slug` - URL-friendly идентификатор для SEO
- `DeletedAt` - мягкое удаление
- Самосвязь через `Parent` и `Children`

#### 3. **Product** - Товары
```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid GroupId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
    
    // Навигационные свойства
    public ProductGroup Group { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
```

#### 4. **CartItem** - Элементы корзины
```csharp
public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Навигационные свойства
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

### 🔗 Конфигурация связей в DataContext
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Связь User -> UserAccess (один-ко-многим)
    modelBuilder.Entity<UserAccess>()
        .HasOne(ua => ua.User)
        .WithMany(u => u.Accesses)
        .HasForeignKey(ua => ua.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Связь ProductGroup -> Product (один-ко-многим)
    modelBuilder.Entity<Product>()
        .HasOne(p => p.Group)
        .WithMany(g => g.Products)
        .HasForeignKey(p => p.GroupId)
        .OnDelete(DeleteBehavior.Cascade);

    // Самосвязь для ProductGroup (иерархия)
    modelBuilder.Entity<ProductGroup>()
        .HasOne(g => g.Parent)
        .WithMany(g => g.Children)
        .HasForeignKey(g => g.ParentId)
        .OnDelete(DeleteBehavior.Restrict);

    // Уникальный индекс для CartItem
    modelBuilder.Entity<CartItem>()
        .HasIndex(c => new { c.UserId, c.ProductId })
        .IsUnique();
}
```

**Объяснение конфигурации:**
- `OnDelete(DeleteBehavior.Cascade)` - каскадное удаление
- `OnDelete(DeleteBehavior.Restrict)` - запрет удаления при наличии связанных записей
- `HasIndex().IsUnique()` - уникальный составной индекс

---

## 🎮 Контроллеры (MVC Pattern)

### 📋 ShopController - Основной контроллер магазина

#### **Index()** - Главная страница магазина
```csharp
public IActionResult Index()
{
    var groups = _dataAccessor.ProductGroups();
    var viewedProducts = _viewedProductsService.GetViewedProducts(8);
    
    ViewData["ViewedProducts"] = viewedProducts;
    
    return View(groups);
}
```

**Объяснение:**
- Получает все группы товаров из БД
- Загружает просмотренные товары для отображения
- Передает данные в представление через `ViewData`

#### **Group(string slug)** - Страница группы товаров
```csharp
public IActionResult Group(string slug)
{
    var group = _dataAccessor.ProductGroups()
        .FirstOrDefault(g => g.Slug == slug);
    
    if (group == null) return NotFound();
    
    return View(group);
}
```

#### **Product(string slug)** - Страница товара
```csharp
public IActionResult Product(string slug)
{
    var product = _dataAccessor.ProductGroups()
        .SelectMany(g => g.Products)
        .FirstOrDefault(p => p.Slug == slug);
    
    if (product == null) return NotFound();
    
    // Добавляем товар в историю просмотров
    _viewedProductsService.AddViewedProduct(product.Id);
    
    var relatedProducts = product.Group.Products
        .Where(p => p.Id != product.Id)
        .Take(4);
    
    ViewData["RelatedProducts"] = relatedProducts;
    ViewData["ViewedProducts"] = _viewedProductsService.GetViewedProducts(8);
    
    return View(product);
}
```

**Объяснение:**
- `SelectMany()` - "разворачивает" коллекции товаров из всех групп
- `AddViewedProduct()` - добавляет товар в историю просмотров
- `Take(4)` - ограничивает количество связанных товаров

### 🛒 CartController - Управление корзиной

#### **Index()** - Страница корзины
```csharp
public IActionResult Index()
{
    if (!HttpContext.User.Identity?.IsAuthenticated ?? false)
        return RedirectToAction("SignUp", "User");
    
    var userId = GetCurrentUserId();
    var cartItems = _dataAccessor.GetUserCartItems(userId);
    var total = _dataAccessor.GetCartTotal(userId);
    
    // Получаем рекомендуемые товары
    var recommendedProducts = GetRecommendedProducts(8);
    
    ViewData["CartItems"] = cartItems;
    ViewData["Total"] = total;
    ViewData["RecommendedProducts"] = recommendedProducts;
    ViewData["ViewedProducts"] = _viewedProductsService.GetViewedProducts(8);
    
    return View();
}
```

**Объяснение:**
- Проверка аутентификации пользователя
- Получение элементов корзины и общей суммы
- Загрузка рекомендуемых и просмотренных товаров

### 🔐 UserController - Управление пользователями

#### **SignUp()** - Регистрация пользователя
```csharp
[HttpPost]
public IActionResult SignUp(UserSignupFormModel model)
{
    if (!ModelState.IsValid)
        return View(new UserSignupViewModel { FormModel = model });
    
    // Проверка уникальности логина
    if (_dataAccessor.IsLoginExists(model.Login))
    {
        ModelState.AddModelError("Login", "Логин уже существует");
        return View(new UserSignupViewModel { FormModel = model });
    }
    
    // Создание пользователя
    var user = new User
    {
        Id = Guid.NewGuid(),
        Login = model.Login,
        PasswordHash = _kdfService.GetDerivedKey(model.Password, _randomService.GetRandomSalt()),
        Email = model.Email,
        Name = model.Name,
        RegisterDt = DateTime.UtcNow
    };
    
    _dataAccessor.AddUser(user);
    
    return RedirectToAction("Login");
}
```

**Объяснение:**
- `ModelState.IsValid` - проверка валидации модели
- `IsLoginExists()` - проверка уникальности логина
- `GetDerivedKey()` - хеширование пароля с солью
- `Guid.NewGuid()` - генерация уникального ID

---

## 🌐 API Контроллеры (REST API)

### 🛒 CartController API - Операции с корзиной

#### **POST /api/cart/add** - Добавление товара в корзину
```csharp
[HttpPost("add")]
public IActionResult AddToCart([FromBody] AddToCartRequest request)
{
    if (!HttpContext.User.Identity?.IsAuthenticated ?? false)
        return Unauthorized(new { success = false, message = "Необходима авторизация" });
    
    if (request == null || request.ProductId == Guid.Empty || request.Quantity <= 0)
        return BadRequest(new { success = false, message = "Некорректные данные" });
    
    var userId = GetCurrentUserId();
    
    try
    {
        _dataAccessor.AddToCart(userId, request.ProductId, request.Quantity);
        return Ok(new { success = true, message = "Товар добавлен в корзину" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { success = false, message = ex.Message });
    }
}
```

**Объяснение:**
- `[FromBody]` - данные приходят в теле HTTP-запроса
- `Unauthorized()` - возврат HTTP 401 для неавторизованных
- `BadRequest()` - возврат HTTP 400 для некорректных данных
- `Ok()` - возврат HTTP 200 с данными

#### **PUT /api/cart/update** - Обновление количества товара
```csharp
[HttpPut("update")]
public IActionResult UpdateCartItem([FromBody] UpdateCartItemRequest request)
{
    if (!HttpContext.User.Identity?.IsAuthenticated ?? false)
        return Unauthorized(new { success = false, message = "Необходима авторизация" });
    
    var userId = GetCurrentUserId();
    
    try
    {
        _dataAccessor.UpdateCartItemQuantity(userId, request.ProductId, request.Quantity);
        return Ok(new { success = true, message = "Количество обновлено" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { success = false, message = ex.Message });
    }
}
```

### ✏️ AdminEditController API - Редактирование для админов

#### **PUT /api/adminedit/groups/{id}** - Обновление группы
```csharp
[HttpPut("groups/{id}")]
public IActionResult UpdateGroup(Guid id, [FromBody] EditGroupViewModel model)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        return BadRequest(new { success = false, message = "Помилки валідації", errors });
    }
    
    var group = _dataAccessor.GetGroupById(id);
    if (group == null)
        return NotFound(new { success = false, message = "Групу не знайдено" });
    
    // Проверка уникальности slug
    if (!_dataAccessor.IsGroupSlugUnique(model.Slug, id))
        return BadRequest(new { success = false, message = "Slug групи вже існує" });
    
    // Обновление данных
    group.Name = model.Name;
    group.Description = model.Description;
    group.ImageUrl = model.ImageUrl;
    group.ParentId = model.ParentId;
    group.Slug = model.Slug;
    
    _dataAccessor.UpdateGroup(group);
    
    return Ok(new { success = true, message = "Групу успішно оновлено" });
}
```

**Объяснение:**
- `ModelState.IsValid` - проверка валидации на сервере
- `IsGroupSlugUnique()` - проверка уникальности slug
- Обновление только измененных полей
- Возврат структурированного JSON-ответа

---

## 🎨 Представления (Razor Views)

### 📄 Views/Shop/Index.cshtml - Главная страница
```html
@model IEnumerable<ProductGroup>

<div class="container mt-4">
    <h1 class="text-center mb-5">Наш магазин</h1>
    
    <div class="row">
        @foreach (var group in Model)
        {
            <div class="col-md-4 mb-4">
                <div class="card h-100">
                    <img src="/uploads/@group.ImageUrl" class="card-img-top" alt="@group.Name">
                    <div class="card-body d-flex flex-column">
                        <h5 class="card-title">@group.Name</h5>
                        <p class="card-text flex-grow-1">@group.Description</p>
                        <a asp-controller="Shop" asp-action="Group" asp-route-slug="@group.Slug" 
                           class="btn btn-primary mt-auto">Перейти к группе</a>
                    </div>
                </div>
            </div>
        }
    </div>
    
    <!-- Просмотренные товары -->
    @if (ViewData["ViewedProducts"] is IEnumerable<Product> viewedProducts && viewedProducts.Any())
    {
        <div class="mt-5">
            <h3>Недавно просмотренные</h3>
            @await Html.PartialAsync("_ViewedProducts", new ViewedProductsViewModel 
            { 
                Products = viewedProducts, 
                Title = "Недавно просмотренные",
                ShowOnHomePage = true 
            })
        </div>
    }
</div>
```

**Объяснение Razor синтаксиса:**
- `@model` - объявление типа модели
- `@foreach` - цикл по коллекции
- `asp-controller`, `asp-action` - генерация URL для маршрутизации
- `@await Html.PartialAsync()` - включение частичного представления

### 🛒 Views/Cart/Index.cshtml - Страница корзины
```html
<div class="container mt-4">
    <h2>Корзина покупок</h2>
    
    @if (ViewData["CartItems"] is IEnumerable<CartItem> cartItems && cartItems.Any())
    {
        <div class="table-responsive">
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Товар</th>
                        <th>Цена</th>
                        <th>Количество</th>
                        <th>Сумма</th>
                        <th>Действия</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in cartItems)
                    {
                        <tr data-product-id="@item.ProductId">
                            <td>
                                <strong>@item.Product.Name</strong>
                                <br><small class="text-muted">@item.Product.Group.Name</small>
                            </td>
                            <td>@item.Product.Price.ToString("C")</td>
                            <td>
                                <div class="input-group" style="width: 120px;">
                                    <button class="btn btn-outline-secondary" type="button" 
                                            onclick="decreaseQuantity('@item.ProductId')">-</button>
                                    <input type="number" class="form-control text-center" 
                                           value="@item.Quantity" min="1" 
                                           onchange="updateQuantity('@item.ProductId', this.value)">
                                    <button class="btn btn-outline-secondary" type="button" 
                                            onclick="increaseQuantity('@item.ProductId')">+</button>
                                </div>
                            </td>
                            <td class="item-total">@((item.Quantity * item.Product.Price).ToString("C"))</td>
                            <td>
                                <button class="btn btn-danger btn-sm" 
                                        onclick="removeFromCart('@item.ProductId')">
                                    Удалить
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        
        <div class="row">
            <div class="col-md-6">
                <h4>Общая сумма: <span class="text-success">@ViewData["Total"]</span></h4>
            </div>
            <div class="col-md-6 text-end">
                <button class="btn btn-success btn-lg" onclick="proceedToCheckout()">
                    Оформить заказ
                </button>
            </div>
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <h4>Корзина пуста</h4>
            <p>Добавьте товары из каталога, чтобы оформить заказ.</p>
            <a asp-controller="Shop" asp-action="Index" class="btn btn-primary">
                Перейти к каталогу
            </a>
        </div>
    }
</div>
```

---

## 🧠 Сервисы (Business Logic)

### 👀 ViewedProductsService - Управление историей просмотров
```csharp
public class ViewedProductsService : IViewedProductsService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DataAccessor _dataAccessor;
    private const string SESSION_KEY = "ViewedProducts";

    public ViewedProductsService(IHttpContextAccessor httpContextAccessor, DataAccessor dataAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _dataAccessor = dataAccessor;
    }

    public void AddViewedProduct(Guid productId)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        var viewedProducts = GetViewedProductIds();
        
        // Удаляем дубликаты и добавляем в начало
        viewedProducts.Remove(productId);
        viewedProducts.Insert(0, productId);
        
        // Ограничиваем количество (последние 10)
        if (viewedProducts.Count > 10)
            viewedProducts.RemoveAt(viewedProducts.Count - 1);
        
        // Сохраняем в сессию
        var json = JsonConvert.SerializeObject(viewedProducts);
        session.SetString(SESSION_KEY, json);
    }

    public IEnumerable<Product> GetViewedProducts(int maxItems = 8)
    {
        var viewedProductIds = GetViewedProductIds().Take(maxItems);
        
        return _dataAccessor.ProductGroups()
            .SelectMany(g => g.Products)
            .Where(p => viewedProductIds.Contains(p.Id))
            .OrderBy(p => viewedProductIds.ToList().IndexOf(p.Id));
    }

    private List<Guid> GetViewedProductIds()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return new List<Guid>();

        var json = session.GetString(SESSION_KEY);
        if (string.IsNullOrEmpty(json)) return new List<Guid>();

        try
        {
            return JsonConvert.DeserializeObject<List<Guid>>(json) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }
}
```

**Объяснение сервиса:**
- `IHttpContextAccessor` - доступ к HTTP-контексту и сессии
- `JsonConvert.SerializeObject()` - сериализация в JSON
- `session.SetString()` - сохранение в сессии
- `OrderBy(IndexOf())` - сортировка по порядку просмотра

---

## 🎯 JavaScript (Client-side Logic)

### 🛒 Функции корзины
```javascript
// Добавление товара в корзину
async function addToCart(productId) {
    try {
        const response = await fetch('/api/cart/add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                ProductId: productId,
                Quantity: 1
            })
        });

        const data = await response.json();
        
        if (data.success) {
            showCartNotification('Товар добавлен в корзину!');
            updateCartCounter();
        } else {
            showAlert('Ошибка', data.message, 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Ошибка', 'Не удалось добавить товар в корзину', 'danger');
    }
}

// Обновление количества товара
async function updateQuantity(productId, quantity) {
    if (quantity < 1) {
        removeFromCart(productId);
        return;
    }

    try {
        const response = await fetch('/api/cart/update', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                ProductId: productId,
                Quantity: parseInt(quantity)
            })
        });

        const data = await response.json();
        
        if (data.success) {
            updateItemTotal(productId, quantity);
            updateCartTotal();
        } else {
            showAlert('Ошибка', data.message, 'danger');
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('Ошибка', 'Не удалось обновить количество', 'danger');
    }
}

// Обновление счетчика корзины
async function updateCartCounter() {
    try {
        const response = await fetch('/api/cart/count');
        const data = await response.json();
        
        if (data.success) {
            const counter = document.getElementById('cart-counter');
            if (counter) {
                counter.textContent = data.count;
                counter.style.display = data.count > 0 ? 'inline' : 'none';
            }
        }
    } catch (error) {
        console.error('Error updating cart counter:', error);
    }
}
```

**Объяснение JavaScript:**
- `async/await` - асинхронное программирование
- `fetch()` - современный API для HTTP-запросов
- `JSON.stringify()` - сериализация объекта в JSON
- `parseInt()` - преобразование строки в число
- Обработка ошибок с `try/catch`

---

## 🔐 Аутентификация и авторизация

### 🍪 Cookie-based Authentication
```csharp
// В Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";
        options.AccessDeniedPath = "/User/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
```

### 🔑 Claims-based Authorization
```csharp
// Создание Claims при входе
var claims = new List<Claim>
{
    new Claim("Id", user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Name),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, userRole.Name)
};

var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
var authProperties = new AuthenticationProperties
{
    IsPersistent = true,
    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
};

await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(claimsIdentity), authProperties);
```

**Объяснение:**
- `Claims` - информация о пользователе (ID, имя, роль)
- `IsPersistent = true` - "запомнить меня"
- `SlidingExpiration = true` - продление сессии при активности

---

## 📱 Адаптивный дизайн (Bootstrap)

### 🎨 CSS стили
```css
/* Убираем подчеркивание в описаниях групп и товаров */
.card-text {
    text-decoration: none !important;
}

/* Стили для кнопок корзины */
.btn-cart {
    transition: all 0.3s ease;
}

.btn-cart:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

/* Стили для страницы товара */
.product-image-container {
    position: relative;
    overflow: hidden;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.product-image-container img {
    transition: transform 0.3s ease;
    width: 100%;
    height: auto;
    min-height: 400px;
    object-fit: cover;
}

.product-image-container:hover img {
    transform: scale(1.05);
}

/* Адаптивность для мобильных устройств */
@media (max-width: 768px) {
    .product-image-container img {
        min-height: 250px;
    }
    
    .product-actions .btn {
        width: 100%;
        margin-bottom: 0.5rem;
    }
}
```

**Объяснение CSS:**
- `transition` - плавные анимации
- `transform: scale()` - масштабирование при наведении
- `object-fit: cover` - правильное масштабирование изображений
- `@media` - адаптивные стили для мобильных устройств

---

## 🚀 Развертывание и конфигурация

### ⚙️ Program.cs - Конфигурация приложения
```csharp
var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация сервисов
builder.Services.AddScoped<DataAccessor>();
builder.Services.AddScoped<IViewedProductsService, ViewedProductsService>();
builder.Services.AddHttpContextAccessor();

// Аутентификация
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

// Сессии
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Конфигурация pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

**Объяснение конфигурации:**
- `AddDbContext` - регистрация Entity Framework
- `AddScoped` - регистрация сервисов с областью действия запроса
- `UseSession()` - включение поддержки сессий
- `MapControllerRoute` - настройка маршрутизации MVC

---

## 📊 Миграции базы данных

### 🔄 Создание миграции
```bash
dotnet ef migrations add Initial
dotnet ef migrations add AddCartItems
dotnet ef database update
```

### 📝 Структура миграции
```csharp
public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Login = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                RegisterDt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");
    }
}
```

---

## 🎓 Ключевые концепции и паттерны

### 1. **MVC Pattern (Model-View-Controller)**
- **Model** - данные и бизнес-логика
- **View** - представление (HTML/CSS/JavaScript)
- **Controller** - обработка запросов и координация

### 2. **Repository Pattern**
- `DataAccessor` - слой доступа к данным
- Инкапсуляция логики работы с БД
- Разделение бизнес-логики и доступа к данным

### 3. **Dependency Injection**
- Внедрение зависимостей через конструктор
- Управление жизненным циклом объектов
- Тестируемость и гибкость кода

### 4. **REST API Design**
- Стандартные HTTP-методы (GET, POST, PUT, DELETE)
- Структурированные JSON-ответы
- Коды состояния HTTP

### 5. **Soft Delete Pattern**
- `DeletedAt` вместо физического удаления
- Возможность восстановления данных
- Сохранение истории изменений

---

## 🔧 Отладка и логирование

### 📝 Логирование в контроллерах
```csharp
public IActionResult AddToCart([FromBody] AddToCartRequest request)
{
    Console.WriteLine($"Cart API ADD - User claims: {string.Join(", ", HttpContext.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
    
    var userId = GetCurrentUserId();
    Console.WriteLine($"Cart API ADD - Extracted userId: {userId}");
    
    Console.WriteLine($"Cart API ADD - Request is null: {request == null}");
    if (request != null)
    {
        Console.WriteLine($"Cart API ADD - Request: ProductId={request.ProductId}, Quantity={request.Quantity}");
    }
    
    // ... остальная логика
}
```

### 🐛 Обработка ошибок
```csharp
try
{
    _dataAccessor.AddToCart(userId, request.ProductId, request.Quantity);
    return Ok(new { success = true, message = "Товар добавлен в корзину" });
}
catch (Exception ex)
{
    Console.WriteLine($"Error adding to cart: {ex.Message}");
    return BadRequest(new { success = false, message = "Ошибка добавления в корзину" });
}
```

---

## 📈 Производительность и оптимизация

### 🚀 Оптимизация запросов к БД
```csharp
// Использование Include для загрузки связанных данных
public IEnumerable<ProductGroup> ProductGroups()
{
    return _dataContext.ProductGroups
        .Where(g => g.DeletedAt == null)
        .Include(g => g.Products.Where(p => p.DeletedAt == null))
        .AsEnumerable();
}

// Кэширование в сессии
public IEnumerable<Product> GetViewedProducts(int maxItems = 8)
{
    var viewedProductIds = GetViewedProductIds().Take(maxItems);
    // Загружаем только нужные товары
    return _dataAccessor.ProductGroups()
        .SelectMany(g => g.Products)
        .Where(p => viewedProductIds.Contains(p.Id))
        .OrderBy(p => viewedProductIds.ToList().IndexOf(p.Id));
}
```

### 📱 Оптимизация фронтенда
```javascript
// Дебаунсинг для поиска
let searchTimeout;
function searchProducts(query) {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        performSearch(query);
    }, 300);
}

// Ленивая загрузка изображений
function lazyLoadImages() {
    const images = document.querySelectorAll('img[data-src]');
    const imageObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
                imageObserver.unobserve(img);
            }
        });
    });
    
    images.forEach(img => imageObserver.observe(img));
}
```

---

## 🧪 Тестирование

### ✅ Unit тесты (пример)
```csharp
[Test]
public void AddViewedProduct_ShouldAddProductToSession()
{
    // Arrange
    var httpContextAccessor = new Mock<IHttpContextAccessor>();
    var session = new Mock<ISession>();
    var dataAccessor = new Mock<DataAccessor>();
    
    httpContextAccessor.Setup(x => x.HttpContext.Session).Returns(session.Object);
    
    var service = new ViewedProductsService(httpContextAccessor.Object, dataAccessor.Object);
    var productId = Guid.NewGuid();
    
    // Act
    service.AddViewedProduct(productId);
    
    // Assert
    session.Verify(s => s.SetString(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

---

## 📚 Заключение

### 🎯 Что было изучено:

1. **ASP.NET Core MVC** - современный фреймворк для веб-разработки
2. **Entity Framework Core** - ORM для работы с базами данных
3. **Razor Views** - серверный рендеринг HTML
4. **REST API** - создание API для клиентских приложений
5. **Authentication & Authorization** - безопасность приложения
6. **Session Management** - управление состоянием пользователя
7. **Bootstrap** - адаптивный дизайн
8. **JavaScript ES6+** - современный клиентский код
9. **Dependency Injection** - управление зависимостями
10. **Database Migrations** - версионирование схемы БД

### 🚀 Навыки, которые развились:

- **Архитектурное мышление** - проектирование масштабируемых приложений
- **Работа с базами данных** - проектирование схем и оптимизация запросов
- **Frontend разработка** - создание интерактивных интерфейсов
- **API дизайн** - создание RESTful сервисов
- **Безопасность** - аутентификация, авторизация, валидация
- **Отладка** - поиск и исправление ошибок
- **Тестирование** - написание unit-тестов

### 📖 Рекомендации для дальнейшего изучения:

1. **Entity Framework Core** - более глубокое изучение
2. **ASP.NET Core Web API** - создание микросервисов
3. **SignalR** - real-time коммуникация
4. **Docker** - контейнеризация приложений
5. **Azure/AWS** - облачное развертывание
6. **Unit Testing** - покрытие кода тестами
7. **CI/CD** - автоматизация развертывания

---

## 📋 Список файлов проекта

### 🎮 Контроллеры
- `Controllers/HomeController.cs` - главная страница
- `Controllers/ShopController.cs` - каталог товаров
- `Controllers/UserController.cs` - управление пользователями
- `Controllers/CartController.cs` - корзина покупок
- `Controllers/Api/CartController.cs` - API корзины
- `Controllers/Api/AdminEditController.cs` - API редактирования

### 📊 Модели данных
- `Data/Entities/User.cs` - пользователи
- `Data/Entities/ProductGroup.cs` - группы товаров
- `Data/Entities/Product.cs` - товары
- `Data/Entities/CartItem.cs` - элементы корзины
- `Data/DataContext.cs` - контекст БД
- `Data/DataAccessor.cs` - слой доступа к данным

### 🎨 Представления
- `Views/Home/Index.cshtml` - главная страница
- `Views/Shop/Index.cshtml` - каталог
- `Views/Shop/Group.cshtml` - страница группы
- `Views/Shop/Product.cshtml` - страница товара
- `Views/Cart/Index.cshtml` - корзина
- `Views/Shop/Admin.cshtml` - админ панель
- `Views/Shop/AdminEdit.cshtml` - редактирование

### 🧠 Сервисы
- `Services/ViewedProductsService.cs` - история просмотров
- `Services/Kdf/PbKdf1Service.cs` - хеширование паролей
- `Services/Random/DefaultRandomService.cs` - генерация случайных значений

### 🎯 Модели представления
- `Models/Shop/ShopIndexViewModel.cs` - главная страница
- `Models/Shop/AdminEditModels.cs` - редактирование
- `Models/Shop/ViewedProductsViewModel.cs` - просмотренные товары
- `Models/User/UserSignupViewModel.cs` - регистрация

---

**Дата создания:** 2025-01-04  
**Автор:** AI Assistant  
**Версия:** 1.0  
**Статус:** Завершен ✅
