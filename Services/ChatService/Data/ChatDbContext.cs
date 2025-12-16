using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Data
{
    public class ChatDbContext: BaseDbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
        }
}
