using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; }

        public DateTime ExpiredAt { get; set; }
    }
}
