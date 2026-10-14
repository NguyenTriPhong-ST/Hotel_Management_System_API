namespace HotelManagementSystem.Entities.Enums;

public enum AccountRole : byte { Admin = 1, Receptionist = 2, StaffRoom = 3 }

public enum RoomOperationalStatus : byte { Active = 1, OutOfService = 2 }

public enum HousekeepingStatus : byte { Dirty = 1, Cleaning = 2, Clean = 3, Inspected = 4 }

public enum DocumentType : byte { NationalId = 1, Passport = 2 }

public enum ReservationStatus : byte { Confirmed = 1, CheckedIn = 2, Completed = 3, Cancelled = 4, NoShow = 5 }

public enum InvoiceStatus : byte { Draft = 1, Finalized = 2 }

public enum InvoiceLineType : byte { RoomCharge = 1, ServiceCharge = 2 }

public enum PaymentMethod : byte { Cash = 1, BankTransfer = 2, Card = 3 }

public enum HotelTaskType : byte { Housekeeping = 1, Maintenance = 2 }

public enum HousekeepingKind : byte { Cleaning = 1, Inspection = 2 }

public enum HotelTaskStatus : byte { Pending = 1, Assigned = 2, InProgress = 3, Completed = 4, Cancelled = 5 }

public enum TaskPriority : byte { Low = 1, Normal = 2, High = 3, Urgent = 4 }