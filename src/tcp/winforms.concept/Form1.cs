using cc.isr.Iot.Tcp.Session.Helper;

namespace cc.isr.Iot.Tcp.Session.WinForms.Concept
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
            this.CounterBtn.Click += this.OnCounterClicked;
        }

        private int _count = 0;

        private void OnCounterClicked( object? sender, EventArgs e )
        {
            this._count++;

            this.CounterBtn.Text = $"Clicked {this._count} time{(this._count == 1 ? string.Empty : 's')}";

            InstrumentId instrumentId = InstrumentId.K7510;
            Random rnd = new ( DateTime.Now.Second );
            this.WelcomeLabel.Text = rnd.NextDouble() > 0.5
                ? SessionManager.QueryIdentityAsync( instrumentId, TimeSpan.FromMilliseconds( 10 ) )
                : SessionManager.QueryIdentityAsync( instrumentId, TimeSpan.FromMilliseconds( 10 ) );

            this.InstrumentLabel.Text = $"{this._count} {SessionManager.QueryInfo}";
        }

    }
}
