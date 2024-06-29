using Entities.Models;
using Entities.RequestObject;

namespace Entities.ResponseObject
{
    public class PostDetail
    {
        public PostDetail()
        {

        }
        public PostDetail(Post post)
        {
            AddressSlot = post.AddressSlot;
            CategorySlot = post.CategorySlot;
            ContentPost = post.ContentPost;
            FullName = post.IdUserToNavigation.FullName;
            HightLightImage = post.ImgUrl;
            LevelSlot = post.LevelSlot;
            ImgUrlUser = post.IdUserToNavigation.ImgUrl;
            SortProfile = post.IdUserToNavigation.SortProfile;
            TotalRate = post.IdUserToNavigation.TotalRate;
            UserId = post.IdUserTo.Value;
            Title = post.Title;
            postSlot=new List<PostSlot>();
            var DateSlot=new DateTime() ;

            List<DateTime> Dates = post.SlotsPost.Where(sp => sp.IdPost == post.Id).Select(sp => sp.SlotDate).Distinct().ToList();
            List<string> sl=new List<string>();
            foreach (var item in Dates) 
            {
                sl = post.SlotsPost.Where(sp=>sp.SlotDate== item).Select(sp => sp.ContextPost).ToList();
                PostSlot ps = new PostSlot();
                ps.DateSlot = item.ToString("dd/MM/yyyy");
                ps.slot = sl;
                postSlot.Add(ps);
            }
           
          
        }

        public string? AddressSlot { get; set; }
        public string? LevelSlot { get; set; }
        public string? CategorySlot { get; set; }
        public string? ContentPost { get; set; }
        public string? HightLightImage { get; set; }
        public List<string>? ImageUrls { get; set; }
        public string? FullName { get; set; }
        public int? TotalRate { get; set; }
        public string? ImgUrlUser { get; set; }
        public string? SortProfile { get; set; }
        public int UserId { get; set; }
        public string? Title { get; set; }
        public List<PostSlot> postSlot { get; set; }
        public class PostSlot  
        {
            public string? DateSlot { get; set; }
            public List<string> slot { get; set; } = new List<string>();

        }
    }
}
