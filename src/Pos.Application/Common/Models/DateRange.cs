namespace Pos.Application.Common.Models;

/// <summary>A half-open UTC range [FromUtc, ToUtc).</summary>
public readonly record struct DateRange(DateTime FromUtc, DateTime ToUtc);
