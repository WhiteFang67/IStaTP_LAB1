using System;

namespace OnlineStoreDomain.Model
{
    public partial class Review : Entity
    {
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateOnly Date { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}