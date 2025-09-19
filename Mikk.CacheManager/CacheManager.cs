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

namespace Mikk.CacheManager;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Create a context of key-value pairs of data that is saved in the moment of update.
/// </summary>
public class Cache
{
    public const char m_ReservedKeyword = '_';

    public readonly string FileName;

    private Dictionary<string, JToken> KeyValues = new Dictionary<string, JToken>();

    private static readonly List<WeakReference<Cache>> _CacheInstances_ = new List<WeakReference<Cache>>();

    public Cache( string filename )
    {
        this.FileName = filename;

        if( !File.Exists( this.FileName ) )
        {
            string dir = Path.GetDirectoryName( this.FileName )!;

            if( !Directory.Exists( dir ) )
            {
                Directory.CreateDirectory( dir );
            }

            File.WriteAllText( this.FileName, "{}" );
        }
        else
        {
            try
            {
                JObject file_content = (JObject)JsonConvert.DeserializeObject( File.ReadAllText( this.FileName ) )!;

                foreach( KeyValuePair<string, JToken?> vals in file_content )
                {
                    this.KeyValues[ vals.Key ] = vals.Value!;
                }
            }
            catch
            {
                string name = Path.GetFileNameWithoutExtension( this.FileName );

                string datetime = DateTime.Now.ToString( "yyyy-MM-dd_HH.mm.ss" );

                string new_name =$"{name}_backup_{datetime}{Path.GetExtension( this.FileName )}";

                File.Copy( this.FileName, Path.Combine( Path.GetDirectoryName( this.FileName )!, new_name ) );
                File.WriteAllText( this.FileName, "{}" );

                Console.WriteLine( $"Failed to deserialize cache file \"{name}\"" );
                Console.WriteLine( $"Renamed file as \"{new_name}\" to prevent issues." );
            }
        }

        Cache._CacheInstances_.Add( new WeakReference<Cache>(this) );
    }

    private bool IsValidKey( string key )
    {
        if( key[0] == Cache.m_ReservedKeyword )
        {
            throw new FieldAccessException( $"Key names starting with {Cache.m_ReservedKeyword} are reserved for internal operations" );
        }
        return false;
    }

    /// <summary>
    /// Gets a JToken value from the cache
    /// </summary>
    /// <param name="key">Key name in the cache</param>
    /// <exception cref="FieldAccessException">Key name starts with "_"</exception>
    public JToken? GetRaw( string key )
    {
        if( this.IsValidKey( key ) && this.KeyValues.TryGetValue( key, out JToken? value ) && value is not null )
        {
            return value;
        }

        return null;
    }

    public T? Get<T>( string key )
    {
        return this.Get<T>( key, null );
    }

    /// <summary>
    /// Gets a value from the cache
    /// </summary>
    /// <typeparam name="T">Internally they're just JToken</typeparam>
    /// <param name="key">Key name in the cache</param>
    /// <param name="default_value">Default value to set if the cache is empty</param>
    /// <returns>The stored value if Any. default_value if none stored and store it. or default null if no default_value provided.</returns>
    /// <exception cref="FieldAccessException">Key name starts with "_"</exception>
    /// <exception cref="InvalidDataException">The cast type is not the same as the stored value</exception>
    public T Get<T>( string key, object? default_value )
    {
        Type type;

        if( this.IsValidKey( key ) && this.KeyValues.TryGetValue( key, out JToken? value ) && value is not null )
        {
            return value.Value<T>()!;
        }
        else if( default_value is not null )
        {
            this.KeyValues[ key ] = JToken.FromObject( default_value );
            this.Store();
            return (T)default_value;
        }

        throw new InvalidDataException( $"Key '{key}' has no value and 'default_value' was null" );
    }

    /// <summary>
    /// Sets a key-value pair to the cache
    /// </summary>
    /// <param name="key">Key name in the cache</param>
    /// <param name="value">Value to update</param>
    /// <exception cref="FieldAccessException">The key name starts with "_"</exception>
    public void Set( string key, object value )
    {
        if( this.IsValidKey( key ) )
        {
            this.KeyValues[ key ] = JToken.FromObject( value );
            this.Store();
        }
    }

    /// <summary>
    /// Store the cache manually. this is called automatically when Get/Set
    /// </summary>
    public void Store()
    {
        File.WriteAllText( this.FileName, JsonConvert.SerializeObject( this.KeyValues, Formatting.Indented ) );
    }

#if false
    private static void GlobalStore( object? s, EventArgs e )
    {
        foreach( WeakReference<Cache> cache in Cache._CacheInstances_ )
        {
            if( cache.TryGetTarget( out Cache? instance ) && instance is not null )
            {
                instance.Store();
            }
        }
    }

    static Cache()
    {
        AppDomain.CurrentDomain.ProcessExit += Cache.GlobalStore;
        Console.CancelKeyPress += Cache.GlobalStore;
    }

    ~Cache()
    {
        this.Store();
    }
#endif

    public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
    {
        return this.KeyValues.GetEnumerator();
    }
}
