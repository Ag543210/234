﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Randomizer.Generator.Assignment;
using Randomizer.Generator;
using System;
using System.Collections.Generic;
using Randomizer.Generator.Core;

namespace Randomizer.Generator.Test
{
    [TestClass]
    public class AssignmentTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

		[TestMethod]
		[TestCategory("Serialization")]
		public void AssignmentSerialize()
		{
			var generator = new AssignmentDefinition()
			{
				Version = new Version(1, 2, 3, 4),
				ShowInList = false
			};
			generator.LineItems.Add("Start", new LineItemList()
			{
				new LineItem() { Content = "Test", Next = "One" }
			});
			generator.LineItems.Add("One", new LineItemList()
			{
				new LineItem() { Content = "One Test", Weight = 10, Repeat = "2", Variable = "Var"},
				new LineItem() { Content = "Two Test", Weight = 20, Repeat = "2", Variable = "Var"}
			});
			generator.PreProcessItems.Add(new PreProcessItem() { Variable = "One", Content = "My Content" });
            generator.Parameters.Add("Gender",
                                     new Parameter()
                                     {
                                         Display = "Gender",
                                         Type = ParameterTypes.List,
                                         Value = "Any",
                                         Options = new ListOptionList()
                                         {
                                             { "Any", "Any" },
                                             { "Male", "Male" },
                                             { "Female", "Female" }
                                         }
                                     });
            TestContext.WriteLine(BaseDefinition.Serialize(generator));
        }

        [TestMethod]
		[TestCategory("Serialization")]
		public void AssignmentDeserialize()
        {
            var generator = BaseDefinition.Deserialize(Properties.Resources.AD_DeserializeTest_rgen);
            var value = generator.Generate();
            TestContext.WriteLine($"Value = {value}");
            Assert.AreNotEqual(String.Empty, value);
        }

        [TestMethod]
		[TestCategory("Assignment")]
        public void OneItemTest()
        {
            var generator = BaseDefinition.Deserialize(Properties.Resources.AD_OneItemTest_rgen);
            var value = generator.Generate();
            Assert.AreEqual("One item", value);
        }

        [TestMethod]
		[TestCategory("Assignment")]
		public void AdventureHookTest()
        {
            var generator = BaseDefinition.Deserialize(Properties.Resources.Adventure_Hooks_Fantasy_rgen);
            var value = generator.Generate();
            TestContext.WriteLine(value);
            Assert.AreNotEqual(String.Empty, value);
        }

        [TestMethod]
		[TestCategory("Assignment")]
		public void TokenizeTest()
        {
            var tokens = Tokenizer.Tokenize("This [is] a [@string][=1+1]!");
            var parsed = String.Empty;

            foreach (var token in tokens)
            {
                parsed += token.Value;
                TestContext.WriteLine(token.ToString());
            }
            TestContext.WriteLine(parsed);
            Assert.AreEqual(6, tokens.Count);
            Assert.AreEqual(TokenTypes.Text, tokens[0].TokenType);
            Assert.AreEqual(TokenTypes.Item, tokens[1].TokenType);
            Assert.AreEqual(TokenTypes.Text, tokens[2].TokenType);
            Assert.AreEqual(TokenTypes.Variable, tokens[3].TokenType);
            Assert.AreEqual(TokenTypes.Equation, tokens[4].TokenType);
            Assert.AreEqual(TokenTypes.Text, tokens[5].TokenType);

            Assert.AreEqual("is", tokens[1].Value);
            Assert.AreEqual("string", tokens[3].Value);
            Assert.AreEqual("1+1", tokens[4].Value);

            StringAssert.EndsWith(tokens[0].Value, " ");
            StringAssert.StartsWith(tokens[2].Value, " ");
            
        }
    }
}
