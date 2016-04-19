namespace DataLink.Driver.S7
{
    public class IntByRef
    {
        public IntByRef(int Val)
        {
            this.Value = Val;
        }
        public IntByRef()
        {
            this.Value = 0;
        }
        public int Value;
    }
}
