﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Generator.Exceptions
{
	public class RandomizerGeneratorException : Exception
	{
		public RandomizerGeneratorException() : base() { }
		public RandomizerGeneratorException(String message) : base(message) { }
	}
}
