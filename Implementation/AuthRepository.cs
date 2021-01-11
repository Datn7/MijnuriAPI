using Microsoft.EntityFrameworkCore;
using MijnuriAPI.Data;
using MijnuriAPI.Interfaces;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Implementation
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext context;

        public AuthRepository(DataContext context)
        {
            this.context = context;
        }

        public async Task<User> Login(string username, string password)
        {
            //get user with username
            var user = await context.Users.Include(p=>p.Photos).FirstOrDefaultAsync(x => x.Username == username);

            //if user does not exists return nothing
            if(user == null)
                return null;

            //verify password hash if it does not match return false
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            //hash password salt and compare each byte to eachother
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for(int i=0; i< computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            //if it matches return its true
            return true;
        }

        //method gets user class and string of password
        public async Task<User> Register(User user, string password)
        {
            //create variables of salt and hash 
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            //save hashed and salted values to user class
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            //add user to sql server db and save changes
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            //return user to controller of class user
            return user;

        }


        //create hash and salt for password
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            //use hmacsha512 algorithm
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }


        //check if user exists
        public async Task<bool> UserExists(string username)
        {
            //check in db if similar user exists with username
            if (await context.Users.AnyAsync(x => x.Username == username))
                return true;

            //if not, return false
            return false;
        }
    }
}
