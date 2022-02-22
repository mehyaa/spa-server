namespace Spa.Server;

public class StaticFileOptions
{
    public string RootPath { get; set; }
    public string RequestPath { get; set; }
    public bool Download { get; set; }
}
