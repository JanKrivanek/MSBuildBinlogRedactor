﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Build.BinlogRedactor.BinaryLog;
using Microsoft.Build.BinlogRedactor.Commands;
using Microsoft.Build.BinlogRedactor.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Build.BinlogRedactor;
internal sealed class Program
{
    // TODO:
    // mask error on no input file?
    public static Task<int> Main(string[] args)
    {
        return BuildCommandLine()
            .UseHost(
            _ => Host.CreateDefaultBuilder(),
            host =>
            {
                host.ConfigureServices(services =>
                {
                    services.AddSingleton<RedactBinlogCommandHandler>();
                    services.AddSingleton<IStderrWriter, DefaultStderrWriter>();
                    services.AddSingleton<IStdoutWriter, DefaultStdoutWriter>();
                    BinlogRedactor.RegisterDefaultServices(services);
                    services.AddSingleton<BinlogRedactor>();
                })
                .AddCancellationTokenProvider()
                .ConfigureLogging(logging =>
                {
                    logging.ConfigureBinlogRedactorLogging(host);
                });
            })
            .UseExceptionHandler(ExceptionHandler)
            .UseParseErrorReporting((int)BinlogRedactorErrorCode.InvalidOption)
            .CancelOnProcessTermination()
            .UseHelp()
            .UseDefaults()
            .EnablePosixBundling(true)
            .Build()
            .InvokeAsync(args);
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        var command = new RedactBinlogCommand();
        command.AddGlobalOption(CommonOptionsExtensions.s_consoleVerbosityOption);

        return new CommandLineBuilder(command);
    }

    private static void ExceptionHandler(Exception exception, InvocationContext context)
    {
        if (exception is TargetInvocationException)
        {
            exception = exception.InnerException ?? exception;
        }

        ILogger? logger = context.BindingContext.GetService<ILogger<Program>>();
        logger?.LogCritical(exception, "Unhandled exception occurred ({type})", exception.GetType());
        context.ExitCode = (int)BinlogRedactorErrorCode.InternalError;
    }
}
