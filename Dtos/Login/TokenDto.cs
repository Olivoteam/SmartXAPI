using System;
namespace SmartxAPI.Dtos.Login
{
    public class TokenDto
    {
		public string Token { get; set; }
		public DateTime Expiry { get; set; }
    public string RefreshToken { get; set; }
    public int n_AppID { get; set; }
		
    }
}