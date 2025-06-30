// Copyright 2011-2017 Melvyn Laïly
// https://zerowidthjoiner.net

// This file is part of NegativeScreen.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NegativeScreen
{
	/// <summary>
	/// Represents the class storing the parsed key-value pairs from the configuration file.
	/// </summary>
	public interface IConfigurable
	{
		/// <summary>
		/// This method is automatically called when a key from the configuration file
		/// does not match any property marked with a <see cref="MatchingKeyAttribute"/>.
		/// This allows to handle dynamically declared keys in the configuration file.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		void HandleDynamicKey(string key, string value);
	}

	/// <summary>
	/// Represents a custom parser for a given type.
	/// This allows a better control over the values' parsing.
	/// </summary>
	public interface ICustomParser
	{
		/// <summary>
		/// Type this custom parser handles.
		/// </summary>
		Type ReturnType { get; }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="rawValue">
		/// Raw value to parse, trimmed from any whitespace.
		/// </param>
		/// <param name="customParameter">
		/// A custom parameter, from the <see cref="MatchingKeyAttribute"/>.
		/// Its behaviour is left to the implementer's discretion.
		/// </param>
		/// <returns></returns>
		object Parse(string rawValue, object customParameter);
	}

	/// <summary>
	/// Mark a property as the storage for a matching key in the configuration file.
	/// All keys are case insensitive.
	/// A custom parameter can be provided, which will be passed to the parser handling this property type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class MatchingKeyAttribute : Attribute
	{
		public string Key { get; }

		public MatchingKeyAttribute(string key)
		{
			Key = key.ToLowerInvariant();
		}

		public object CustomParameter { get; set; }
	}

	/// <summary>
	/// Static class allowing to parse configuration files.
	/// </summary>
	public static class Parser
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="content">Raw configuration file content.</param>
		/// <param name="configuration">The configuration object, implementing IConfigurable.</param>
		/// <param name="customParsers">Optional array of parser allowing to parse any type the way you want.</param>
		public static void AssignConfiguration(string content, IConfigurable configuration, params ICustomParser[] customParsers)
		{
			Dictionary<string, string> rawConfiguration = ParseConfiguration(content);
			var configurableProperties =
				(from p in configuration.GetType().GetProperties()
				 let attr = p.GetCustomAttributes(typeof(MatchingKeyAttribute), true)
				 where attr.Length == 1
				 select new { Property = p, Attribute = attr.First() as MatchingKeyAttribute })
				.ToList();
			foreach (var item in rawConfiguration)
			{
				string key = item.Key.ToLowerInvariant();
				var matchingProps = configurableProperties.Where(x => x.Attribute.Key == key);
				PropertyInfo matchingProp = null;
				MatchingKeyAttribute matchingAttribute = null;
				if (matchingProps.Any())
				{
					try
					{
						// try to find a matching property for this key
						var single = matchingProps.Single();
						matchingProp = single.Property;
						matchingAttribute = single.Attribute;
						configurableProperties.Remove(single);
					}
					catch (Exception ex)
					{
						throw new Exception(string.Format("The key \"{0}\" was found multiple times!", key), ex);
					}
					// parse value
					object parsedValue = null;
					// try to find a custom parser for this type
					var customParser = customParsers.SingleOrDefault(x => x.ReturnType == matchingProp.PropertyType);
					if (customParser != null)
					{
						parsedValue = customParser.Parse(item.Value, matchingAttribute.CustomParameter);
					}
					else
					{
						// if no custom parser is found, try to parse it with the default .net parsers
						try
						{
							parsedValue = Convert.ChangeType(item.Value, matchingProp.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
						}
						catch (Exception ex)
						{
							throw new Exception(string.Format("Unable to parse the value \"{0}\" to a {1}.", item.Value, matchingProp.PropertyType.Name), ex);
						}
					}
					// assign parsed value
					matchingProp.SetValue(configuration, parsedValue, null);
				}
				else
				{
					configuration.HandleDynamicKey(item.Key, item.Value);
				}
			}
			// handle the properties that were not in the configuration file
			foreach (var item in configurableProperties)
			{
				if (item.Attribute.CustomParameter != null)
				{
					item.Property.SetValue(configuration, item.Attribute.CustomParameter, null);
				}
			}
		}

		private static Dictionary<string, string> ParseConfiguration(string content)
		{
			var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, string> result = new Dictionary<string, string>();
			string currentKey = null;
			foreach (var line in lines)
			{
				string currentLine = line.Trim();
				if (currentLine.Length == 0 || currentLine.StartsWith("#"))
				{
					continue;
				}

				if (currentLine.Contains('='))
				{
					var splitted = currentLine.Split(new char[] { '=' }, 2);
					currentKey = splitted[0].Trim();
					string value = splitted[1].Trim();
					// remove quotes
					if (value.StartsWith("\"") && value.EndsWith("\""))
					{
						value = value.Substring(1, value.Length - 2);
					}
					result[currentKey] = value;
				}
				else
				{
					if (currentKey != null)
					{
						result[currentKey] += "\n" + currentLine;
					}
				}
			}
			return result;
		}
	}
}
