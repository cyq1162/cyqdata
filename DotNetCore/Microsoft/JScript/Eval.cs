using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ClearScript.V8;
namespace Microsoft.JScript
{
    internal class Eval
    {
        static V8ScriptEngine v8; 
        public static object JScriptEvaluate(string code, object obj)
        {
            if (v8 == null)
            {
                v8 = new V8ScriptEngine();
            }
            return v8.Evaluate(code);
        }
    }
}
