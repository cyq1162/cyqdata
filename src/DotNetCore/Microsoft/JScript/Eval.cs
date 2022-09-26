
using CYQ.Data;
using System.Linq.Expressions;

namespace Microsoft.JScript
{
    internal class Eval
    {
        static DynamicExpresso.Interpreter express = new DynamicExpresso.Interpreter();
        public static object JScriptEvaluate(string code, object obj)
        {
            //'${type}'=='file'?'file':'text' 修正单引号 "${type}"=="file"?"文件":("${type}"=="header"?"请求头":"${type}")
            if (code.Contains("'=='") || code.Contains("'?'") || code.Contains("':'"))
            {
                code = code.Replace("'", "\"");
            }
            if (code.Contains("True") || code.Contains("False"))
            {
                code = code.Replace("True?", "true?").Replace("False?", "false?").Replace("=True", "=true").Replace("=False", "=false");
            }
            return express.Eval(code);
            //if (v8 == null)
            //{
            //    v8 = new Microsoft.ClearScript.V8.V8ScriptEngine();
            //}
            //return v8.Evaluate(code);
        }
    }
}
