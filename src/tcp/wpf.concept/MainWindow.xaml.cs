using System;
using System.Windows;

using cc.isr.Iot.Tcp.Session.Helper;

namespace cc.isr.Iot.Tcp.Session.Wpf.Concept
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int _count = 0;

        private void OnCounterClicked( object? sender, EventArgs e )
        {
            this._count++;

            this.CounterBtn.Content = $"Clicked {this._count} time{(this._count == 1 ? string.Empty : 's')}";

            InstrumentId instrumentId = InstrumentId.K7510;
            Random rnd = new Random( DateTime.Now.Second );
            if ( rnd.NextDouble() > 0.5 )
                this.WelcomeLabel.Content = SessionManager.QueryIdentityAsync( instrumentId, TimeSpan.FromMilliseconds( 10 ) );
            else
                this.WelcomeLabel.Content = SessionManager.QueryIdentityAsync( instrumentId, TimeSpan.FromMilliseconds( 10 ) );

            this.InstrumentLabel.Content = $"{this._count} {SessionManager.QueryInfo}";
        }

    }
}
