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

using System.Text;
using System.Reflection;

/// <summary>
/// Generate Python classes from C# classes. Purpose: Python type hints for using with PythonNET. no more no less.
/// </summary>
public class TypeHint
{
    public static readonly Logger logger = new Logger( "PythonNET Type Hints", ConsoleColor.Yellow );

    public Dictionary<string, string> m_DocStrings = new Dictionary<string, string>();

    public Dictionary<Type, string> MapTypeList = new()
    {
        { typeof(string), "str" },
        { typeof(string[]), "list[str]" },
        { typeof(List<string>), "list[str]" },
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(void), "None" },
        { typeof(bool), "bool" }
    };

    /// <summary>Load a xml document and parse into m_DocStrings</summary>
    /// <param name="XMLDocument">Path to a .xml file for C# summary to Python docstring</param>
    /// <param name="Override">Whatever to clear the summary if not empty</param>
    /// <returns>True if all right</returns>
    public bool LoadDocument( string? XMLDocument, bool Override = false )
    {
        if( !string.IsNullOrEmpty( XMLDocument ) )
        {
            if( File.Exists( XMLDocument ) )
            {
                Dictionary<string, string>? newxml = System.Xml.Linq.XDocument.Load( XMLDocument )
                    .Descendants( "member" )
                    .Where( m => m.Attribute( "name" ) != null && !string.IsNullOrWhiteSpace( m.Element( "summary" )?.Value ) )
                    .ToDictionary( m => m.Attribute( "name" )!.Value, m => m.Element( "summary" )!.Value.TrimStart() );

                if( newxml is null )
                {
                    return false;
                }

                if( Override )
                {
                    this.m_DocStrings = newxml;
                }
                else
                {
                    foreach( KeyValuePair<string, string> kv in newxml )
                    {
                        this.m_DocStrings[ kv.Key ] = kv.Value;
                    }
                }

                return true;
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

        return false;
    }

    /// <summary>
    /// </summary>
    /// <param name="XMLDocument">Path to a .xml file for C# summary to Python docstring</param>
    public TypeHint( string? XMLDocument )
    {
        this.LoadDocument( XMLDocument );
    }

    public void WriteMember( StringBuilder strbuild, char type, Type member, Type prop_type, MemberInfo prop )
    {
        strbuild.AppendLine( $"\t{prop.Name}: {this.MapType(prop_type, member)}" );

        if( this.m_DocStrings.TryGetValue( $"{type}:{member.Name}.{prop.Name}", out string? methodsummary ) )
        {
            strbuild.AppendLine( $"\t'''{methodsummary.Trim()}'''" );
        }
    }

    public string Generate( Type type, StringBuilder? strbuild = null )
    {
        if( strbuild is null )
        {
            strbuild = new StringBuilder();
        }
        else
        {
            strbuild.AppendLine();
        }

        strbuild.AppendLine( $"class {type.Name}:" );

        if( this.m_DocStrings.TryGetValue( $"T:{type.Name}", out string? classsummary ) )
        {
            strbuild.AppendLine($"\t'''{classsummary.Trim()}'''");
        }

        foreach( PropertyInfo prop in type.GetProperties() )
        {
            if( !prop.IsSpecialName )
            {
                this.WriteMember( strbuild, 'P', type, prop.PropertyType, prop );
            }
        }

        foreach( MethodInfo method in this.ExtensionMethods( type ) )
        {
            this.WriteMethods( strbuild, method, type );
        }

        foreach( MethodInfo method in type.GetMethods(
            BindingFlags.Public |
            BindingFlags.GetProperty |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly
        ) )
        {
            if( !method.IsSpecialName && !method.IsStatic && !method.IsPrivate )
            {
                this.WriteMethods( strbuild, method, type );
            }
        }

        return strbuild.ToString();
    }

    public void WriteMethods( StringBuilder strbuild, MethodInfo method, Type member )
    {
        ParameterInfo[] parameters = method.GetParameters();

        strbuild.Append( $"\tdef {method.Name}( self" );

        string doc_string = $"M:{member.Name}.{method.Name}";

        if( parameters.Length > 0 )
        {
            doc_string = $"M:{member.Name}.{method.Name}({string.Join( ",", parameters.Select( p => p.ParameterType.FullName ) ).Trim()})";

            if( method.IsDefined( typeof( System.Runtime.CompilerServices.ExtensionAttribute ), false ) )
            {
                parameters = parameters.Skip(1).ToArray();
            }

            if( parameters.Length > 0 )
            {
                strbuild.Append( $", " );

                int counter = 0;

                foreach( ParameterInfo param in parameters )
                {
                    counter++;

                    if( this.IsParameterNullable( param ) )
                    {
                        strbuild.Append( $"{param.Name}: Optional[{MapType(param.ParameterType, member)}] = None" );
                    }
                    else
                    {
                        strbuild.Append( $"{param.Name}: {MapType(param.ParameterType, member)}" );
                    }

                    if( counter < parameters.Length )
                    {
                        strbuild.Append( ", " );
                    }
                }
            }
        }

        strbuild.Append( $" ) -> {MapType(method.ReturnType, member)}:" );
        strbuild.AppendLine();

        if( this.m_DocStrings.TryGetValue( doc_string, out string? methodsummary ) )
        {
            strbuild.AppendLine( $"\t\t'''{methodsummary.Trim()}'''" );
        }

        strbuild.AppendLine( "\t\tpass;" );
    }

    public IEnumerable<MethodInfo> ExtensionMethods(Type extype)
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()

            from type in assembly.GetTypes()

            where type.IsSealed && type.IsAbstract && !type.IsGenericType && !type.IsNested

            from method in type.GetMethods(
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic
            )

            where method.IsDefined( typeof( System.Runtime.CompilerServices.ExtensionAttribute ), false )

            let parameters = method.GetParameters()

            where parameters.Length > 0 && parameters[0].ParameterType == extype

            select method;
    }

    public bool IsParameterNullable( ParameterInfo param )
    {
        NullabilityInfoContext nullability_info = new NullabilityInfoContext();

        NullabilityInfo info = nullability_info.Create( param );

        return ( info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable );
    }

    /// <summary>
    /// Maps a C# type to a Python type
    /// </summary>
    public string MapType( Type type, Type member )
    {
        // Pythonism, can't make classes return their own type as is not yet "defined" X[
        if( type == member )
            return "Any";

        // Threat Templates as Any
        if( type.IsGenericParameter )
            return "Any";

        if( type.IsGenericType )
        {
            type = type.GetGenericTypeDefinition();

            if( type == typeof( List<> ) )
                return $"list[Any]";

            if( type == typeof( Dictionary<,> ) )
                return $"dict[Any, Any]";

            return "Any";
        }

        if( this.MapTypeList.TryGetValue( type, out string? pyType ) && !string.IsNullOrWhiteSpace( pyType ) )
            return pyType;

        TypeHint.logger.warn
            .Write("Undefined python type conversion for CSharp's ")
            .Write( type.Name, ConsoleColor.Green )
            .NewLine()
            .Write( "Example: " )
            .Write( "MapTypeList", ConsoleColor.Cyan )
            .Write( "[ ", ConsoleColor.DarkCyan )
            .Write( "typeof", ConsoleColor.Blue )
            .Write( "(", ConsoleColor.Yellow )
            .Write( type.Name, ConsoleColor.Green )
            .Write( ")", ConsoleColor.Yellow )
            .Write( " ] ", ConsoleColor.DarkCyan )
            .Write( "=", ConsoleColor.Yellow )
            .Write( $"\"{type.Name.ToLower()}\"", ConsoleColor.DarkYellow )
            .Write( ";", ConsoleColor.Yellow )
            .NewLine();

        return "Any";
    }
}
