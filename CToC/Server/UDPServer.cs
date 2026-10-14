using FFMpegCore.Arguments;
using K4os.Compression.LZ4;
using Microsoft.Graphics.Canvas;
using SMR.Mouse;
using SMR.Screen;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WindowsInput;
using Point = System.Windows.Point;

namespace SMR.Server
{
    public class UdpServer
    {
        #region headers
        public Socket? Accept;
        InputSimulator? inputsime;
        public Socket? Client;
        int PORT = 3800;

        public static event Action? PC2DisConnect;
        private bool ServerShotDown = false;

        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;

        EndPoint ClientendPoint = new IPEndPoint(IPAddress.Any, 3800);



        public delegate void ShowPic(byte[] bytes, int W, int H);
        public static event ShowPic? FrameArrived;
        public delegate void SentFrameToPC2Handlerbit(byte[] bytes, int W, int H);
        public static event SentFrameToPC2Handlerbit? SentFrameToPC2bytes;

        public delegate void ProcessRecieve(bool KeepRecieve);
        public static event ProcessRecieve? KeepReceiveFromUser;

        public static event ProcessRecieve? KeepReceiveFromRemote;
        public FrameCapture frameCapture = new();
        #endregion
        private bool ContinueSend = true;
        private bool ContinueReceive = true;
        private const int _SingleFrameChunk = 64000;

        public UdpServer(TypeOfConnection typeOfConnection)
        {
            var CurrentComputerIP = GetIPAddress();
            var CurrentComputerEndPoint = new IPEndPoint(CurrentComputerIP, PORT);

            if (typeOfConnection == TypeOfConnection.Sender)
            {
                Accept = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ExclusiveAddressUse = false, SendBufferSize = 300 };
                Accept?.Bind(CurrentComputerEndPoint);

            }

            if (typeOfConnection == TypeOfConnection.Remote)
            {
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ExclusiveAddressUse = false };

                Client?.Bind(CurrentComputerEndPoint);
            }
        }

        public static IPAddress GetIPAddress()
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


        public async Task Sender( IPAddress IPAddressOfPC2)
        {
            if (Accept == null) return;
            if (Accept.Connected == true) return;

            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);


            ContinueSend = true;
            ContinueReceive = true;

            try
            {
                await Accept.ConnectAsync(ClientendPoint);
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

            if (Accept.Connected == false)
            {
                System.Windows.MessageBox.Show("You are not connected to the IP address you picked\ntry reconnect to other IP address");
            }
            else
            {
                StartSendHooks();

                KeepReceiveFromRemote += StartReceiveFromRemote;

                KeepReceiveFromRemote?.Invoke(ContinueReceive);
            }
        }

        private async void StartSendHooks()
        {

            try
            {
                if (ContinueSend)
                {
                    MainWindow.KeyPressEvent += PressThisKey;
                    MainWindow.MouseChange += MouseChangepos;
                    MainWindow.MousePressEvent += MousePressedDown;
                    MainWindow.MouseWheelevent += MainWindow_MouseWheelevent;

                }

            }
            catch (Exception ex)
            {

                System.Windows.MessageBox.Show(ex.Message);

               

            }

        }

        private void StartReceiveFromHooksUser(bool State)
        {
            inputsime = new();


            if (ContinueReceive && Client != null)

                new Task(async () =>
                {

                    while (ContinueReceive)
                    {



                        byte[] RecievedByte = new byte[255];
                        try
                        {

                            var size = await Client.ReceiveAsync(RecievedByte);

                            Array.Resize(ref RecievedByte, size);

                            UDPMessage MSG = FromByteArrayToUDPMessage(RecievedByte);
                            if (MSG.type == MessageType.Keyboard)
                            {
                                int vk = KeyInterop.VirtualKeyFromKey(MSG.key);
                                var key = (WindowsInput.VirtualKeyCode)vk;
                                if (MSG.IsKeyDown)
                                    inputsime.Keyboard.KeyDown(key);
                                else
                                    inputsime.Keyboard.KeyUp(key);
                            }


                            else if (MSG.type == MessageType.MouseWheelChange)
                            {
                                int Clicks = MSG.MouseWheelDelta / 120;
                                inputsime.Mouse.VerticalScroll(Clicks);
                            }


                            else if (MSG.type == MessageType.Mousepoint)
                            {
                                var P = GetPC2MousePos(MSG.Mousepoint);
                                MousePosition.SetCursorPos((int)P.X, (int)P.Y);
                            }

                            else if (MSG.type == MessageType.MouseChange)
                            {
                                if (MSG.mousestate == MouseButtonState.Pressed)
                                {
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                                    {
                                        inputsime.Mouse.LeftButtonDown();

                                    }
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                                    {
                                        inputsime.Mouse.RightButtonDown();
                                    }
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Middle)
                                    {
                                        inputsime.Mouse.MiddleButtonDown();
                                    }
                                }

                                else if (MSG.mousestate == MouseButtonState.Released)
                                {
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Left)
                                    {
                                        inputsime.Mouse.LeftButtonUp();

                                    }
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Right)
                                    {
                                        inputsime.Mouse.RightButtonUp();
                                    }
                                    if (MSG.MouseSide == System.Windows.Input.MouseButton.Middle)
                                    {
                                        inputsime.Mouse.MiddleButtonUp();
                                    }
                                }
                            }
                            else if (MSG.type == MessageType.Error)
                            {
                                if (MSG.ErrorMessageType == _UDPErrorMessageTypes.ClientExit)
                                    System.Windows.MessageBox.Show("there been an exist or a disconnect from the client side ");

                            }



                        }
                        catch (SocketException)
                        {
                            // Exist from the await line .

                        }
                        catch (Exception ex)
                        {

                            System.Windows.MessageBox.Show(ex.Message);
                        }

                    }
                }).Start();

        }
        private async void StartReceiveFromRemote(bool state)
        {

            if (state == true && Accept != null)

                new Task(async () =>
                {

                    MemoryStream Concate = new();
                    int currentfingerprint = 0;
                    int _singleFrameChunkConst = 64000;
                    int lastframe = 0;
                    bool concatefirst = false;

                    while (ContinueReceive)
                    {
                        var buf = new byte[65000];

                        if (ClientendPoint != null)
                        {
                            try
                            {

                                var size = await Accept.ReceiveAsync(buf);
                                Array.Resize(ref buf, size);

                                var UDPMSG = FromByteArrayToUDPFrameMessage(buf);
                                if (UDPMSG.type == MessageType.Error)
                                {
                                    if (UDPMSG.ErrorMessageType == _UDPErrorMessageTypes.ClientExit)
                                    {
                                        System.Windows.MessageBox.Show("there been an exist or a disconnect from the client side ");
                                    }
                                }
                                if (currentfingerprint != UDPMSG.FrameFingerPrint)
                                {
                                    currentfingerprint = UDPMSG.FrameFingerPrint;

                                    Concate = new(UDPMSG.TotalSizeOfTheFrame);
                                    concatefirst = false;

                                    lastframe = UDPMSG.CurrentChunkNumber;
                                }
                                if (currentfingerprint == UDPMSG.FrameFingerPrint)
                                {
                                    if (UDPMSG.CurrentChunkNumber == 1)
                                    {
                                        concatefirst = true;
                                        Concate.Write(UDPMSG.ChunkByteArray, 0, _singleFrameChunkConst);

                                    }
                                    if (concatefirst == true && lastframe == UDPMSG.CurrentChunkNumber - 1)
                                    {
                                        if (UDPMSG.CurrentChunkNumber < UDPMSG.totalChunks)
                                        {
                                            Concate.Write(UDPMSG.ChunkByteArray, 0, _singleFrameChunkConst);
                                        }

                                        //last chunk
                                        if (UDPMSG.CurrentChunkNumber == UDPMSG.totalChunks)
                                        {
                                            Array.Resize(ref UDPMSG.ChunkByteArray, UDPMSG.ShouldResizeTo);

                                            Concate.Write(UDPMSG.ChunkByteArray, 0, UDPMSG.ChunkByteArray.Length);

                                            var w = Concate.ToArray().Length;
                                            if (UDPMSG.TotalSizeOfTheFrame == Concate.ToArray().Length)
                                            {

                                                FrameArrived?.Invoke(Concate.ToArray(), UDPMSG.FrameWidth, UDPMSG.FrameHeight);
                                            }

                                            concatefirst = false;
                                        }

                                        lastframe = UDPMSG.CurrentChunkNumber;
                                    }
                                }


                            }
                            catch (SocketException)
                            {
                                // Exist from the await line .

                            }
                            catch (Exception ex)
                            {

                                System.Windows.MessageBox.Show(ex.Message);
                            }
                        }
                    }
                })
                {

                }.Start();
        }

        public async Task Reciever(IPAddress IPAddressOfPC2)
        {
            stopwatch.Start();
            if (Client == null) return;
            if (Client.Connected == true) return;

            ClientendPoint = new IPEndPoint(IPAddressOfPC2, PORT);

            ContinueSend = true;
            ContinueReceive = true;

            try
            {
                await Client.ConnectAsync(ClientendPoint);
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


            if (Client.Connected == false)
            {
                System.Windows.MessageBox.Show("You are not connected to the IP address you picked\ntry reconnect to other IP address");
            }
            else
            {
                frameCapture.Stream();

                if (ContinueSend)
                {
                    SentFrameToPC2bytes += UdpServer_SentFrameToPC2bytes;
                }


                KeepReceiveFromUser += StartReceiveFromHooksUser;

                KeepReceiveFromUser?.Invoke(ContinueReceive);
            }
        }


        #region convertAndMessages


        static byte[] getBytesOfUDPMessage<T>(T str)
        {
            int size = Marshal.SizeOf(str);

            byte[] arr = new byte[65000];

            Array.Resize(ref arr, size);

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(str, ptr, false);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }
        UDPMessage FromByteArrayToUDPMessage(byte[] arr)
        {
            UDPMessage str = new UDPMessage();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);
                str = (UDPMessage)Marshal.PtrToStructure(ptr, str.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }
        UDPframeMessage FromByteArrayToUDPFrameMessage(byte[] arr)
        {
            UDPframeMessage str = new UDPframeMessage();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                str = (UDPframeMessage)Marshal.PtrToStructure(ptr, str.GetType());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }
        #endregion



        #region FrameCaptureRegion


        Random random = new();
        private async void UdpServer_SentFrameToPC2bytes(byte[] _MessageByte, int W, int H)
        {

            var subBuffers = _MessageByte.Chunk(_SingleFrameChunk);
            var totalchunks = subBuffers.Count();

            //if (totalchunks >= 30) return;
            var count = 0;
            var RandomFingerPrint = random.Next(0, 1000);
            var totalsize = _MessageByte.Length;
            foreach (var buf in subBuffers)
            {
                count++;

                var MSG = new UDPframeMessage(totalchunks, count, buf, buf.Length, RandomFingerPrint, totalsize, W, H) { type = MessageType.Fram };

                var UDPMSG = getBytesOfUDPMessage(MSG);


                Client?.SendAsync(UDPMSG);

            }


            FrameCounter++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                Debug.WriteLine($"{FrameCounter} FPS");
                
                stopwatch.Restart();
                FrameCounter = 0;
            }

        }

        private static CanvasDevice _canvasDevice => CanvasDevice.CreateFromDirect3D11Device(FrameCapture.dev);
        private static Stopwatch stopwatch=new();
        private static int FrameCounter=0;
        public static async void _direct3D11CaptureFramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {

            
            try
            {
                
                using var frame = sender.TryGetNextFrame();

                if (frame != null)
                {

                    var W = frame.Surface.Description.Width;
                    var H = frame.Surface.Description.Height;

                    var bytes = LZ4Compression(frame.Surface);

                    SentFrameToPC2bytes?.Invoke(bytes, W, H);

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private static byte[] LZ4Compression(IDirect3DSurface surface)
        {
          

            var _renderTargetBitmap = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, surface);
            
            byte[] raw = _renderTargetBitmap.GetPixelBytes();
            
            var compressed = new byte[LZ4Codec.MaximumOutputSize(raw.Length)];
            var co = compressed;
            var w= LZ4Codec.MaximumOutputSize(raw.Length);
            int size = LZ4Codec.Encode(raw, 0, raw.Length, compressed, 0, compressed.Length, LZ4Level.L00_FAST);
            
            
            Array.Resize(ref compressed, size);



            return compressed;
        }

        #endregion
        #region ConnectionRegion
        UDPMessage MSG = new();
        public async Task disconnectClient()
        {
            if (frameCapture.IsStreaming == true)
            {
                frameCapture.EndStream();

            }
            if (Client != null && frameCapture.IsStreaming==false && Client.Connected==true)
            {
                SentFrameToPC2bytes -= UdpServer_SentFrameToPC2bytes;
                KeepReceiveFromUser -= StartReceiveFromHooksUser;

                ContinueReceive = false;
                ContinueSend = false;
                var MSG = new UDPframeMessage() { ErrorMessageType = _UDPErrorMessageTypes.ClientExit, type = MessageType.Error };

                var bytes = getBytesOfUDPMessage(MSG);


                await Client.SendAsync(bytes);

                try
                {

                    Client.Close();
                }catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }

        }
        public async Task disconnectAcceptor()
        {
            if (Accept != null&&Accept.Connected==true)
            {
                ContinueSend = false;
                ContinueReceive = false;

                MSG = new UDPMessage() { ErrorMessageType = _UDPErrorMessageTypes.ClientExit, type = MessageType.Error };
                var bytes = getBytesOfUDPMessage(MSG);
                await Accept.SendAsync(bytes);


                KeepReceiveFromRemote -= StartReceiveFromRemote;
                try
                {
                    Accept.Close();
                }catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
            MainWindow.KeyPressEvent -= PressThisKey;
            MainWindow.MouseChange -= MouseChangepos;
            MainWindow.MousePressEvent -= MousePressedDown;
            MainWindow.MouseWheelevent -= MainWindow_MouseWheelevent;
        }
        #endregion
        #region SendEventsRegion
        private Point GetPC2MousePos(Point portion)
        {
            var Xpoint = (portion.X / 100) * ScreenWidth;
            var Ypoint = (portion.Y / 100) * ScreenHeight;
            return new(Xpoint, Ypoint);
        }
        public async void PressThisKey(Key key, bool e)
        {

            //skip pressing the Lwin and Rwin button
            if (Key.LWin == key || key == Key.RWin) return;

            MSG = new();
            MSG.type = MessageType.Keyboard;
            MSG.key = key;
            MSG.IsKeyDown = e;

            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
                await Accept.SendAsync(bytes);
        }
        public async void MouseChangepos(System.Windows.Point portion)
        {

            MSG = new();


            MSG.type = MessageType.Mousepoint;


            MSG.Mousepoint = portion;

            byte[] bytes = getBytesOfUDPMessage(MSG);
            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendAsync(bytes);

            }

        }
        public async void MousePressedDown(System.Windows.Input.MouseButton mouseside, MouseButtonState state)
        {

            MSG = new();


            MSG.type = MessageType.MouseChange;
            MSG.MouseSide = mouseside;
            MSG.mousestate = state;

            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendAsync(bytes);
            }
        }
        private async void MainWindow_MouseWheelevent(int delta)
        {
            MSG = new();


            MSG.type = MessageType.MouseWheelChange;
            MSG.MouseWheelDelta = delta;

            byte[] bytes = getBytesOfUDPMessage(MSG);


            if (Accept != null && ServerShotDown == false)
            {
                await Accept.SendAsync(bytes);
            }
        }
        #endregion
    }

}