﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Generator.Phonotactics
{
    class Token
    {
        #region Properties
        public TokenTypes TokenType { get; set; }
        public Char Value { get; set; }
        #endregion

        #region Public Methods
        public override String ToString()
        {
            return $"{TokenType}: {Value}";
        }
        #endregion
    }
}
