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

// 1. Connection Factory (Singleton)
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// 2. Repositories (Scoped theo chuẩn Blazor Server)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAgingReportRepository, AgingReportRepository>();

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

// 7. Aging Report Use Cases (Scoped)
builder.Services.AddScoped<IGetAgingReportUseCase, GetAgingReportUseCase>();
builder.Services.AddScoped<IViewAgingReportUseCase, ViewAgingReportUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
