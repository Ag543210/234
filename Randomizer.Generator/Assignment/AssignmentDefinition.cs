﻿using NCalc;
using Newtonsoft.Json;
using Randomizer.Generator.Core;
using Randomizer.Generator.Exceptions;
using Randomizer.Generator.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Randomizer.Generator.Assignment
{
	/// <summary>
	/// Using a series of line items containing line item references, equations, and variables generates random content
	/// </summary>
	public class AssignmentDefinition : BaseDefinition
	{
		#region Constants
		/// <summary>The starting lineitem for generation</summary>
		private const String START_ITEM = "Start";
		/// <summary>The maximum level of recursion to allow before aborting the generation process</summary>
		private const Int32 MAX_RECURSION_DEPTH = 1000;
		/// <summary>The maximum number of loops to allow before aborting the generation process</summary>
		private const Int32 MAX_LOOP_COUNT = 10000000;
		#endregion

		#region Members
		/// <summary>Tracks the number of loops that have occured during generation</summary>
		private Int32 _loopCount;
		/// <summary>Tracks the recursion depth during generation</summary>
		private Int32 _recursionDepth;
		/// <summary>Is set to true when importing of <see cref="Imports"/> is complete</summary>
		private Boolean _importComplete;
		/// <summary>Denotes that pre processing is happening</summary>
		private Boolean _preprocessing = true;
		#endregion

		#region Properties
		/// <summary>A list of variables to populate</summary>
		[JsonProperty(Order = 109)]
		public PreProcessList PreProcessItems
		{
			get => GetProperty(new PreProcessList());
			set => SetProperty(value);
		}
		/// <summary>List of line items used in the generator</summary>
		[JsonProperty(Order = 110)]
		public LineItemDictionary LineItems
		{
			get => GetProperty(new LineItemDictionary());
			set => SetProperty(value);
		}
		/// <summary>List of imported assignment generators</summary>
		[JsonProperty(Order = 101)]
		public List<String> Imports
		{
			get => GetProperty(new List<String>());
			set => SetProperty(value);
		}
		/// <summary>The case to convert the text to</summary>
		[JsonProperty(Order = 100)]
		public TextCases TextCase
		{
			get => GetProperty(TextCases.None);
			set => SetProperty(value);
		}
		/// <summary>Variables added during generation</summary>
		private InsensitiveDictionary<String> Variables { get; set; } = new();
		public override Boolean SupportsParameters => true;
		#endregion

		#region Public Methods
		/// <summary>
		/// Generates random content
		/// </summary>
		/// <returns>The generated content</returns>
		public override string Generate()
		{
			if (LineItems.Sum(li => li.Value.Count) == 0) throw new DefinitionException("Definition does not have any line items.");
			ParseLineItems();
			if (ValidateParameters())
			{
				PreProcess();
				var next = START_ITEM;
				LineItem item;
				LoadImports();

				_loopCount = 0;
				_recursionDepth = 0;

				item = LineItems[next].SelectRandomItem();
				var value = EvaluateLineItem(next, item);

				return value.ToCase(TextCase);
			}
			return String.Empty;
		}

		public override String Analyze(AnalyzeOptions options)
		{
			var analysis = new AnalysisWriter(base.Analyze(options));

			analysis.AppendHeader(GeneratorTypes.Assignment);
			analysis.AppendLine($"Category Count: {LineItems.Count:#,##0}");
			analysis.AppendLine($"Line Item Count: {LineItems.Sum(kvp => kvp.Value.Count):#,##0}");

			if (Imports.Any())
			{
				analysis.AppendHeader("Imports");
				foreach (var import in Imports)
				{
					analysis.AppendLine($"{import}");
					if (options.HasFlag(AnalyzeOptions.ShowImportDetails))
					{
						try
						{
							var importDef = LoadImport(import);
							var importDetail = importDef.Analyze(options).Split('\n');
							analysis.AppendSeparator();
							analysis.Level++;
							foreach (var detail in importDetail)
							{
								analysis.AppendLine(detail);
							}
							analysis.Level--;
						}
						catch (Exception ex)
						{
							analysis.AppendLine("Error Loading Import:");
							analysis.AppendLine(ex.Message);
						}
					}
				}
			}

			if (LineItems.Any() && options.HasFlag(AnalyzeOptions.IterateItems))
			{
				foreach (var lineItem in LineItems)
				{
					analysis.AppendLine();
					analysis.AppendLine(lineItem.Key);
					analysis.AppendSeparator();
					foreach (var item in lineItem.Value)
					{
						analysis.AppendLine(item.Content);
						var tokens = Tokenizer.Tokenize(item.Content);
						var i = 1;
						analysis.Level++;
						foreach (var token in tokens)
						{
							var value = token.Value;
							if (token.TokenType == TokenTypes.Text) value = $"'{value}'";
							analysis.AppendItemValue($"{i:00}. {token.TokenType}", value, 14);
							i++;
						}
						analysis.Level--;
						analysis.AppendLine();
					}
				}
			}

			return analysis.ToString();
		}
		#endregion

		protected void ParseLineItems()
		{
			const String DECK = ".Deck";
			var remove = new List<String>();
			var decks = LineItems.Where(kvp => kvp.Key.EndsWith(DECK)).ToList();
			foreach (var item in decks)
			{
				item.Value.Draw = true;
				LineItems.Add(item.Key[0..^(DECK.Length)], item.Value);
				remove.Add(item.Key);
			}
			foreach (var key in remove)
				LineItems.Remove(key);
		}

		#region Protected Methods
		/// <summary>
		/// Loads a definition from the list of <see cref="Imports"/>
		/// </summary>
		/// <param name="name">The name to the definition to import</param>
		/// <returns>The <see cref="AssignmentDefinition"/> requested</returns>
		protected virtual AssignmentDefinition LoadImport(String name)
		{
			if (DataAccess.DataAccess.Instance.LibraryExists(name))
				return (AssignmentDefinition)DataAccess.DataAccess.Instance.GetLibrary(name);
			else if (DataAccess.DataAccess.Instance.DefinitionExists(name))
				return (AssignmentDefinition)DataAccess.DataAccess.Instance.GetDefinition(name);
			else
				throw new DefinitionNotFoundException(name);
		}

		/// <summary>
		/// Loads all definitions in the <see cref="Imports"/> list
		/// </summary>
		protected virtual void LoadImports()
		{
			if (!_importComplete)
			{
				foreach (var import in Imports)
				{
					var definition = LoadImport(import);
					if (definition != null)
					{
						foreach (var item in definition.LineItems)
						{
							if (LineItems.ContainsKey(item.Key))
								LineItems[item.Key].AddRange(item.Value);
							else
								LineItems.Add(item.Key, item.Value);
						}
					}
				}
				_importComplete = true;
			}
		}

		/// <summary>
		/// Loops through the preprocess items
		/// </summary>
		protected virtual void PreProcess()
		{
			// Loop through the pre process items and calculate the values
			foreach (var item in PreProcessItems)
			{
				var value = new StringBuilder();
				var tokens = Tokenizer.Tokenize(item.Content);

				foreach (var token in tokens)
				{
					switch (token.TokenType)
					{
						case TokenTypes.Equation:
							value.Append(Calculate(token.Value));
							break;
						case TokenTypes.Item:
							var ex = new DefinitionException("Items are not supported in PreProcessItems");
							ex.AddData("Variable", item.Variable);
							ex.AddData("Content", item.Content);
							ex.AddData("Item", token.Value);
							throw ex;
						case TokenTypes.Text:
							value.Append(token.Value);
							break;
						case TokenTypes.Variable:
							if (Variables.ContainsKey(token.Value))
								value.Append(Variables[token.Value]);
							else if (Parameters.ContainsKey(token.Value))
								value.Append(Parameters[token.Value].Value);
							else
							{
								var vex = new DefinitionException("Unrecognized variable.");
								vex.AddData("Variable", item.Variable);
								throw vex;
							}
							break;
					}
				}

				Variables.Add(item.Variable, value.ToString());
			}
			_preprocessing = false;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Evaluates a <see cref="LineItem"/>
		/// </summary>
		/// <param name="item">The <see cref="LineItem"/> to evaluate</param>
		/// <returns>The result of the evaluation</returns>
		private string EvaluateLineItem(String name, LineItem item)
		{
			var result = new StringBuilder();
			UInt32 repeat;
			
			if (String.IsNullOrWhiteSpace(item.Repeat))
				repeat = 1;
			else
			{
				if (UInt32.TryParse(item.Repeat, out var number))
					repeat = number;
				else if (item.Repeat.StartsWith("=", StringComparison.CurrentCulture))
					repeat = UInt32.Parse(Calculate(item.Repeat[1..]), System.Globalization.CultureInfo.CurrentCulture.NumberFormat);
				else
					throw new InvalidPropertyValueException(nameof(item.Repeat), item.Repeat);
			}

			_recursionDepth++;
			_loopCount++;
			if (_recursionDepth > MAX_RECURSION_DEPTH) throw new MaxRecursionDepthExceededException(MAX_RECURSION_DEPTH);
			if (_loopCount > MAX_LOOP_COUNT) throw new MaxLoopCountException(MAX_LOOP_COUNT);

			for (var i = 1; i <= repeat; i++)
			{
				if (!String.IsNullOrWhiteSpace(item.Content))
				{
					var evaluated = EvaluateContent(item.Content);

					if (!String.IsNullOrWhiteSpace(item.Variable))
					{
						if (!Variables.ContainsKey(item.Variable.ToUpperInvariant()))
							Variables.Add(item.Variable.ToUpperInvariant(), String.Empty);
						Variables[item.Variable.ToUpperInvariant()] = evaluated;
					}
					else
					{
						result.Append(evaluated);
					}
				}
				if (!String.IsNullOrWhiteSpace(item.Next))
				{
					var value = item.Next;
					if (value.StartsWith("=", StringComparison.CurrentCultureIgnoreCase))
						value = Calculate(value[1..]);
					if (!String.IsNullOrWhiteSpace(value))
						result.Append(EvaluateLineItem(value, LineItems[value].SelectRandomItem()));
				}
			}
			_recursionDepth--;
			return result.ToString();
		}

		/// <summary>
		/// Processes <see cref="LineItem.Content"/>
		/// </summary>
		/// <param name="content">The content to process</param>
		/// <returns>The result of the evaluation</returns>
		private string EvaluateContent(String content)
		{
			var result = new StringBuilder();
			var tokens = Tokenizer.Tokenize(content);

			foreach (var token in tokens)
			{
				switch (token.TokenType)
				{
					case TokenTypes.Text:
						result.Append(token.Value);
						break;
					case TokenTypes.Variable:
						if (Variables.ContainsKey(token.Value))
							result.Append(Variables[token.Value]);
						else if (Parameters.ParameterExists(token.Value))
							result.Append(Parameters[token.Value].Value);
						break;
					case TokenTypes.Equation:
						result.Append(Calculate(token.Value));
						break;
					case TokenTypes.Item:
						var name = token.Value;

						var format = String.Empty;

						var formatParts = name.Split(":");
						if (formatParts.Length > 1)
						{
							name = formatParts[0];
							format = formatParts[1];
						}	

						// If there are more than on item names in the token, select one
						var or = name.Split("|");
						if (or.Length > 1)
							name = or[Utility.Random.RandomNumber(0, or.Length - 1)];

						// If there is a parameter with this name, get the value
						if (Parameters.ContainsKey(token.Value))
							name = Parameters[token.Value].Value.ToString();

						// Reevaluate to allow nested items
						name = EvaluateContent(name);
						var value = String.Empty;

						if (LineItems.ContainsKey(name))
							value = EvaluateLineItem(name, LineItems[name].SelectRandomItem());
						else if (name.Contains('+'))
						{
							var and = name.Split('+');
							var andLineItems = new LineItemList();

							foreach (var part in and)
							{
								andLineItems.AddRange(LineItems[part]);
							}
							value = EvaluateLineItem(name, andLineItems.SelectRandomItem());
						}
						else
							result.Append(name);
						switch (format.ToLowerInvariant())
						{
							case "ucase": value = value.UCase(); break;
							case "lcase": value = value.LCase(); break;
							case "tcase": value = value.TCase(); break;
							case "scase": value = value.SCase(); break;

						}
						result.Append(value);
						break;
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// Handles providing values for parameters requested by the <see cref="CalculationEngine"/>
		/// </summary>
		/// <param name="name">The name of the parameter</param>
		/// <param name="args">The result of the parameter request</param>
		protected override void EvaluateParameter(String name, ParameterArgs args)
		{
			if (Variables.ContainsKey(name))
			{
				args.Result = Variables[name];
			}
			else if (Parameters.ContainsKey(name))
			{
				args.Result = Parameters[name].Value;
			}
			else if (!_preprocessing && LineItems.ContainsKey(name))
			{
				args.Result = EvaluateLineItem(name, LineItems[name].SelectRandomItem());
			}
		}
		#endregion
	}
}
