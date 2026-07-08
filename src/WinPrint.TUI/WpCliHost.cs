using System.Reflection;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Cli;
using Terminal.Gui.Drivers;

namespace WinPrint.TUI;

/// <summary>
///     <see cref="CliHost" /> fork that dispatches <see cref="IHeadlessCliCommand" /> without
///     initializing Terminal.Gui, matching the <c>help --cat</c> lifecycle (#240).
/// </summary>
/// <remarks>
///     Upstream <see cref="CliHost" /> is sealed and wraps every <see cref="CommandKind.Input" />
///     command in an inline Terminal.Gui session. Headless commands such as <c>wp print</c> never
///     paint a view, but teardown still parks the cursor on the shell prompt and corrupts scrollback.
///     Track an upstream <c>IHeadlessCliCommand</c> in Terminal.Gui.Cli and delete this type when it
///     lands.
/// </remarks>
public sealed class WpCliHost
{
    private readonly IHelpProvider _helpProvider;
    private readonly CliHostOptions _options;
    private readonly ArgParser _parser;

    /// <summary>Creates a host, applies configuration, creates its registry, and registers built-ins.</summary>
    public WpCliHost(Action<CliHostOptions>? configure = null)
    {
        _options = new CliHostOptions();
        configure?.Invoke(_options);
        _helpProvider = _options.HelpProvider ?? new MetadataHelpProvider();
        Registry = new CommandRegistry();
        RegisterBuiltIns();
        _parser = new ArgParser(_options.GlobalOptions, _options.MaxInitialChars);
    }

    /// <summary>The command registry owned by this host. Register consumer commands before RunAsync.</summary>
    public ICommandRegistry Registry { get; }

    /// <summary>Parses args, dispatches a command, writes output, and returns a process exit code.</summary>
    public async Task<int> RunAsync(
        string[] args,
        CancellationToken cancellationToken = default,
        TextWriter? stdout = null,
        TextWriter? stderr = null)
    {
        stdout ??= Console.Out;
        stderr ??= Console.Error;

        ArgParser.ParseResult initialParse = _parser.Parse(args);

        if (!initialParse.Success)
        {
            if (_options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync(args, cancellationToken, stdout, stderr);
            }

            stderr.WriteLine(initialParse.Error);
            return ExitCodes.UsageError;
        }

        if (initialParse.RootFlag is { } rootFlag)
        {
            if (rootFlag == ArgParser.RootFlag.Help && args.Length == 0 && _options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync(args, cancellationToken, stdout, stderr);
            }

            WriteRootFlag(rootFlag, stdout);
            return ExitCodes.Ok;
        }

        if (initialParse.Alias is null || !Registry.TryResolve(initialParse.Alias, out ICliCommand? command) ||
            command is null)
        {
            if (_options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync(args, cancellationToken, stdout, stderr);
            }

            stderr.WriteLine($"Unknown command '{initialParse.Alias}'.");
            return ExitCodes.UsageError;
        }

        return await DispatchCommandAsync(args, (ICliCommand)command, cancellationToken, stdout, stderr);
    }

    private async Task<int> RunWithDefaultCommandAsync(
        string[] args,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        if (!Registry.TryResolve(_options.DefaultCommand!, out ICliCommand? defaultCmd) || defaultCmd is null)
        {
            stderr.WriteLine($"Default command '{_options.DefaultCommand}' is not registered.");
            return ExitCodes.UsageError;
        }

        string[] adjusted = [_options.DefaultCommand!, .. args];
        ArgParser.ParseResult parse = _parser.Parse(adjusted, defaultCmd, true);

        if (!parse.Success || parse.Options is null)
        {
            stderr.WriteLine(parse.Error);
            return ExitCodes.UsageError;
        }

        return await ExecuteCommandAsync(defaultCmd, parse.Options, cancellationToken, stdout, stderr);
    }

    private async Task<int> DispatchCommandAsync(
        string[] args,
        ICliCommand command,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        ArgParser.ParseResult parse = _parser.Parse(args, command);

        if (!parse.Success || parse.Options is null)
        {
            stderr.WriteLine(parse.Error);
            return ExitCodes.UsageError;
        }

        return await ExecuteCommandAsync(command, parse.Options, cancellationToken, stdout, stderr);
    }

    private async Task<int> ExecuteCommandAsync(
        ICliCommand command,
        CommandRunOptions runOptions,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        var stdoutIsConsole = ReferenceEquals(stdout, Console.Out);
        var stderrIsConsole = ReferenceEquals(stderr, Console.Error);

        if (runOptions.Initial is not null && !command.TryValidateInitial(runOptions.Initial, runOptions))
        {
            stderr.WriteLine("Invalid --initial value.");
            return ExitCodes.ValidationError;
        }

        using CancellationTokenSource? timeoutSource = runOptions.Timeout is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeoutSource is not null && runOptions.Timeout is { } timeout)
        {
            timeoutSource.CancelAfter(timeout);
        }

        CancellationToken effectiveToken = timeoutSource?.Token ?? cancellationToken;

        if (command is IViewerCommand viewer && runOptions.Cat)
        {
            CommandResult? catResult;

            try
            {
                catResult = await viewer.RenderCatAsync(runOptions, stdout, effectiveToken);
            }
            catch (OperationCanceledException)
            {
                return ExitCodes.Cancelled;
            }

            if (catResult is { } cat)
            {
                if (cat.Status is not (CommandStatus.Ok or CommandStatus.NoResult))
                {
                    ResultWriter.Write(cat, runOptions.JsonOutput, stdout, stderr, runOptions.OutputPath,
                        _options.ResultJsonResolver);
                }

                return ExitCodes.FromResult(cat);
            }
        }

        CommandResult result;

        if (command is IHeadlessCliCommand headless)
        {
            try
            {
                result = await headless.RunHeadlessAsync(runOptions, effectiveToken);
            }
            catch (OperationCanceledException)
            {
                result = CreateCancelledResult();
            }
        }
        else
        {
            try
            {
                result = await RunWithTerminalGuiAsync(command, runOptions, effectiveToken);
            }
            catch (OperationCanceledException)
            {
                result = CreateCancelledResult();
            }

            if (stdoutIsConsole || stderrIsConsole)
            {
                if (Console.OutputEncoding.CodePage != Encoding.UTF8.CodePage)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }

                if (stdoutIsConsole)
                {
                    stdout = Console.Out;
                }

                if (stderrIsConsole)
                {
                    stderr = Console.Error;
                }
            }
        }

        if (!ResultWriter.Write(result, runOptions.JsonOutput, stdout, stderr, runOptions.OutputPath,
                _options.ResultJsonResolver))
        {
            return ExitCodes.UsageError;
        }

        return ExitCodes.FromResult(result);
    }

    private static CommandResult CreateCancelledResult()
    {
        return new CommandResult(CommandStatus.Cancelled, null, null, null);
    }

    private async Task<CommandResult> RunWithTerminalGuiAsync(
        ICliCommand command,
        CommandRunOptions runOptions,
        CancellationToken cancellationToken)
    {
        var useInline = command.Kind == CommandKind.Input && !runOptions.Fullscreen;
        AppModel previousAppModel = Application.AppModel;
        Application.AppModel = useInline ? AppModel.Inline : AppModel.FullScreen;

        try
        {
            using IApplication app = Application.Create();
            app.Init();

            return await command.RunAsync(app, runOptions.Initial, runOptions, cancellationToken);
        }
        finally
        {
            Application.AppModel = previousAppModel;
        }
    }

    private void WriteRootFlag(ArgParser.RootFlag rootFlag, TextWriter stdout)
    {
        switch (rootFlag)
        {
            case ArgParser.RootFlag.Help:
                var helpMarkdown = _helpProvider.GetRootHelp(Registry) ??
                                   new MetadataHelpProvider().GetRootHelp(Registry) ?? string.Empty;
                MarkdownRenderer.RenderToAnsi(helpMarkdown, stdout);
                break;
            case ArgParser.RootFlag.Version:
                stdout.WriteLine($"{_options.ApplicationName} {_options.Version ?? "0.0.0"}");
                break;
            case ArgParser.RootFlag.OpenCli:
                stdout.WriteLine(OpenCliWriter.Generate(Registry, _options));
                break;
        }
    }

    private void RegisterBuiltIns()
    {
        Registry.Register(new HelpCommand(Registry, _helpProvider));

        if (_options.AgentGuide is not null)
        {
            Registry.Register(new AgentGuideCommand(ResolveAgentGuide()));
        }
    }

    private string ResolveAgentGuide()
    {
        if (!_options.AgentGuideIsResource)
        {
            return _options.AgentGuide ?? string.Empty;
        }

        Assembly assembly = _options.ResourceAssembly ?? Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("No assembly is available for resolving AgentGuide.");

        using Stream? stream = assembly.GetManifestResourceStream(_options.AgentGuide!);

        if (stream is null)
        {
            throw new InvalidOperationException($"AgentGuide resource '{_options.AgentGuide}' was not found.");
        }

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
