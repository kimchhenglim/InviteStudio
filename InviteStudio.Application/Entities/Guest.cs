using System;
using System.Collections.Generic;
using System.Text;

namespace InviteStudio.Application.Entities;

public class Guest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid GuestTagId { get; set; }
    public GuestTag GuestTag { get; set; } = null!;
}
