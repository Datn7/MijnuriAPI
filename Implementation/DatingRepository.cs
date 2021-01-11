using Microsoft.EntityFrameworkCore;
using MijnuriAPI.Data;
using MijnuriAPI.Helpers;
using MijnuriAPI.Interfaces;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Implementation
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext context;

        public DatingRepository(DataContext context)
        {
            this.context = context;
        }

        //add entity which is class
        public void Add<T>(T entity) where T : class
        {
            context.Add(entity);
        }

        //delete entity which is class
        public void Delete<T>(T entity) where T : class
        {
            context.Remove(entity);
        }

        //get user like
        public async Task<Like> GetLike(int userId, int recipientId)
        {
            //get like where we have userid and recipientid
            return await context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);

        }

        //get main photo for user
        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            //return photo where we have user and photo's ismain is true
            return await context.Photos.Where(u => u.User.Id == userId).FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            //get photo from db with id
            var photo = await context.Photos.FirstOrDefaultAsync(p => p.Id == id);
            return photo;
        }

        //get user with specified id
        public async Task<User> GetUser(int id)
        {
            //get user and include photos also which is seperate class but included property
            var user = await context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);

            //return user
            return user;
        }

        //get all users with user parameters
        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            //get users from db, include photos and order it by descending by last activity
            var users = context.Users.Include(p => p.Photos).OrderByDescending(u=>u.LastActive).AsQueryable();

            //return users NOT including urself. you may like yourself but its narcisism. anyway
            users = users.Where(u => u.Id != userParams.UserId);

            //get users where gender is specified in userparams, if u are girl u get boys and reverse. if u are gay u can list all btw.
            users = users.Where(u => u.Gender == userParams.Gender);

            //if user has likes get them
            if (userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            //if user likes others get them
            if (userParams.Likees)
            {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            //check if returned users are from 18 to 99 years
            if (userParams.MinAge !=18 || userParams.MaxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge -1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            //check in what order users should be arranged
            if(!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        //get user likes
        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            //get users and include likers and likees
            var user = await context.Users
                .Include(x => x.Likers)
                .Include(x => x.Likees)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (likers)
            {
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);
            }
            else
            {
                return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);
            }
        }

        //if we have changes, save them
        public async Task<bool> SaveAll()
        {
            return await context.SaveChangesAsync() > 0;
        }

        //get single message
        public async Task<Message> GetMessage(int id)
        {
            //get message with id
            return await context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        //get messages for user with message parameters
        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            //get messages and include photos for sender and recipient
            var messages = context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .AsQueryable();

            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                        && u.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where(u => u.SenderId == messageParams.UserId
                        && u.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                        && u.RecipientDeleted == false && u.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending(d => d.MessageSent);

            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => m.RecipientId == userId && m.RecipientDeleted == false
                    && m.SenderId == recipientId
                    || m.RecipientId == recipientId && m.SenderId == userId
                    && m.SenderDeleted == false)
                .OrderByDescending(m => m.MessageSent)
                .ToListAsync();

            return messages;
        }
    }
}
