using System.Diagnostics;
using System.Text;

namespace FMemory.Tests;

public class ReadStringTests
{
  [Fact]
  public void ReadStringTestDefaultEncoding()
  {
    using var process = Process.GetCurrentProcess();
    using var mm = new MemoryManager(process.Id);

    var str = "ÑPÑÇÑyÑrÑuÑÑ, ÑÅÑÇÑyÑrÑuÑÑ! x654";
    var strBytes = Encoding.UTF8.GetBytes(str);
    using var allDisposable = mm.AllocateMemory(100, out var allocation);
    mm.WriteBytes(allocation, strBytes);

    var readValue = mm.ReadString(allocation, 100);

    Assert.Equal(str, readValue);
  }

  [Fact]
  public void ReadStringTestASCII()
  {
    using var process = Process.GetCurrentProcess();
    using var mm = new MemoryManager(process.Id);

    var str = "x654 hi hi!";
    var strBytes = Encoding.ASCII.GetBytes(str);
    using var allDisposable = mm.AllocateMemory(100, out var allocation);
    mm.WriteBytes(allocation, strBytes);

    var readValue = mm.ReadString(allocation, 100, Encoding.ASCII);

    Assert.Equal(str, readValue);
  }

  [Fact]
  public void ReadStringTestUnsufficientSize()
  {
    using var process = Process.GetCurrentProcess();
    using var mm = new MemoryManager(process.Id);

    var str = "ÑPÑÇÑyÑrÑuÑÑ, ÑÅÑÇÑyÑrÑuÑÑ! x654";
    var strBytes = Encoding.UTF8.GetBytes(str);
    using var allDisposable = mm.AllocateMemory(100, out var allocation);
    mm.WriteBytes(allocation, strBytes);

    Assert.Throws<InvalidOperationException>(() => mm.ReadString(allocation, strBytes.Length - 1));
  }

}
