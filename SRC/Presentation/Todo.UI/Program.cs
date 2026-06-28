var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

// M5: Security headers applied to every response
var apiBase = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/api";
var apiOrigin = new Uri(apiBase).GetLeftPart(UriPartial.Authority);

app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers.Append("Content-Security-Policy",
        $"default-src 'self'; " +
        $"script-src 'self' 'unsafe-inline' cdn.jsdelivr.net; " +
        $"style-src 'self' 'unsafe-inline' cdn.jsdelivr.net; " +
        $"font-src cdn.jsdelivr.net data:; " +
        $"img-src 'self' data:; " +
        $"connect-src 'self' {apiOrigin}; " +
        $"object-src 'none'; " +
        $"base-uri 'self'; " +
        $"frame-ancestors 'none'");
    headers.Append("X-Frame-Options", "DENY");
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
