using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Todo.UI.Pages.Account;

public class LoginModel : PageModel
{
    public string ApiBase { get; private set; }

    public LoginModel(IConfiguration configuration)
    {
        ApiBase = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/api";
    }

    public void OnGet() { }
}
