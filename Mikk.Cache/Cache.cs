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

namespace Mikk.Cache;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

/// <summary>
/// Simple cache context
/// </summary>
public class Cache : IEnumerable<KeyValuePair<string, JToken?>>
{
    /// <summary>
    /// Prefix key names for intenal operations
    /// </summary>
    public const char InternalPrefix = '_';

    /// <summary>
    /// Absolute path to the json file in the current cache instance
    /// </summary>
    public readonly string FileName;

    /// <summary>
    /// Object of the current cache instance
    /// </summary>
    public JObject data;

    public Cache( string filename )
    {
        this.FileName = filename;
        this.data = Cache.Read( this.FileName );
        this.data[ $"{InternalPrefix}Cache.FileName" ] = this.FileName;

        AppDomain.CurrentDomain.ProcessExit += ( e, a ) => { this.Write(); };
        Console.CancelKeyPress += ( e, a ) => { this.Write(); };
    }

    /// <summary>
    /// Try write the current cache instance to its file
    /// </summary>
    public void Write()
    {
        Cache.Write( this.FileName, this.data );
    }

    ~Cache()
    {
        Cache.Write( this.FileName, this.data );
    }

    public IEnumerator<KeyValuePair<string, JToken?>> GetEnumerator()
    {
        foreach( var pairs in this.data )
        {
            if( !this.IsInternal( pairs.Key ) )
                yield return pairs;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

    public bool IsInternal( string key ) => key[0] == Cache.InternalPrefix;

    /// <summary>
    /// Gets and cast an object from the JObject.
    /// </summary>
    /// <exception cref="AccessViolationException"></exception>
    public T? Get<T>( string key )
    {
        if( this.IsInternal( key ) )
            throw new AccessViolationException( $"Key name \"{key}\" is reserved for internal operations" );

        if( this.data.TryGetValue( key, out JToken? token ) && token is not null )
        {
            return token.Value<T>();
        }

        return default;
    }

    public JToken? this[ string key ]
    {
        get
        {
            if( this.data.ContainsKey( key ) )
            {
                return this.data[ key ];
            }
            return null;
        }
        set
        {
            this.data[ key ] = value;
            Cache.Write( this.FileName, this.data );
        }
    }

    /// <summary>
    /// Gets and cast an object from the JObject. if it doesn't exists the default_value will be writed and returned
    /// </summary>
    /// <exception cref="AccessViolationException"></exception>
    public T Get<T>( string key, T default_value )
    {
        T? type = this.Get<T>( key );

        if( type is not null )
            return type;

        this.data[ key ] = JToken.FromObject( default_value ?? throw new NullReferenceException() );

        Cache.Write( this.FileName, this.data );

        return (T)default_value;
    }

    /// <summary>
    /// Find the cache owner of this object and try to write the cache instance
    /// </summary>
    public static void Write( JObject obj )
    {
        while( obj.Parent is not null )
        {
            obj = (JObject)obj.Parent;
        }

        if( obj.TryGetValue( $"{Cache.InternalPrefix}Cache.FileName", out JToken? token ) && token is not null )
        {
            Cache.Write( token.Value<string>()!, obj );
        }
    }

    /// <summary>
    /// Read a cache file into a JObject.
    /// If it doesn't exists a new one will be generated.
    /// If fails to open a backup will be generated and a new one will be returned.
    /// </summary>
    /// <param name="filename">Absolute path to the json file</param>
    /// <returns>The json object</returns>
    public static JObject Read( string filename )
    {
        if( !File.Exists( filename ) )
        {
            string dir = Path.GetDirectoryName( filename )!;

            if( !Directory.Exists( dir ) )
            {
                Directory.CreateDirectory( dir );
            }

            File.WriteAllText( filename, "{}" );
        }
        else
        {
            try
            {
                return (JObject?)JsonConvert.DeserializeObject( File.ReadAllText( filename ) ) ?? throw new Exception();
            }
            catch
            {
                string name = Path.GetFileNameWithoutExtension( filename );

                string datetime = DateTime.Now.ToString( "yyyy-MM-dd_HH.mm.ss" );

                string new_name =$"{name}_backup_{datetime}{Path.GetExtension( filename )}";

                File.Copy( filename, Path.Combine( Path.GetDirectoryName( filename )!, new_name ) );
                File.WriteAllText( filename, "{}" );

                Console.WriteLine( $"Failed to deserialize cache file \"{name}\"" );
                Console.WriteLine( $"Renamed file as \"{new_name}\" to prevent issues." );
            }
        }

        return new JObject();
    }

    /// <summary>
    /// Try write the content cache into the given filename
    /// </summary>
    /// <param name="filename">Absolute path to a json file</param>
    /// <param name="content">Object to write</param>
    /// <returns>The given object after validation</returns>
    public static JObject? Write( string filename, object content )
    {
        string dir = Path.GetDirectoryName( filename )!;

        if( !Directory.Exists( dir ) )
        {
            Directory.CreateDirectory( dir );
        }

        string Serialized = JsonConvert.SerializeObject( content, Newtonsoft.Json.Formatting.Indented );

        JObject? validation = (JObject)JsonConvert.DeserializeObject( File.ReadAllText( filename ) )!;

        if( validation is not null )
        {
            File.WriteAllText( filename, Serialized );
        }

        return validation;
    }
}
