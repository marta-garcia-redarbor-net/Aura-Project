using System.Reflection;
using Aura.Application.Ports;

namespace Aura.UnitTests.Application.Ports;

public class IErrorStoreContractTests
{
    [Fact]
    public void IErrorStore_Interface_HasRecordAsyncMethod()
    {
        var methods = typeof(IErrorStore).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var recordAsync = methods.FirstOrDefault(m =>
            m.Name == "RecordAsync" &&
            m.ReturnType == typeof(Task) &&
            m.GetParameters().Length >= 1 &&
            m.GetParameters()[0].ParameterType == typeof(ErrorEntry));

        Assert.NotNull(recordAsync);
    }

    [Fact]
    public void IErrorStore_Interface_HasGetRecentAsyncMethod()
    {
        var methods = typeof(IErrorStore).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var getRecentAsync = methods.FirstOrDefault(m =>
            m.Name == "GetRecentAsync" &&
            m.ReturnType.IsGenericType &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) &&
            m.GetParameters().Length >= 1 &&
            m.GetParameters()[0].ParameterType == typeof(int));

        Assert.NotNull(getRecentAsync);
    }

    [Fact]
    public void ErrorEntry_Record_HasRequiredProperties()
    {
        var properties = typeof(ErrorEntry).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(properties, p => p.Name == "CorrelationId" && p.PropertyType == typeof(string));
        Assert.Contains(properties, p => p.Name == "Timestamp" && p.PropertyType == typeof(DateTimeOffset));
        Assert.Contains(properties, p => p.Name == "Message" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void ErrorEntry_Record_CanBeConstructed()
    {
        var entry = new ErrorEntry("corr-123", DateTimeOffset.UtcNow, "Test error");

        Assert.Equal("corr-123", entry.CorrelationId);
        Assert.NotNull(entry.Message);
    }
}
