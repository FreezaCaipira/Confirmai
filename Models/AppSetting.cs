using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

public class AppSetting
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Value { get; set; } = string.Empty;
}


