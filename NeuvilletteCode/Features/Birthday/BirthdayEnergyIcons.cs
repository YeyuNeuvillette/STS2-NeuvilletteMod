using Neuvillette.Extensions;

namespace Neuvillette.Features.Birthday;

internal static class BirthdayEnergyIcons
{
    private const int BirthdayMonth = 8;
    private const int BirthdayDay = 5;

    public static bool IsActive => IsBirthday(DateTime.Now);

    public static string BigIconPath(string defaultFileName) =>
        (IsActive ? "charui/energy_neuvillette_big_birthday.png" : $"charui/{defaultFileName}").ImagePath();

    public static string TextIconPath(string defaultFileName) =>
        (IsActive ? "charui/energy_neuvillette_birthday.png" : $"charui/{defaultFileName}").ImagePath();

    public static string CounterTexturePath =>
        "neuvillette_energy_counter_birthday.png".CharacterImgPath("Neuvillette");

    internal static bool IsBirthday(DateTime localTime) =>
        localTime.Month == BirthdayMonth && localTime.Day == BirthdayDay;
}
