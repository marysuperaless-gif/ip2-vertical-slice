using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public sealed class RevokedToken
{
    public string Jti { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
