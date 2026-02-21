using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Models
{
    public class AdminSettings
    {
        public int Id { get; set; }

        [Required]
        public string SettingKey { get; set; } = string.Empty;

        public string SettingValue { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
