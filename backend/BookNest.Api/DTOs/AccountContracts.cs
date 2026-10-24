using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BookNest.Api.DTOs;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ProfileDto([Required, StringLength(120, MinimumLength = 2)] string FullName, [StringLength(40)] string? PhoneNumber, [StringLength(1000)] string? DefaultShippingAddress);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ChangePasswordDto([Required] string CurrentPassword, [Required, StringLength(128, MinimumLength = 8)] string NewPassword, [Required] string ConfirmPassword);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UserStatusDto([property: JsonRequired] bool IsActive);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UserRoleDto([Required, RegularExpression("^(Admin|Customer)$")] string Role);