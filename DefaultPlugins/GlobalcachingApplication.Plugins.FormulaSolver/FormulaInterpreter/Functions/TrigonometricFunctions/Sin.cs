﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobalcachingApplication.Plugins.FormulaSolver.FormulaInterpreter.Functions.TrigonometricFunctions
{
    public class Sin: TrigonometricFunction
    {
        protected override object ExecuteTrigFunction(double arg)
        {
            return Math.Sin(arg);
        }
    }
}
