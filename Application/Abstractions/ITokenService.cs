 using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions;

using Domain.Entities;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    DateTime GetAccessTokenExpiry();
}