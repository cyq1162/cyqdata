namespace System.Windows.Forms
{
    internal class Control
    {
        public Control.ControlCollection Controls { get; }

        public class ControlCollection
        {
            internal Control[] Find(string v1, bool v2)
            {
                throw new NotImplementedException();
            }
        }
    }
}