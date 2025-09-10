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

namespace Mikk.PythonNET;

using Mikk.Logger;

/// <summary>
/// Generate Python classes from C# classes. Purpose: Python type hints for using with PythonNET. no more no less.
/// </summary>
public class TypeHint
{
    public static readonly Logger logger = new Logger( "PythonNET Type Hints", ConsoleColor.Yellow );

    private readonly Dictionary<string, string> DocStrings = new Dictionary<string, string>();

    private Dictionary<Type, string> MapTypeList = new()
    {
        { typeof(string), "str" },
        { typeof(string[]), "list[str]" },
        { typeof(List<string>), "list[str]" },
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(void), "None" },
        { typeof(bool), "bool" }
    };

    public string GetPairs()
    {
        System.Text.StringBuilder s = new System.Text.StringBuilder();

        foreach( var kv in this.DocStrings )
        {
            s.AppendLine( $"{kv.Key}" );
            s.AppendLine( $"    {kv.Value}" );
        }

        return s.ToString();
    }

    /// <summary>
    /// </summary>
    /// <param name="XMLDocument">Path to a .xml file for C# summary to Python docstring</param>
    public TypeHint( string? XMLDocument )
    {
        if( !string.IsNullOrEmpty( XMLDocument ) )
        {
            if( File.Exists( XMLDocument ) )
            {
                this.DocStrings = System.Xml.Linq.XDocument.Load( XMLDocument )
                    .Descendants( "member" )
                    .Where( m => m.Attribute( "name" ) != null && !string.IsNullOrWhiteSpace( m.Element( "summary" )?.Value ) )
                    .ToDictionary( m => m.Attribute( "name" )!.Value, m => m.Element( "summary" )!.Value.TrimStart() );
            }
            else
            {
                TypeHint.logger.error
                    .Write( "XMLDocument file at path \"" )
                    .Write( XMLDocument, ConsoleColor.Green )
                    .Write( "\" doesn't exists!" )
                    .NewLine();
            }
        }
        else
        {
            TypeHint.logger.info.WriteLine( "No XMLDocument specified. Python docstring will not be generated" );
        }
    }

    public string Generate( Type type, System.Text.StringBuilder? StringBuilder = null )
    {
        if( StringBuilder is null )
        {
            StringBuilder = new System.Text.StringBuilder();
        }
        else
        {
            StringBuilder.AppendLine();
        }

        StringBuilder.AppendLine( $"class {type.Name}:" );

        if( this.DocStrings.TryGetValue( $"T:{type.Name}", out string? ClassSum ) )
        {
            StringBuilder.AppendLine($"\t'''{ClassSum}'''");
        }

        StringBuilder.AppendLine();

        foreach( System.Reflection.PropertyInfo prop in type.GetProperties() )
        {
            StringBuilder.AppendLine( $"\t{prop.Name}: {this.MapType(prop.PropertyType, type)}" );

            if( this.DocStrings.TryGetValue( $"P:{type.Name}.{prop.Name}", out string? MethodSum ) )
            {
                StringBuilder.AppendLine( $"\t'''{MethodSum}'''" );
            }
        }

        foreach( System.Reflection.MethodInfo method in type.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.GetProperty |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly |
            System.Reflection.BindingFlags.Static
        ) )
        {
            this.WriteMethods( StringBuilder, method, type );
        }

        return StringBuilder.ToString();
    }

    private void WriteMethods( System.Text.StringBuilder StringBuilder, System.Reflection.MethodInfo method, Type member )
    {
        if( method.IsPrivate || method.IsStatic || method.IsSpecialName )
        {
            return;
        }

        System.Reflection.ParameterInfo[] parameters = method.GetParameters();

        StringBuilder.Append( $"\tdef {method.Name}( self" );

        if( parameters.Length > 0 )
        {
            StringBuilder.Append( $", " );

            if( method.IsDefined( typeof( System.Runtime.CompilerServices.ExtensionAttribute ), false ) )
            {
                parameters = parameters.Skip(1).ToArray();
            }

            if( parameters.Length > 0 )
            {
                StringBuilder.Append( string.Join( ", ", method.GetParameters().Select( p => $"{p.Name}: {MapType(p.ParameterType, member)}" ) ) );
            }
        }

        StringBuilder.Append( $" ) -> {MapType(method.ReturnType, member)}:" );
        StringBuilder.AppendLine();

        StringBuilder.AppendLine( "\t\tpass;" );
    }

    private static bool IsNullable( System.Reflection.PropertyInfo property )
    {
        System.Reflection.NullabilityInfoContext nullabilityInfoContext = new System.Reflection.NullabilityInfoContext();

        System.Reflection.NullabilityInfo info = nullabilityInfoContext.Create( property );

        return ( info.WriteState == System.Reflection.NullabilityState.Nullable || info.ReadState == System.Reflection.NullabilityState.Nullable );
    }

    /// <summary>
    /// Maps a C# type to a Python type
    /// </summary>
    public string MapType( Type type, Type member )
    {
        // Pythonism, can't make classes return their own type as is not yet "defined" X[
        if( type == member )
            return "Any";

        if( MapTypeList.TryGetValue( type, out string? pyType ) && !string.IsNullOrWhiteSpace( pyType ) )
            return pyType;

        TypeHint.logger.warn
            .Write("Undefined python type conversion for CSharp's ")
            .Write( type.Name, ConsoleColor.Green )
            .NewLine()
            .Write( "Try using this class's method " )
            .Write( "AddTypeConversion", ConsoleColor.Yellow )
            .NewLine();

        return "Any";
    }

    /// <summary>
    /// Adds a C#-Python type conversion
    /// </summary>
    public void AddTypeConversion( Type type, string conversion )
    {
        MapTypeList[ type ] = conversion;
    }
}
