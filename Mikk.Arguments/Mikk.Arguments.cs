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

namespace Mikk.Arguments;

/// <summary>
/// Arguments provided on the App's execution
/// </summary>
public class Arguments( string[] args )
{
    public readonly string[] args = args;

    /// <summary>
    /// Try get a launch parameter (Starting with -)
    /// </summary>
    /// <param name="arg">Whatever the argument exists</param>
    /// <returns></returns>
    public bool HasArgument( string arg )
    {
        return arg[0] == '-' && this.args.Contains( arg );
    }

    /// <summary>
    /// Try get a launch parameter's value (Starting with --)
    /// </summary>
    /// <param name="arg">Whatever value it contains, null if none</param>
    /// <returns></returns>
    public string? TryGetArgument( string arg )
    {
        if( arg.StartsWith( "--" ) )
        {
            int index = Array.IndexOf( this.args, arg );

            if( index >= 0 && index < this.args.Length - 1 )
            {
                return this.args[ index + 1 ];
            }
        }

        return null;
    }
}
