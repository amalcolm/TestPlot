
namespace Plotter
{
    internal class MySerialPlotter : MyPlotter
    {
        public MySerialIO IO
        {
            get => _io;
            set
            {
                if (_io == value) return;
                if (_io != null ) _io.TextReceived -= IO_TextReceived;
                _io = value;
                if (_io != null ) _io.TextReceived += IO_TextReceived;
            }
        }

        private void IO_TextReceived(MySerialIO io, string text)
        {

            var data = MyTextParser.Parse(text);
        }



        protected override void Init()
        {
            base.Init();
        }
        protected override void ShutDown()
        {
            base.ShutDown();
        }


        private MySerialIO _io = default!;
    }
}
