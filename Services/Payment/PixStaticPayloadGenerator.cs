using System.Globalization;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services.Payment;

public class PixStaticPayloadGenerator
{
    public static string Build(string pixKey, string groupName, string? city, decimal amount)
    {
        static string F(string tag, string v) => $"{tag}{v.Length:D2}{v}";

        var name    = groupName.Length > 25 ? groupName[..25] : groupName;
        var cityStr = string.IsNullOrWhiteSpace(city) ? "Brasil" : (city.Length > 15 ? city[..15] : city);
        var amtStr  = amount.ToString("F2", CultureInfo.InvariantCulture);

        var mai  = F("0014", "br.gov.bcb.pix") + F("01", pixKey);
        var body = "000201"
            + F("26", mai)
            + "52040000"
            + "5303986"
            + F("54", amtStr)
            + "5802BR"
            + F("59", name)
            + F("60", cityStr)
            + "6304";

        ushort crc = 0xFFFF;
        foreach (char c in body)
        {
            crc ^= (ushort)(c << 8);
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return body + crc.ToString("X4");
    }

    public static string? GetGroupAdminPixKey(Group group)
    {
        if (!string.IsNullOrWhiteSpace(group.PixReceiverUserId))
        {
            var chosen = group.Members
                .FirstOrDefault(m => m.UserId == group.PixReceiverUserId);
            if (!string.IsNullOrWhiteSpace(chosen?.User?.PixKey))
                return chosen.User.PixKey;
        }

        return group.Members
            .Where(m => m.Role == GroupMemberRole.Admin && !string.IsNullOrWhiteSpace(m.User?.PixKey))
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.User!.PixKey)
            .FirstOrDefault();
    }
}
