using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Dtos
{
    public class UserForRegisterDto
    {
        [Required(ErrorMessage ="მომხმარებელი აუცილებელია")]
        public string Username { get; set; }

        [Required(ErrorMessage ="პაროლი აუცილებელია")]
        [StringLength(8, MinimumLength = 4, ErrorMessage ="შეიყვანეთ 4 დან 8 ასომდე")]
        public string Password { get; set; }

    }
}
