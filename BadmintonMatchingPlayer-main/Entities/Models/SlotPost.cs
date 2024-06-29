using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class SlotPost
    {

        [Key]
        public int IdSlot { get; set; }

        [Required]
        public int IdPost { get; set; }

        [Required]
        [StringLength(50)]
        public string ContextPost { get; set; }

        [Required]
        public DateTime SlotDate { get; set; }

        [Required]
        public decimal? SlotPrice { get; set; }

        [ForeignKey("IdPost")]
        public virtual Post Post { get; set; }

    }
}
