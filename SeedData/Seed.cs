using MijnuriAPI.Data;
using MijnuriAPI.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.SeedData
{
    public class Seed
    {
        public static void SeedUsers(DataContext context)
        {
            //check if there are any users
            if (!context.Users.Any())
            {
                //read text from file json
                var userData = System.IO.File.ReadAllText("SeedData/UserSeedData.json");

                //deserialize json to list of users
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                //for every user, password will be salted and hashed and will be set to "password"
                foreach(var user in users)
                {
                    byte[] passwordHash, passwordSalt;
                    CreatePasswordHash("password", out passwordHash, out passwordSalt);

                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;
                    user.Username = user.Username.ToLower();

                    //add user to db
                    context.Users.Add(user);
                }

                //save changes
                context.SaveChanges();
            }
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
