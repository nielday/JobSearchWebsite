namespace JobSearchWebsite.Models
{
    public class CareerGuide
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now; // Gán mặc định là thời gian hiện tại

        public string AuthorId { get; set; }

        public ApplicationUser Author { get; set; }

        public string ImageUrl { get; set; }
    }
}