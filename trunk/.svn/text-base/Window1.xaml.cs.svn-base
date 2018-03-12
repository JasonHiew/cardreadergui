using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardReaderGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public readonly DependencyProperty StatusProperty;

        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        public readonly DependencyProperty CardProperty;

        public ISmartCard Card
        {
            get { return (ISmartCard)GetValue(CardProperty); }
            set { SetValue(CardProperty, value); }
        }

        private CardNative iCard;
        private string reader;

        public Window1()
        {
            StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(Window1), new PropertyMetadata("Please insert card."));
            CardProperty = DependencyProperty.Register("Card", typeof(ISmartCard), typeof(Window1), new PropertyMetadata(null));
            InitializeComponent();
            InitCardReader();
        }

        private void InitCardReader()
        {
            iCard = new CardNative();

            string[] readers = new string[0];
            try
            {
                readers = iCard.ListReaders();
            }
            catch (Exception)
            {
                if (iCard.LastError == 0x8010001D)
                {
                    Status = "Smart card service not running.";
                    return;
                }
            }
            if (readers.Length == 0)
            {
                Status = "No card readers found.";
                return;
            }
            reader = readers[0];
            Status = reader;

            iCard.OnCardInserted += new CardInsertedEventHandler(iCard_OnCardInserted);
            iCard.OnCardRemoved += new CardRemovedEventHandler(iCard_OnCardRemoved);
            iCard.StartCardEvents(readers[0]);

            try
            {
                StartReadCard();
            }
            catch (Exception e)
            {
                //System.Diagnostics.Debug.Assert(false, e.Message + " " + e.StackTrace);
                if (iCard.LastError == 0x80100069)
                    Status = "No card inserted.";
                else
                {
                    Status = e.Message;
                    System.Diagnostics.Debug.Assert(false, e.Message + " "+ iCard.LastError+ " "+ e.StackTrace );
                }
                //no card present, do nothing
            }
        }

        private void StartReadCard()
        {
            iCard.Connect(reader, SHARE.Shared, PROTOCOL.T0orT1);
            System.Threading.Thread.Sleep(100);

            MyKad myKad = new MyKad(iCard);
            TouchNGoCard tngcard = new TouchNGoCard(iCard);
            EMVCard emvCard = new EMVCard(iCard);
            NETSCashCard netsCard = new NETSCashCard(iCard);

            //check if MyKad
            if (myKad.SelectApplication())
            {
                myKad.ReadCard();
                Card = myKad;
            }
            //check if its a touch n go card
            else if (tngcard.SelectApplication())
            {
                tngcard.ReadCard();
                Card = tngcard;
            }
            //check if EMV card
            else if (emvCard.SelectApplication())
            {
                emvCard.ReadCard();
                Card = emvCard;
            }
            //check if NETS cash card
            else if (netsCard.SelectApplication())
            {
                netsCard.ReadCard();
                Card = netsCard;
            }
            else
                Card = new UnknownCard();

            iCard.Disconnect(DISCONNECT.Unpower);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            iCard.StopCardEvents();
        }

        void iCard_OnCardRemoved()
        {
            this.Dispatcher.Invoke(new Action(delegate { Status = "Card removed."; }));
        }

        void iCard_OnCardInserted()
        {
            this.Dispatcher.Invoke(new Action(delegate { Status = "Card inserted."; StartReadCard(); }));
        }

        private void Props_Click(object sender, RoutedEventArgs e)
        {
            PropertyWindow propWindow = new PropertyWindow();
            propWindow.Properties = Card.Properties;
            propWindow.Show();
        }
    }

    public static class Command
    {
        public static readonly RoutedUICommand OpenProperties = new RoutedUICommand("Open Properties", "OpenProperties", typeof(Window1));
    } 

}
