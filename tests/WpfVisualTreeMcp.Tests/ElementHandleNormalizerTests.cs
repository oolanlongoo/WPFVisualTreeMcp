using FluentAssertions;
using WpfVisualTreeMcp.Server.Services;
using Xunit;

namespace WpfVisualTreeMcp.Tests;

public class ElementHandleNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForVisualTree_Blank_ReturnsNull(string? handle)
    {
        ElementHandleNormalizer.ForVisualTree(handle).Should().BeNull();
    }

    [Theory]
    [InlineData("window_0x100C4C")]
    [InlineData("window_0x1B1398")]
    [InlineData("WINDOW_0xABC")]
    [InlineData("window_")]
    public void ForVisualTree_WindowAlias_ReturnsNull(string handle)
    {
        ElementHandleNormalizer.ForVisualTree(handle).Should().BeNull();
    }

    [Theory]
    [InlineData("elem_00000009")]
    [InlineData("elem_0000004E")]
    public void ForVisualTree_ElementHandle_Preserved(string handle)
    {
        ElementHandleNormalizer.ForVisualTree(handle).Should().Be(handle);
    }
}
