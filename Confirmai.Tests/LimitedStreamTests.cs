using Confirmai.Services.Payment;
using System.Text;

namespace Confirmai.Tests;

public class LimitedStreamTests
{
    [Fact]
    public void Read_WithinLimit_ReadsAllBytes()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);
        var buffer = new byte[20];

        // Act
        var bytesRead = limitedStream.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(data.Length, bytesRead);
        Assert.Equal("Hello, World!", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public void Read_ExceedsLimit_ReadsOnlyUpToLimit()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 5);
        var buffer = new byte[20];

        // Act
        var bytesRead = limitedStream.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public async Task ReadAsync_WithinLimit_ReadsAllBytes()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);
        var buffer = new byte[20];

        // Act
        var bytesRead = await limitedStream.ReadAsync(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(data.Length, bytesRead);
        Assert.Equal("Hello, World!", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public async Task ReadAsync_ExceedsLimit_ReadsOnlyUpToLimit()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 5);
        var buffer = new byte[20];

        // Act
        var bytesRead = await limitedStream.ReadAsync(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public async Task ReadAsync_Memory_WithinLimit_ReadsAllBytes()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);
        var buffer = new byte[20];

        // Act
        var bytesRead = await limitedStream.ReadAsync(buffer);

        // Assert
        Assert.Equal(data.Length, bytesRead);
        Assert.Equal("Hello, World!", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public async Task ReadAsync_Memory_ExceedsLimit_ReadsOnlyUpToLimit()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 5);
        var buffer = new byte[20];

        // Act
        var bytesRead = await limitedStream.ReadAsync(buffer);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, bytesRead));
    }

    [Fact]
    public void CanRead_ReturnsTrue()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.True(limitedStream.CanRead);
    }

    [Fact]
    public void CanSeek_ReturnsFalse()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.False(limitedStream.CanSeek);
    }

    [Fact]
    public void CanWrite_ReturnsFalse()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.False(limitedStream.CanWrite);
    }

    [Fact]
    public void Position_ReturnsBytesRead()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello");
        var innerStream = new MemoryStream(data);
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);
        var buffer = new byte[10];
        var bytesRead = limitedStream.Read(buffer, 0, 3);

        // Act & Assert
        Assert.Equal(3, limitedStream.Position);
    }

    [Fact]
    public void Length_ThrowsNotSupportedException()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _ = limitedStream.Length);
    }

    [Fact]
    public void SetPosition_ThrowsNotSupportedException()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => limitedStream.Position = 10);
    }

    [Fact]
    public void Seek_ThrowsNotSupportedException()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => limitedStream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void SetLength_ThrowsNotSupportedException()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => limitedStream.SetLength(100));
    }

    [Fact]
    public void Write_ThrowsNotSupportedException()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var limitedStream = new LimitedStream(innerStream, maxBytes: 100);
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => limitedStream.Write(buffer, 0, buffer.Length));
    }
}
