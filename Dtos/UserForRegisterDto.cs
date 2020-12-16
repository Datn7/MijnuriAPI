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

        [Required]
        public string Gender { get; set; }

        [Required]
        public string KnownAs { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Country { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }

        public UserForRegisterDto()
        {
            Created = DateTime.Now;
            LastActive = DateTime.Now;
        }

    }
}
