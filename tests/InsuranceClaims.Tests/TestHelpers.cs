using AutoMapper;
using InsuranceClaims.Service.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceClaims.Tests;

internal static class TestMapper
{
    public static MapperConfiguration CreateConfiguration() =>
        new(cfg => cfg.AddMaps(typeof(ClaimProfile).Assembly), NullLoggerFactory.Instance);

    public static IMapper Create() => CreateConfiguration().CreateMapper();
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
