using System;
using System.Collections.Generic;
using System.Text;

namespace InviteStudio.Application.Entities;

public class GuestTag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Guest> Guests { get; set; } = new List<Guest>();
}
