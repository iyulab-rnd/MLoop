using System.ComponentModel.DataAnnotations;

namespace MLoop.Worker.Tasks.CmdTask;

public class CmdConfig
{
    [Required]
    public string Command { get; set; } = string.Empty;

    [Required]
    public Dictionary<string, object> Args { get; set; } = [];

    public CmdConfig()
    {
    }

    public CmdConfig(string command, Dictionary<string, object> args)
    {
        Command = command;
        Args = args;
    }
}