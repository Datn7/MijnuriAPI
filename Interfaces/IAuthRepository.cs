using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Interfaces
{
    public interface IAuthRepository
    {
        //register user
        Task<User> Register(User user, string password);

        //login user
        Task<User> Login(string username, string password);

        //check if user exists
        Task<bool> UserExists(string username);
    }
}
