﻿using Hjson;
using JsonSubTypes;
using NCalc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Randomizer.Generator.Converters;
using Randomizer.Generator.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Randomizer.Generator.Test")]

namespace Randomizer.Generator.Core
{
	/// <summary>
	/// The base class for all definitions
	/// </summary>
	public abstract class BaseDefinition
    {
        private CalculationEngine _calculator;

		#region Public Static Methods
		/// <summary>
		/// Deserializes an HJSON byte array into a definition
		/// </summary>
		/// <param name="value">A byte array in the default encoding</param>
		/// <returns>A definition instance</returns>
		public static BaseDefinition Deserialize(Byte[] value) 
		{
			return Deserialize(Encoding.Default.GetString(value));
		}

		/// <summary>
		/// Deserializes an HJSON string into a definition
		/// </summary>
		/// <param name="value">An HJSON formatted string</param>
		/// <returns>A definition instance</returns>
		public static BaseDefinition Deserialize(String value) 
        {
            var json = HjsonValue.Parse(value).ToString();
			var serializer = JsonSerializer.Create(SerializerSettings);
			using var sReader = new StringReader(json);
			using var reader = new JsonTextReader(sReader);
            return serializer.Deserialize<BaseDefinition>(reader);
        }

        /// <summary>
        /// Serializes a definition into HJSON
        /// </summary>
		/// <param name="value">The definition to serialize</param>
		/// <param name="hjsonFormat">If true, the return value will be in hjson format otherwise it will be in json format</param>
		/// <returns>A string representation of the definition</returns>
        public static String Serialize(BaseDefinition value, Boolean hjsonFormat = true)
        {
			var builder = new StringBuilder();
			using var sWriter = new StringWriter(builder);
			using var writer = new JsonTextWriter(sWriter);
			var serializer = JsonSerializer.Create(SerializerSettings);
			serializer.Serialize(writer, value);
			var hjson = HjsonValue.Parse(builder.ToString());
            return hjson.ToString(hjsonFormat ? Stringify.Hjson : Stringify.Formatted);
        }
		#endregion

		#region Properties
		/// <summary>
		/// The settings used to serialize and deserialize definitions
		/// </summary>
		protected static JsonSerializerSettings SerializerSettings => new()
		{
			Formatting = Formatting.Indented,
			Converters =
					{
						new StringEnumConverter(),
						new JsonVersionConverter(),
						new JsonListOptionConverter(),
						JsonSubtypesConverterBuilder.Of(typeof(BaseDefinition), "GeneratorType")
													.RegisterSubtype<Assignment.AssignmentDefinition>(GeneratorTypes.Assignment)
													.RegisterSubtype<List.ListDefinition>(GeneratorTypes.List)
													.RegisterSubtype<Lua.LuaDefinition>(GeneratorTypes.Lua)
													.RegisterSubtype<Phonotactics.PhonotacticsDefinition>(GeneratorTypes.Phonotactics)
													.RegisterSubtype<Table.TableDefinition>(GeneratorTypes.Table)
													.SerializeDiscriminatorProperty() // ask to serialize the type property
													.Build(),
						JsonSubtypesConverterBuilder.Of(typeof(Table.BaseTable), "TableType")
													.RegisterSubtype<Table.LoopTable>(Table.TableTypes.Loop)
													.RegisterSubtype<Table.RandomTable>(Table.TableTypes.Random)
													.RegisterSubtype<Table.SelectTable>(Table.TableTypes.Select)
													.SerializeDiscriminatorProperty() // ask to serialize the type property
													.Build()
					}
		};

		/// <summary>Name of the generator definition</summary>
		[JsonProperty(Order = 0)]
		public String Name { get; set; }
		[JsonProperty(Order = 1)]
        /// <summary>Author of the generator definition</summary>
        public String Author { get; set; }
        /// <summary>Description of the generator definition</summary>
		[JsonProperty(Order = 2)]
        public String Description { get; set; }
		/// <summary>Version of the generator definition</summary>
		[JsonProperty(Order = 3)]
		public Version Version { get; set; }
		/// <summary>URL for more information about the generator definition</summary>
		[JsonProperty(Order = 4)]
		public String URL { get; set; }
		/// <summary>A list of tags for the generator definition</summary>
		[JsonProperty(Order = 5)]
		public List<String> Tags { get; set; }
		/// <summary>The format that the generator outputs</summary>
		[JsonProperty(Order = 6)]
		public OutputFormats OutputFormat { get; set; }
		/// <summary>Parameters for the generator definition</summary>
		[JsonProperty(Order = 7)]
		public virtual ParameterDictionary Parameters { get; set; } = new();
        #endregion

        #region Public Methods
		/// <summary>
		/// In child classes, overridden to process the definition and return a result
		/// </summary>
		/// <returns>The result of the definition process</returns>
        public abstract String Generate();
        #endregion

        #region Protected Methods
		/// <summary>
		/// Processes an expression through the <see cref="CalculationEngine"/>
		/// </summary>
		/// <param name="expression">The expression to process</param>
		/// <returns>The result of the evaluation</returns>
        protected String Calculate(String expression)
        {
            Object value;
            _calculator = new CalculationEngine(expression);
            _calculator.EvaluateFunction += OnEvaluateFunction;
            _calculator.EvaluateParameter += OnEvaluateParameter;
            value = _calculator.Evaluate();
            _calculator.EvaluateFunction -= OnEvaluateFunction;
            _calculator.EvaluateParameter -= OnEvaluateParameter;
            return value.ToString();
        }

		/// <summary>
		/// Called when the base generator doesn't isn't aware of the function named
		/// </summary>
		/// <param name="name">The name of the function found by the <see cref="CalculationEngine"/></param>
		/// <param name="e">The parameters and result for the call</param>
		protected virtual void OnEvaluateFunction(String name, FunctionArgs e) 
		{
			EvaluateFunction(name, e);
		}

		/// <summary>
		/// Called when the base generator isn't aware of the parameter named
		/// </summary>
		/// <param name="name">The name of the parameter found by the <see cref="CalculationEngine"/></param>
		/// <param name="e">The result for the call</param>
		protected virtual void OnEvaluateParameter(String name, ParameterArgs e) 
		{
			if (Parameters != null && Parameters.ContainsKey(name))
				e.Result = Parameters[name];
			else
				EvaluateParameter(name, e);
		}

		/// <summary>
		/// When overridden in child classes, handles unknown Function processing for the <see cref="Calculate(string)"/> method
		/// </summary>
		/// <param name="name">The name of the function found by the <see cref="CalculationEngine"/></param>
		/// <param name="e">The parameters and result for the call</param>
		protected virtual void EvaluateFunction(String name, FunctionArgs e) { }

		/// <summary>
		/// When overridden in child classes, handles unknown Parameter processing for the <see cref="Calculate(string)"/> method
		/// </summary>
		/// <param name="name">The name of the parameter found by the <see cref="CalculationEngine"/></param>
		/// <param name="e">The result for the call</param>
		protected virtual void EvaluateParameter(String name, ParameterArgs e) { }

		/// <summary>
		/// Serializes the definition and returns the string result
		/// </summary>
		/// <returns>The serialized definition</returns>
		public override String ToString()
		{
			return Serialize(this);
		}

		/// <summary>
		/// Including explicit cast to <see cref="String"/> for convenience
		/// </summary>
		/// <param name="definition">The definition to serialize</param>
		public static explicit operator String(BaseDefinition definition)
		{
			return definition.ToString();
		}

		/// <summary>
		/// Including explicit cast from <see cref="String"/> for convenience
		/// </summary>
		/// <param name="definition">The hjson to deserialize</param>
		public static explicit operator BaseDefinition(String hjson)
		{
			return Deserialize(hjson);
		}
		#endregion

	}
}
