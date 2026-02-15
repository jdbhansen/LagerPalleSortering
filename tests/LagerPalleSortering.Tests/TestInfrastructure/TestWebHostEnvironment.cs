using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace LagerPalleSortering.Tests.TestInfrastructure;

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string rootPath)
    {
        ApplicationName = "LagerPalleSortering.Tests";
        EnvironmentName = "Development";
        ContentRootPath = rootPath;
        WebRootPath = rootPath;
        ContentRootFileProvider = new NullFileProvider();
        WebRootFileProvider = new NullFileProvider();
    }

    public string ApplicationName { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string EnvironmentName { get; set; }
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}
