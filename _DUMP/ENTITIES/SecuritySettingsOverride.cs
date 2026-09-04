using System;
using University.Shared.Common;

namespace University.Core.Entities;

public class SecuritySettingsOverride : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? ValueJson { get; set; }
}
