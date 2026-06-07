using NovoSei.Web.Configuration;
using NovoSei.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNovoSeiAuthentication(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHostedService<NovoSei.Web.Services.IngestaoBackgroundWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints();
app.MapDocumentoEndpoints();
app.MapIngestaoEndpoints();
app.MapHub<NovoSei.Web.Hubs.NotificationHub>("/notificationHub");
app.MapRazorComponents<NovoSei.Web.Components.App>().AddInteractiveServerRenderMode();

app.Run();
