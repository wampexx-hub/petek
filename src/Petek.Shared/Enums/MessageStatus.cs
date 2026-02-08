namespace Petek.Shared.Enums;

/// <summary>
/// Mesaj durumu
/// </summary>
public enum MessageStatus
{
    /// <summary>Gönderildi</summary>
    Sent = 0,

    /// <summary>İletildi</summary>
    Delivered = 1,

    /// <summary>Okundu</summary>
    Read = 2,

    /// <summary>Hata</summary>
    Failed = 3
}
