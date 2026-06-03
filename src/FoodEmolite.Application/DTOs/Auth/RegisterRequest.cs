using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }
    }
}
