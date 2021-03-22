using CommandClasses;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client;


        public bool IsX { get; set; }
        public char Symbol => IsX ? 'X' : 'O';
        public char OpponentSymbol => IsX ? 'O' : 'X';
        public string Nickname { get; set; }
        public string OpponentNickname { get; set; }
        public bool IsPlaying { get; set; }
        private bool isMoving = false;

        char[,] arr = new char[3, 3];
        public bool IsMoving
        {
            get { return isMoving; }
            set
            {
                isMoving = value;

                var brush = isMoving ? Brushes.White : Brushes.Gray;
                foreach (Border item in fieldGrid.Children.OfType<Border>())
                {
                    item.Background = brush;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            HelpArr();
            IsPlaying = false;
            IsMoving = false;
        }

        private bool IsWin(char Sign)
        {
            if (arr[0, 0] == Sign && arr[0, 1] == Sign && arr[0, 2] == Sign) return true;
            else if (arr[0, 0] == Sign && arr[1, 0] == Sign && arr[2, 0] == Sign) return true;
            else if (arr[0, 0] == Sign && arr[1, 1] == Sign && arr[2, 2] == Sign) return true;
            else if (arr[0, 2] == Sign && arr[1, 1] == Sign && arr[2, 0] == Sign) return true;
            else if (arr[0, 2] == Sign && arr[1, 2] == Sign && arr[2, 2] == Sign) return true;
            else if (arr[2, 2] == Sign && arr[2, 1] == Sign && arr[2, 0] == Sign) return true;
            else if (arr[0, 1] == Sign && arr[1, 1] == Sign && arr[2, 1] == Sign) return true;
            else if (arr[1, 0] == Sign && arr[1, 1] == Sign && arr[1, 2] == Sign) return true;
            else return false;
        }


        private void HelpArr()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    arr[i, j] = '-';
        }


        private Task SendCommand(ClientCommand command)
        {

            return Task.Run(() =>
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(client.GetStream(), command);
            });
        }

        private Task<ServerCommand> ReceiveCommand()
        {
            return Task.Run(() =>
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (ServerCommand)formatter.Deserialize(client.GetStream());
            });
        }

        private async void Listen()
        {
            try
            {
                bool isExit = false;
                while (!isExit)
                {

                    ServerCommand command = await ReceiveCommand();


                    switch (command.Type)
                    {

                        case CommandType.WAIT:

                            opponentNameTxtBox.Content = "Waiting...";
                            break;

                        case CommandType.START:

                            IsX = command.IsX;
                            IsMoving = IsX;
                            symbolLabel.Content = Symbol;
                            OpponentNickname = command.OpponentName;
                            opponentNameTxtBox.Content = OpponentNickname;
                            break;

                        case CommandType.MOVE:

                            foreach (Border item in fieldGrid.Children.OfType<Border>())
                            {

                                if (item.Tag.Equals(command.MoveCoord))
                                {

                                    ((TextBlock)item.Child).Text = OpponentSymbol.ToString();
                                    string str = ((Border)item).Tag.ToString();
                                    char[] wordsSplit = new char[] { ':', };
                                    string[] coords = str.Split(wordsSplit, StringSplitOptions.RemoveEmptyEntries);
                                    arr[int.Parse(coords[0]), int.Parse(coords[1])] = OpponentSymbol;

                                    if (IsWin(OpponentSymbol))
                                    {
                                        MessageBox.Show(OpponentNickname + " Win!");
                                        HelpArr();
                                        if (IsPlaying) await SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
                                    }
                                    else if (IsEndGame())
                                    {
                                        MessageBox.Show("End game");
                                        HelpArr();
                                        if (IsPlaying) await SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
                                    }
                                }

                                IsMoving = true;
                            }
                            break;

                        case CommandType.CLOSE:

                            CloseSession();

                            isExit = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (!IsPlaying)
                {
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
                    client = new TcpClient();

                    client.Connect(serverEndPoint);

                    Nickname = nameTxtBox.Text;

                    await SendCommand(new ClientCommand(CommandType.START, Nickname));

                    Listen();

                    IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

     
        private async void CloseSession()
        {
           
            await SendCommand(new ClientCommand(CommandType.CLOSE, Nickname));         
            client.Close();
            client = null;        
            IsPlaying = false;
            IsMoving = false;
            symbolLabel.Content = "-";
            opponentNameTxtBox.Content = "-";

            foreach (Border item in fieldGrid.Children.OfType<Border>())
            {
                ((TextBlock)item.Child).Text = string.Empty;
            }
        }

      
        async private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
          
            if (IsMoving)
            {
              
                if (((TextBlock)((Border)sender).Child).Text == "")
                {
                    ((TextBlock)((Border)sender).Child).Text = Symbol.ToString();

                    string str = ((Border)sender).Tag.ToString();
                    char[] wordsSplit = new char[] { ':', };
                    string[] coords = str.Split(wordsSplit, StringSplitOptions.RemoveEmptyEntries);
                    arr[int.Parse(coords[0]), int.Parse(coords[1])] = Symbol;

                    ClientCommand command = new ClientCommand(CommandType.MOVE, Nickname)
                    {
                        MoveCoord = (CellCoord)((Border)sender).Tag
                    };
                    await SendCommand(command);

                   
                    IsMoving = false;
                    if (IsWin(Symbol))
                    {
                        MessageBox.Show(Nickname + " Win!");
                        HelpArr();
                        if (IsPlaying) await SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
                    }
                    else if (IsEndGame())
                    {
                        MessageBox.Show("End game");
                        HelpArr();
                        if (IsPlaying) await SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
                    }
                }
            }
        }

        private bool IsEndGame()
        {
            foreach (Border item in fieldGrid.Children.OfType<Border>())
            {
                if (((TextBlock)item.Child).Text == string.Empty)
                    return false;
            }
            return true;
        }

    
        private void Window_Closed(object sender, EventArgs e)
        {     
            if (IsPlaying) SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
        }
    }
}