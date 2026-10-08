using SalesAR.Plugins.DataStore.SQL;
using SalesAR.UseCases.AgingReport;
using SalesAR.UseCases.Customers;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Payments;
using SalesAR.UseCases.PluginInterfaces;
using SalesAR.UseCases.Products;
using SalesAR.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================================
// DEPENDENCY INJECTION REGISTRATION (Clean Architecture Composition Root)
// ============================================================================

// 1. Cấu hình Connection Factory (Singleton)
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// 2. Cấu hình Repositories (Scoped theo chuẩn Blazor Server)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAgingReportRepository, AgingReportRepository>();

// 3. Cấu hình Use Cases (Scoped)
builder.Services.AddScoped<IViewCustomersUseCase, ViewCustomersUseCase>();
builder.Services.AddScoped<IViewProductsUseCase, ViewProductsUseCase>();
builder.Services.AddScoped<IViewInvoicesUseCase, ViewInvoicesUseCase>();
builder.Services.AddScoped<ICreateInvoiceUseCase, CreateInvoiceUseCase>();
builder.Services.AddScoped<IProcessPaymentUseCase, ProcessPaymentUseCase>();
builder.Services.AddScoped<IViewAgingReportUseCase, ViewAgingReportUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
