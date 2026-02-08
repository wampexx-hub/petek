namespace Petek.Shared.Enums;

/// <summary>
/// Kullanıcı durumu (Presence)
/// </summary>
public enum UserStatus
{
    /// <summary>Çevrimdışı</summary>
    Offline = 0,

    /// <summary>Uygun</summary>
    Available = 1,

    /// <summary>Meşgul</summary>
    Busy = 2,

    /// <summary>Dışarıda</summary>
    Away = 3,

    /// <summary>Görünmez</summary>
    Invisible = 4
}
