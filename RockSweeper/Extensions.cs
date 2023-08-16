﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper
{
    public static class Extensions
    {
        /// <summary>
        /// Gets the enum title from it's attribute or name.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string GetEnumTitle( this Enum value )
        {
            FieldInfo fi = value.GetType().GetField( value.ToString() );

            var attributes = (TitleAttribute[])fi.GetCustomAttributes( typeof( TitleAttribute ), false );

            if ( attributes != null && attributes.Length > 0 )
            {
                return attributes[0].Title;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Gets the enum description attribute value or an empty string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string GetEnumDescription( this Enum value )
        {
            FieldInfo fi = value.GetType().GetField( value.ToString() );

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes( typeof( DescriptionAttribute ), false );

            if ( attributes != null && attributes.Length > 0 )
            {
                return attributes[0].Description;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns a new enumerable that is sorted by taking prequisites into account.
        /// </summary>
        /// <typeparam name="T">The type to be sorted.</typeparam>
        /// <param name="nodes">The nodes to be sorted.</param>
        /// <param name="connected">The connected prerequisites that must appear before the current item.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Cyclic connections are not allowed</exception>
        public static IEnumerable<T> TopologicalSort<T>( this IEnumerable<T> nodes, Func<T, IEnumerable<T>> connected )
        {
            var elems = nodes.ToDictionary( node => node,
                                           node => new HashSet<T>( connected( node ) ) );
            while ( elems.Count > 0 )
            {
                var elem = elems.FirstOrDefault( x => x.Value.Count == 0 );
                if ( elem.Key == null )
                {
                    throw new ArgumentException( "Cyclic connections are not allowed" );
                }
                elems.Remove( elem.Key );
                foreach ( var selem in elems )
                {
                    selem.Value.Remove( elem.Key );
                }
                yield return elem.Key;
            }
        }

        /// <summary>
        /// Randomizes the letters and numbers in a string.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="allowedCharacters">A set of characters to be allowed.</param>
        /// <returns></returns>
        public static string RandomizeLettersAndNumbers( this string s, params char[] allowedCharacters )
        {
            var randomizer = new Bogus.Randomizer();
            var chars = s.ToArray();

            for ( int i = 0; i < chars.Length; i++ )
            {
                char c = chars[i];

                if ( allowedCharacters.Contains( c ) )
                {
                    continue;
                }

                if ( char.IsLower( c ) )
                {
                    chars[i] = randomizer.Char( 'a', 'z' );
                }
                else if ( char.IsUpper( c ) )
                {
                    chars[i] = randomizer.Char( 'A', 'Z' );
                }
                else if ( char.IsNumber( c ) )
                {
                    chars[i] = randomizer.Char( '0', '9' );
                }
            }

            return new string( chars );
        }

        /// <summary>
        /// Convert a set of dictionaries into typed objects.
        /// </summary>
        /// <typeparam name="T">The type of object to be created.</typeparam>
        /// <param name="items">The items to be converted.</param>
        /// <returns>A list of typed objects.</returns>
        public static List<T> ToObjects<T>( this IEnumerable<Dictionary<string, object>> items )
            where T : class, new()
        {
            return items
                .Select( item =>
                {
                    var typedItem = new T();
                    var itemType = typeof( T );

                    foreach ( var kvp in item )
                    {
                        var property = itemType.GetProperty( kvp.Key );

                        property.SetValue( typedItem, kvp.Value );
                    }

                    return typedItem;
                } )
                .ToList();
        }

        /// <summary>
        /// Determines if the file is likely an image.
        /// </summary>
        /// <param name="file">The file to be checked.</param>
        /// <returns><c>true</c> if the file is most likely an image; otherwise <c>false</c>.</returns>
        public static bool IsImage( this BinaryFile file )
        {
            return file.FileName.EndsWith( ".jpg", StringComparison.CurrentCultureIgnoreCase )
                || file.FileName.EndsWith( ".jpeg", StringComparison.CurrentCultureIgnoreCase )
                || file.FileName.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase )
                || file.MimeType.StartsWith( "image/", StringComparison.CurrentCultureIgnoreCase );
        }
    }
}
