using K4os.Compression.LZ4;
using SMR.Server;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.System;
using Point = System.Windows.Point;

namespace SMR
{


    public partial class MainWindow : System.Windows.Window
    {
        UdpServer Server = new();
        public delegate void MouseChangeHandler(System.Windows.Point point);

        public static event MouseChangeHandler? MouseChange;

        public DispatcherQueue _dispatcherQueue;

        public delegate void KeyPressHandler(System.Windows.Input.Key key, bool e);
        public static event KeyPressHandler? KeyPressEvent;
        public delegate void MousePressHandler(System.Windows.Input.MouseButton mouseButton, MouseButtonState state);
        public static event MousePressHandler? MousePressEvent;
        public delegate void MouseWheelHandler(int delta);
        public static event MouseWheelHandler? MouseWheelevent;

        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;


            this.ThisPCIPAddress = GetIPAddress().ToString();



            UdpServer.FrameArrived += DisplayFrame;

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Server?.disconnectAccepter();
            Server?.disconnectClient();

        }
        #region FrameDisplay
        private WriteableBitmap _wb;

        private async void DisplayFrame(byte[] bytes, int Width, int Height)
        {
            try
            {

                var decompressbyte = new byte[Width * Height * 4];
                var delta = LZ4Codec.Decode(bytes, 0, bytes.Length, decompressbyte, 0, decompressbyte.Length);


                this.Dispatcher.Invoke(() =>
                {

                    if (_wb == null)
                    {
                        _wb = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr32, null);

                        this.Frames.Source = _wb;
                    }

                    _wb.WritePixels(new Int32Rect(0, 0, Width, Height), decompressbyte, Width * 4, 0);


                });

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


        }


        #endregion

        #region Ip Adresses

        public string _IPAddress
        {
            get
            {
                return ThisPCIPAddress;
            }
        }
        private string ThisPCIPAddress { get; set; }

        private IPAddress GetIPAddress()
        {
            var dns = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var address in dns.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            return IPAddress.Any;
        }

        private void changingpassword_PasswordOfSender(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.IPofRemote.Text.Length > 15)
                IPofRemote.Text = IPofRemote.Text.Substring(0, 4);

        }

        private void changingpassword_PasswordOfController(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.IPOfWantedDevice.Text.Length > 15)
                IPOfWantedDevice.Text = IPOfWantedDevice.Text.Substring(0, 4);
        }
        #endregion
        #region clients
        private void ConnectToRemoteBtn(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;

            try
            {
                IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
                IPAddress ipofpc2 = IPAddress.Parse(this.IPofRemote.Text);



                if (btn != null && btn.IsChecked == true)
                {
                    if (PC1ToPC2.IsChecked == false && (ipofpc1.ToString() != ipofpc2.ToString()))
                    {
                        Server?.Reciever(ipofpc1, ipofpc2);


                    }
                    else
                    {
                        if (ipofpc1.ToString() == ipofpc2.ToString())
                        {
                            System.Windows.MessageBox.Show("you can't connect to the same IP of your computer");
                        }
                        if (PC1ToPC2.IsChecked == true)
                        {
                            System.Windows.MessageBox.Show("you can't be a reciver and a remote at the same time");
                        }
                        btn.IsChecked = false;
                    }
                }
                else if (btn != null && btn.IsChecked == false)
                {
                    Server?.disconnectClient();
                    System.Windows.MessageBox.Show("you disconnect as a remote");
                }
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid IP type . \n {ex.Message}");
                btn?.IsChecked = false;
            }
        }

        private void ConnectToWantedDeviceBtn(object sender, RoutedEventArgs e)
        {

            var btn = sender as ToggleButton;
            try
            {
                IPAddress ipofpc1 = IPAddress.Parse(_IPAddress);
                IPAddress ipofpc2 = IPAddress.Parse(this.IPOfWantedDevice.Text);

                if (btn?.IsChecked == true)
                {
                    if (PC2ToPC1?.IsChecked == false && (ipofpc1.ToString() != ipofpc2.ToString()))
                    {
                        Server?.Sender(ipofpc1, ipofpc2);

                    }
                    else
                    {
                        if (ipofpc1.ToString() == ipofpc2.ToString())
                        {
                            System.Windows.MessageBox.Show("you can't connect to the same IP of your computer");
                        }
                        if (PC2ToPC1?.IsChecked == true)
                        {
                            System.Windows.MessageBox.Show("you can't be a reciver and a remote at the same time");
                        }

                        btn.IsChecked = false;
                    }

                }
                else if (btn != null && btn.IsChecked == false)
                {
                    Server?.disconnectAccepter();
                    System.Windows.MessageBox.Show("you disconnect as a remote");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid IP type .\n {ex.Message}");
                btn?.IsChecked = false;
            }
        }
        #endregion

        #region keyboard and mouse hooks
        private void PC2Fram_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

            Point portionInFram = PortionOfCursorInPC2Fram();
            MouseChange?.Invoke(portionInFram);


        }

        private Point PortionOfCursorInPC2Fram()
        {
            var Mousepos = Mouse.MousePosition.GetCursorPosition();

            //the point in the fram 
            var point = this.PC2Fram.PointFromScreen(Mousepos);



            var Width = this.PC2Fram.ActualWidth;
            var height = this.PC2Fram.ActualHeight;

            var PercentofXfarFromTheABS = ((point.X) / Width) * 100;
            var PercentofYfarFromTheABS = ((point.Y) / height) * 100;

            return new(PercentofXfarFromTheABS, PercentofYfarFromTheABS);
        }
        private void PC2Fram_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            MouseWheelevent?.Invoke(e.Delta);

        }

        private void PC2Fram_MouseUp(object sender, MouseButtonEventArgs e)
        {

            MousePressEvent?.Invoke(e.ChangedButton, e.ButtonState);

        }

        private void PC2Fram_MouseDown(object sender, MouseButtonEventArgs e)
        {

            MousePressEvent?.Invoke(e.ChangedButton, e.ButtonState);


        }

        private void IPofRemote_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IPofRemote.Text.Length > 15)
                IPofRemote.Text = IPofRemote.Text.Substring(0, 15);
        }

        private void IPOfWantedDevice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IPOfWantedDevice.Text.Length > 15)
                IPOfWantedDevice.Text = IPOfWantedDevice.Text.Substring(0, 15);
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (IPofRemote.IsMouseOver)
                {
                    IPofRemote.Focusable = true;
                }
                else
                {
                    IPofRemote.Focusable = false;
                }
                if (IPOfWantedDevice.IsMouseOver)
                {
                    IPOfWantedDevice.Focusable = true;
                }
                else
                {
                    IPOfWantedDevice.Focusable = false;
                }
            }
        }
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEvent?.Invoke(e.Key, e.IsDown);
        }
        #endregion


    }
}