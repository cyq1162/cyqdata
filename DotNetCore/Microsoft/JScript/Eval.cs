
namespace Microsoft.JScript
{
    internal class Eval
    {
        //static Microsoft.ClearScript.V8.V8ScriptEngine v8; 
        public static object JScriptEvaluate(string code, object obj)
        {
            return Z.Expressions.Eval.Execute(code);
            //if (v8 == null)
            //{
            //    v8 = new Microsoft.ClearScript.V8.V8ScriptEngine();
            //}
            //return v8.Evaluate(code);
        }
    }
}
