using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class SocialService
    {
        private readonly AppDbContext _db;

        public SocialService(AppDbContext db)
        {
            _db = db;
        }

        public SocialPost CreatePost(SocialPost post)
        {
            // Tích hợp gi? l?p AI Ki?m duy?t N?i dung (Content Moderator)
            if (ContainsBadWords(post.Content))
            {
                post.Status = "Flagged"; // B? ch?n t? d?ng
            }
            else
            {
                post.Status = "Published";
            }

            post.CreatedAt = DateTime.Now;
            _db.SocialPosts.Add(post);
            _db.SaveChanges();
            return post;
        }

        public List<SocialPost> GetFeed()
        {
            return _db.SocialPosts
                .Where(p => p.Status == "Published")
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToList();
        }

        public SocialComment AddComment(int postId, string author, string content)
        {
            var comment = new SocialComment
            {
                PostId = postId,
                AuthorName = author,
                Content = content,
                CreatedAt = DateTime.Now
            };

            if (ContainsBadWords(content))
            {
                comment.Content = "*** [N?i dung dă b? ?n do vi ph?m tiêu chu?n c?ng d?ng] ***";
            }

            _db.SocialComments.Add(comment);
            _db.SaveChanges();
            return comment;
        }

        public List<SocialComment> GetCommentsForPost(int postId)
        {
            return _db.SocialComments
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ToList();
        }

        public bool LikePost(int postId)
        {
            var post = _db.SocialPosts.Find(postId);
            if (post != null)
            {
                post.Likes++;
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        private bool ContainsBadWords(string text)
        {
            var badWords = new[] { "ch?i", "dánh", "dm", "ngu" }; // Mock t? khóa x?u
            var textLower = text.ToLower();
            return badWords.Any(bw => textLower.Contains(bw));
        }
    }
}

