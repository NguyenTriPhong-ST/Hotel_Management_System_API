namespace HotelManagementSystem.Services;

public enum RoomErrorKind { Invalid, NotFound, Conflict }

public sealed class RoomException : Exception
{
    public RoomErrorKind Kind { get; }

    public RoomException(RoomErrorKind kind, string message) : base(message)
        => Kind = kind;
}
