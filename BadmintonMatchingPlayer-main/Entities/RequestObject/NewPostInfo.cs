using System.Globalization;
using Entities.Models;

namespace Entities.RequestObject
{
    public class NewPostInfo
    {
        public string? LevelSlot { get; set; }
        public string? CategorySlot { get; set; }
        public string? Title { get; set; }
        public string? Address { get; set; }
        public SlotInfo? SlotInfor { get; set; }
        public string? Description { get; set; }
        public string? HighlightUrl { get; set; }
        public List<string>? ImgUrls { get; set; }

    }

    public class SlotInfo
    {
        public class PostSlot
        {
            public string? DateSlot { get; set; }
            public List<SlotPost> TimeSlot { get; set; }=new List<SlotPost>();

        }
        public PostSlot? postSlot { get; set; }=new PostSlot();
        public List<String> TimeSlot { get; set; }
        public decimal? Price { get; set; }
    }
}
