/*
MIT License

Copyright (c) 2025 Mikk155

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
*/

namespace Mikk.Logger;

#pragma warning disable IDE1006 // Naming Styles

/// <summary>
/// Logger levels enum
/// </summary>
public enum LoggerLevel
{
    None = 0,
    Trace =  1 << 0,
    Debug = 1 << 1,
    Information = 1 << 2,
    Warning = 1 << 3,
    Error = 1 << 4,
    Critical = 1 << 5
};

public class Logger
{
    public const ConsoleColor SquareBracketColor = ConsoleColor.DarkYellow;

    /// <summary>
    /// Global level for all Loggers
    /// </summary>
    public static LoggerLevel GlobalLevel = (
        LoggerLevel.Trace |
        LoggerLevel.Debug |
        LoggerLevel.Information |
        LoggerLevel.Warning |
        LoggerLevel.Error |
        LoggerLevel.Critical
    );

    /// <summary>
    /// Name of the logger
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Color of the logger's name
    /// </summary>
    public readonly System.ConsoleColor Color;

    /// <summary>
    /// Current level
    /// </summary>
    public LoggerLevel Level { get; set; }

    /// <summary>
    /// Active logger levels
    /// </summary>
    public LoggerLevel LogLevels = Logger.GlobalLevel;

    /// <summary>
    /// Set a logger level
    /// </summary>
    /// <param name="level">LoggerLevel enum's value</param>
    public void SetLogger( LoggerLevel level )
    {
        if( ( this.LogLevels & level ) == 0 )
            this.LogLevels |= level;
    }

    /// <summary>
    /// Clear a logger level
    /// </summary>
    /// <param name="level">LoggerLevel enum's value</param>
    public void ClearLogger( LoggerLevel level )
    {
        if( ( this.LogLevels & level ) != 0 )
            this.LogLevels &= ~level;
    }

    /// <summary>
    /// Toggle a logger level
    /// </summary>
    /// <param name="level">LoggerLevel enum's value</param>
    public void ToggleLogger( LoggerLevel level )
    {
        if( ( this.LogLevels & level ) == 0 )
            this.LogLevels |= level;
        else
            this.LogLevels &= ~level;
    }

    static Logger()
    {
        Console.CancelKeyPress += (p, e) =>
        {
            Console.ResetColor();
        };

        AppDomain.CurrentDomain.ProcessExit += (p, e) =>
        {
            Console.ResetColor();
        };
    }

    ~Logger()
    {
        Console.ResetColor();
    }

    public Logger( string LoggerName, System.ConsoleColor LoggerColor = System.ConsoleColor.Gray )
    {
        this.Level = LoggerLevel.None;
        this.Name = LoggerName;
        this.Color = LoggerColor;
    }

    /// <summary>
    /// Whatever the current Logger's level is active
    /// </summary>
    public bool IsLevelActive =>
        ( ( Level & LogLevels ) != 0 && ( Level & Logger.GlobalLevel ) != 0 );

    /// <summary>
    /// Write to the console output
    /// </summary>
    /// <param name="text">Text to write</param>
    /// <param name="color">Fore color of the text</param>
    [System.Runtime.CompilerServices.Discardable]
    public Logger Write( string? text, System.ConsoleColor color = System.ConsoleColor.White )
    {
        if( !string.IsNullOrEmpty( text ) && this.IsLevelActive )
        {
            Console.ForegroundColor = color;
            Console.Write( text );
            Console.ResetColor();
        }
        return this;
    }

    public Logger Write( string? text, int color ) {
        return Write( text, (System.ConsoleColor)color );
    }

    /// <summary>
    /// Write to the console output with a line terminator
    /// </summary>
    /// <param name="text">Text to write</param>
    /// <param name="color">Fore color of the text</param>
    public Logger WriteLine( string? text, System.ConsoleColor color = System.ConsoleColor.White )
    {
        if( this.IsLevelActive )
        {
            this.Write( text, color );
            Console.WriteLine();
        }
        return this;
    }

    public Logger WriteLine( string? text, int color ) {
        return WriteLine( text, (System.ConsoleColor)color );
    }

    /// <summary>
    /// Outputs a line terminator
    /// </summary>
    [System.Runtime.CompilerServices.Discardable]
    public Logger NewLine()
    {
        if( this.IsLevelActive )
        {
            Console.WriteLine();
        }
        return this;
    }

    /// <summary>
    ///  Make a noise if the current level is active
    /// </summary>
    [System.Runtime.CompilerServices.Discardable]
    public Logger Beep()
    {
        if( this.IsLevelActive )
        {
            Console.Beep();
        }
        return this;
    }

    /// <summary>
    /// Pause the program and wait for a user input
    /// </summary>
    [System.Runtime.CompilerServices.Discardable]
    public Logger Pause()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine( "Press Enter to continue." );
        Console.ResetColor();
        Console.ReadLine();
        return this;
    }

    /// <summary>
    /// Call a method on chain. useful if you want to shutdown something before Exit and as well as before Pause
    /// </summary>
    [System.Runtime.CompilerServices.Discardable]
    public Logger Call( Action fnCallback )
    {
        fnCallback.Invoke();
        return this;
    }

    /// <summary>
    /// Exit the program.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public void Exit( Action? fnCallback = null )
    {
        if( fnCallback is not null )
        {
            fnCallback.Invoke();
        }

        Environment.Exit(1);
    }

    private Logger WriteLoggerLine( string type, LoggerLevel level, System.ConsoleColor color )
    {
        this.Level = level;

        if( this.IsLevelActive )
        {
            Console.ForegroundColor = SquareBracketColor;
            Console.Write( '[' );

            Console.ForegroundColor = this.Color;
            Console.Write( this.Name );

            Console.ForegroundColor = SquareBracketColor;
            Console.Write( "] [" );

            Console.ForegroundColor = color;
            Console.Write( type );

            Console.ForegroundColor = SquareBracketColor;
            Console.Write( "] " );

            Console.ResetColor();
        }

        return this;
    }

    [System.Runtime.CompilerServices.Discardable]
    public Logger trace => this.WriteLoggerLine( "Trace", LoggerLevel.Trace, ConsoleColor.Gray );

    [System.Runtime.CompilerServices.Discardable]
    public Logger debug => this.WriteLoggerLine( "Debug", LoggerLevel.Debug, ConsoleColor.Cyan );

    [System.Runtime.CompilerServices.Discardable]
    public Logger info => this.WriteLoggerLine( "Info", LoggerLevel.Information, ConsoleColor.Green );

    [System.Runtime.CompilerServices.Discardable]
    public Logger warn => this.WriteLoggerLine( "Warning", LoggerLevel.Warning, ConsoleColor.Yellow );

    [System.Runtime.CompilerServices.Discardable]
    public Logger error => this.WriteLoggerLine( "Error", LoggerLevel.Error, ConsoleColor.Red );

    [System.Runtime.CompilerServices.Discardable]
    public Logger critical => this.WriteLoggerLine( "Critical", LoggerLevel.Critical, ConsoleColor.Red );

    /// <summary>
    /// Converts a single LoggerLevel to a string
    /// </summary>
    public static string LevelToString( LoggerLevel level )
    {
        return level switch
        {
            LoggerLevel.None => "None",
            LoggerLevel.Trace => "Trace",
            LoggerLevel.Debug => "Debug",
            LoggerLevel.Information => "Information",
            LoggerLevel.Warning => "Warning",
            LoggerLevel.Error => "Error",
            LoggerLevel.Critical => "Critical",
            _ => "Invalid"
        };
    }

    /// <summary>
    /// Converts a string to a LoggerLevel
    /// </summary>
    public static LoggerLevel LevelFromString( string level )
    {
        return level.ToLower() switch
        {
            "none" => LoggerLevel.None,
            "trace" => LoggerLevel.Trace,
            "Debug" => LoggerLevel.Debug,
            "Information" => LoggerLevel.Information,
            "Warning" => LoggerLevel.Warning,
            "Error" => LoggerLevel.Error,
            "Critical" => LoggerLevel.Critical,
            _ => LoggerLevel.None
        };
    }
}

#pragma warning restore IDE1006 // Naming Styles
