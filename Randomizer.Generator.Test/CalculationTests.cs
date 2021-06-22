﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Generator.Test
{
	[TestClass]
	public class CalculationTests
	{
		[TestMethod]
		public void TestCustomFunction()
		{
			var expression = "Roll(6)";
			var engine = new Utility.CalculationEngine(expression);
			var result = engine.Evaluate<Int32>();

			Assertions.IsBetween(1, 6, result);
		}
	}
}