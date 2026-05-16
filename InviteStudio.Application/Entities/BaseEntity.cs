using System;
using System.Collections.Generic;
using System.Text;

namespace InviteStudio.Application.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
