using BadmintonMatching.RealtimeHub;
using Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Repositories.Implements;
using Repositories.Intefaces;
using Repository.Services;
using Services.Implements;
using Services.Interfaces;
using System.Reflection;
using BadmintonMatching.Payment;
using CorePush.Google;
using CorePush.Apple;
using Entities.ResponseObject;
using BadmintonMatching.Extension;

var builder = WebApplication.CreateBuilder(args);

var conString = builder.Configuration.GetConnectionString("sqlConnection");
builder.Services.AddDbContext<DataContext>(opts => opts.UseSqlServer(conString));

var acceptAllCors = "AcceptAllCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: acceptAllCors,
                      policy =>
                      {
                          policy.AllowAnyHeader();
                          policy.AllowAnyMethod();
                          policy.AllowAnyOrigin();
                      });
});

builder.Services.AddSignalR();

builder.Services.AddControllers(config =>
{
    config.RespectBrowserAcceptHeader = true;
    config.ReturnHttpNotAcceptable = true;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opt =>
{
    var fileName = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
    var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
    opt.IncludeXmlComments(filePath);
});

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<UserServices>();

builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
builder.Services.AddScoped<IHasingServices, HasingServices>();
builder.Services.AddScoped<IJwtSupport, JwtServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IPostServices, PostServices>();
builder.Services.AddScoped<IReportServices, ReportServices>();
builder.Services.AddScoped<ISlotServices, Services.Implements.SlotServices>();
builder.Services.AddScoped<Services.Interfaces.SlotServices, WalletServices>();
builder.Services.AddScoped<ITransactionServices, TransactionServices>();
builder.Services.AddScoped<IChatServices, ChatService>();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<INotificationServices, NotificationServices>();
builder.Services.AddScoped<ISettingServices, SettingService>();

builder.Services.AddHttpClient<FcmSender>();
builder.Services.AddHttpClient<ApnSender>();
builder.Services.Configure<FcmNotificationSetting>(builder.Configuration.GetSection("FcmNotification"));

builder.Services.Configure<VnPayOption>(builder.Configuration.GetSection("PaymentConfig:VnPay"));


var app = builder.Build();
//var port = Environment.GetEnvironmentVariable("PORT");
//app.Urls.Add($"http://*:{port}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();


    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; 
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors(acceptAllCors);

app.UseAuthorization();

app.MapRazorPages();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapHub<ChatHub>("/chatHub");

app.Run();
