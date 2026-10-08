using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using SalesAR.CoreBusiness.Security;
using SalesAR.Plugins.DataStore.SQL;
using SalesAR.UseCases.AgingReport;
using SalesAR.UseCases.Auth;
using SalesAR.UseCases.Customers;
using SalesAR.UseCases.Invoices;
using SalesAR.UseCases.Payments;
using SalesAR.UseCases.PluginInterfaces;
using SalesAR.UseCases.Products;
using SalesAR.Web.Auth;
using SalesAR.Web.Components;
using SalesAR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================================
// AUTHENTICATION & AUTHORIZATION SERVICES (K4.1 & K4.2)
// ============================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>(sp => 
    (CustomAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

// Security: Password Hasher using PBKDF2 with SHA256 (Salted, No plain text)
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// ============================================================================
// DEPENDENCY INJECTION REGISTRATION (Clean Architecture Composition Root)
// ============================================================================

// 1. Connection Factory (Singleton)
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// 2. Repositories (Scoped theo chuẩn Blazor Server)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAgingReportRepository, AgingReportRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// 3. Customer Use Cases (Scoped)
builder.Services.AddScoped<IViewCustomersUseCase, ViewCustomersUseCase>();
builder.Services.AddScoped<IViewCustomerByIdUseCase, ViewCustomerByIdUseCase>();
builder.Services.AddScoped<IAddCustomerUseCase, AddCustomerUseCase>();
builder.Services.AddScoped<IEditCustomerUseCase, EditCustomerUseCase>();
builder.Services.AddScoped<IDeleteCustomerUseCase, DeleteCustomerUseCase>();
builder.Services.AddScoped<IGetCustomerDebtUseCase, GetCustomerDebtUseCase>();

// 4. Product Use Cases (Scoped)
builder.Services.AddScoped<IViewProductsUseCase, ViewProductsUseCase>();
builder.Services.AddScoped<IViewProductByIdUseCase, ViewProductByIdUseCase>();
builder.Services.AddScoped<IAddProductUseCase, AddProductUseCase>();
builder.Services.AddScoped<IEditProductUseCase, EditProductUseCase>();
builder.Services.AddScoped<IDeleteProductUseCase, DeleteProductUseCase>();

// 5. Invoice Use Cases (Scoped)
builder.Services.AddScoped<IViewInvoicesUseCase, ViewInvoicesUseCase>();
builder.Services.AddScoped<ICreateInvoiceUseCase, CreateInvoiceUseCase>();

// 6. Payment Use Cases (Scoped)
builder.Services.AddScoped<IAllocatePaymentUseCase, AllocatePaymentUseCase>();
builder.Services.AddScoped<IRecordPaymentUseCase, RecordPaymentUseCase>();
builder.Services.AddScoped<IProcessPaymentUseCase, ProcessPaymentUseCase>();
builder.Services.AddScoped<IViewPaymentsUseCase, ViewPaymentsUseCase>();

// 7. Aging Report Use Cases & Exporters (Scoped)
builder.Services.AddScoped<IGetAgingReportUseCase, GetAgingReportUseCase>();
builder.Services.AddScoped<IViewAgingReportUseCase, ViewAgingReportUseCase>();
builder.Services.AddScoped<IAgingReportExporter, AgingReportExporter>();

// 8. AI Debt Reminder Assistant (Scoped - K4.3)
builder.Services.AddScoped<SalesAR.UseCases.AI.IDebtReminderAiService, DebtReminderAiService>();

// 8. Auth & User Management Use Cases (Scoped)
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();
builder.Services.AddScoped<IViewUsersUseCase, ViewUsersUseCase>();
builder.Services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
builder.Services.AddScoped<IToggleUserStatusUseCase, ToggleUserStatusUseCase>();
builder.Services.AddScoped<IViewRolesUseCase, ViewRolesUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ============================================================================
// MINIMAL API ENDPOINTS: EXPORT AGING REPORT (EXCEL & PDF) (K4.3)
// ============================================================================
app.MapGet("/api/reports/aging/excel", async (IGetAgingReportUseCase agingUseCase, IAgingReportExporter exporter, DateTime? asOfDate) =>
{
    var date = asOfDate ?? DateTime.Today;
    var summary = await agingUseCase.ExecuteAsync(date);
    var bytes = exporter.ExportToExcel(summary, date);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AgingReport_{date:yyyyMMdd}.xlsx");
});

app.MapGet("/api/reports/aging/pdf", async (IGetAgingReportUseCase agingUseCase, IAgingReportExporter exporter, DateTime? asOfDate) =>
{
    var date = asOfDate ?? DateTime.Today;
    var summary = await agingUseCase.ExecuteAsync(date);
    var bytes = exporter.ExportToPdf(summary, date);
    return Results.File(bytes, "application/pdf", $"AgingReport_{date:yyyyMMdd}.pdf");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
