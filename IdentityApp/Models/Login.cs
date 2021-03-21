using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityApp.Models
{
    public class Login
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [DataType(DataType.Password), Required(ErrorMessage = "Campo obrigatório"), MinLength(4, ErrorMessage = "Tamanho mínimo de 4 caracteres")]
        public string Password { get; set; }
    }
}
