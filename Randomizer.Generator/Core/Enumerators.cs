﻿using System.Text.Json.Serialization;

namespace Randomizer.Generator.Core
{
	/// <summary>
	/// Types of generators
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GeneratorTypes
    {        
        Assignment,
        List,
		Lua,
        Phonotactics,
		Table
    }

    /// <summary>
    /// Output formats of a generator's results
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OutputFormats
    {
		/// <summary>The generator will output plain text</summary>
        Text,
		/// <summary>The generator will output text in HTML format</summary>
        Html
    }

    /// <summary>
    /// Enumeration of the available types for a parameter
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParameterTypes
    {
		/// <summary>A text entry</summary>
        Text,
		/// <summary>A integer number</summary>
        Integer,
		/// <summary>A decimal number</summary>
        Decimal,
		/// <summary>A list of options to select from</summary>
        List,
		/// <summary>A date and/or time</summary>
        Date,
		/// <summary>A boolean</summary>
        Boolean
    }

    /// <summary>
    /// Possible cases for text lcase, UCASE, Tcase
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TextCases
    {
		/// <summary>Leave the text case as it is.</summary>
        None,
		/// <summary>Change the text case to all lowercase</summary>
        Lower,
		/// <summary>Change the text case to all UPPERCASE</summary>
        Upper,
		/// <summary>Change the text case to Title Case</summary>
        Title
    }
}