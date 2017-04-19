using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace ChatBotClient
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            return System.Convert.ToDouble(value) -
                   System.Convert.ToDouble(parameter);
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        TTransport transport; // Thrift default transport class
        TProtocol protocol; // Thrift default protocol class
        ChatBotSvc.Client client; // Thrift SVHN service interface
        string server_ip = "127.0.0.1";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DoSend()
        {
            if (txtMessage.Text.Trim() == "")
                return;

            var messages = (Messages)Resources["messages"];
            var my_message = txtMessage.Text.Trim();
            messages.Add(new Message(SENDER.ME, my_message));
            txtMessage.Clear();
            lbMessages.ScrollIntoView(lbMessages.Items[lbMessages.Items.Count - 1]);

            // 챗봇 백엔드에 메시지 전송
            string replied_message = "TO  DO : IMPLEMENTATION.";

            try
            {
                replied_message = client.send(new ChatMessage() {  Message = my_message });
            }
            catch (InvalidOperation io)
            {
                Console.WriteLine("Invalid operation: " + io.Why);
            }
            
            // 챗봇 백엔드로부터 받은 응답 추가.
            var reply = new Message(SENDER.YOU, replied_message);
            messages.Add(reply);
            lbMessages.ScrollIntoView(lbMessages.Items[lbMessages.Items.Count - 1]);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            DoSend();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if ( e.Key == Key.Enter)
            {
                DoSend();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                transport = new TSocket(server_ip, 5425);
                protocol = new TBinaryProtocol(transport);
                client = new ChatBotSvc.Client(protocol);
                
                transport.Open();
            }
            catch (TApplicationException x)
            {
                Console.WriteLine(x.StackTrace);
            }

            txtMessage.Focus();
        }
    }

    public enum SENDER
    {
        ME,
        YOU
    }

    public class Message
    {
        public SENDER Sender { get; set; }
        public string Text { get; set; }
        
        public Message(SENDER sender, string text)
        {
            this.Sender = sender;
            this.Text = text;
        }
    }

    public class Messages: ObservableCollection<Message>
    {
        public Messages()
        {
            //Add(new Message(SENDER.ME, "Hey"));
            //Add(new Message(SENDER.YOU, "What's up"));
            //Add(new Message(SENDER.ME, "Nothing"));
            //Add(new Message(SENDER.YOU, "OK"));
        }
    }
}
