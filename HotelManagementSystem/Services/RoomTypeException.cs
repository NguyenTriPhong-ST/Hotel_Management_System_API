namespace HotelManagementSystem.Services;

public enum RoomTypeErrorKind { Invalid, NotFound, Conflict }

public sealed class RoomTypeException : Exception
{
    public RoomTypeErrorKind Kind { get; }

    public RoomTypeException(RoomTypeErrorKind kind, string message) : base(message)
        => Kind = kind;
}
