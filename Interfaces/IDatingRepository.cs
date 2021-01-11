using MijnuriAPI.Helpers;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Interfaces
{
    public interface IDatingRepository
    {
        //add entity
        void Add<T>(T entity) where T : class;

        //delete entity
        void Delete<T>(T entity) where T : class;
        
        //see if changes saved
        Task<bool> SaveAll();

        //get all users with userparams and return pagelist 
        Task<PagedList<User>> GetUsers(UserParams userParams);

        //get single user
        Task<User> GetUser(int id);

        //get single photo
        Task<Photo> GetPhoto(int id);

        //get main photo of user
        Task<Photo> GetMainPhotoForUser(int userId);

        //get likes
        Task<Like> GetLike(int userId, int recipientId);

        //get message
        Task<Message> GetMessage(int id);

        //get message for user
        Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams);

        //get message and include user photo and arrange sent messages
        Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId);
    }
}
