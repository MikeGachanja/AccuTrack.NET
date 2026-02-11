using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Security;

/// <summary>
/// Represents security configuration for a SCADA project.
/// </summary>
public class Security
{
    public List<User> Users { get; set; } = new List<User>();
    public List<Role> Roles { get; set; } = new List<Role>();
    public bool Enabled { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool RequirePasswordChange { get; set; } = false;
    public int PasswordMinLength { get; set; } = 8;

    /// <summary>
    /// Converts security to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject();
        var usersArray = new JArray();
        var rolesArray = new JArray();

        foreach (var user in Users)
        {
            usersArray.Add(user.ToJson());
        }

        foreach (var role in Roles)
        {
            rolesArray.Add(role.ToJson());
        }

        obj["users"] = usersArray;
        obj["roles"] = rolesArray;
        obj["enabled"] = Enabled;
        obj["sessionTimeoutMinutes"] = SessionTimeoutMinutes;
        obj["requirePasswordChange"] = RequirePasswordChange;
        obj["passwordMinLength"] = PasswordMinLength;

        return obj;
    }

    /// <summary>
    /// Creates security from JSON object.
    /// </summary>
    public static Security FromJson(JObject json)
    {
        var security = new Security
        {
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            SessionTimeoutMinutes = json["sessionTimeoutMinutes"]?.ToObject<int>() ?? 30,
            RequirePasswordChange = json["requirePasswordChange"]?.ToObject<bool>() ?? false,
            PasswordMinLength = json["passwordMinLength"]?.ToObject<int>() ?? 8
        };

        var usersArray = json["users"] as JArray;
        if (usersArray != null)
        {
            foreach (var item in usersArray)
            {
                if (item is JObject userObj)
                {
                    security.Users.Add(User.FromJson(userObj));
                }
            }
        }

        var rolesArray = json["roles"] as JArray;
        if (rolesArray != null)
        {
            foreach (var item in rolesArray)
            {
                if (item is JObject roleObj)
                {
                    security.Roles.Add(Role.FromJson(roleObj));
                }
            }
        }

        return security;
    }
}

/// <summary>
/// Represents a user in the security system.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Should be hashed in production
    public string RoleName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastLoginDate { get; set; } = DateTime.MinValue;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Converts user to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["username"] = Username,
            ["passwordHash"] = PasswordHash,
            ["roleName"] = RoleName,
            ["enabled"] = Enabled,
            ["createdDate"] = CreatedDate.ToString("O"),
            ["lastLoginDate"] = LastLoginDate.ToString("O"),
            ["email"] = Email,
            ["fullName"] = FullName
        };
    }

    /// <summary>
    /// Creates user from JSON object.
    /// </summary>
    public static User FromJson(JObject json)
    {
        var user = new User
        {
            Username = json["username"]?.ToString() ?? string.Empty,
            PasswordHash = json["passwordHash"]?.ToString() ?? string.Empty,
            RoleName = json["roleName"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            Email = json["email"]?.ToString() ?? string.Empty,
            FullName = json["fullName"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            user.Id = id;
        }

        if (DateTime.TryParse(json["createdDate"]?.ToString(), out DateTime createdDate))
        {
            user.CreatedDate = createdDate;
        }

        if (DateTime.TryParse(json["lastLoginDate"]?.ToString(), out DateTime lastLoginDate))
        {
            user.LastLoginDate = lastLoginDate;
        }

        return user;
    }
}

/// <summary>
/// Represents a role with permissions.
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new List<string>();

    /// <summary>
    /// Converts role to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var permissionsArray = new JArray();
        foreach (var permission in Permissions)
        {
            permissionsArray.Add(permission);
        }

        return new JObject
        {
            ["id"] = Id.ToString(),
            ["name"] = Name,
            ["description"] = Description,
            ["permissions"] = permissionsArray
        };
    }

    /// <summary>
    /// Creates role from JSON object.
    /// </summary>
    public static Role FromJson(JObject json)
    {
        var role = new Role
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            Description = json["description"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            role.Id = id;
        }

        var permissionsArray = json["permissions"] as JArray;
        if (permissionsArray != null)
        {
            foreach (var item in permissionsArray)
            {
                role.Permissions.Add(item.ToString());
            }
        }

        return role;
    }
}
