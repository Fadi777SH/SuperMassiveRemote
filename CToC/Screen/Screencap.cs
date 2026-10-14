using SMR.Server;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;


namespace SMR.Screen
{

    public class FrameCapture : IDisposable
    {
        int ScreenWidth = SystemInformation.VirtualScreen.Width;
        int ScreenHeight = SystemInformation.VirtualScreen.Height;

        private GraphicsCaptureItem _captureItem;
        public GraphicsCaptureSession _graphicsCaptureSession ;

        public Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
        public delegate void FramArrivedHandler(Direct3D11CaptureFramePool Framepool, GraphicsCaptureSession graphics);

        public bool IsStreaming
        {
            get
            {
                return StreamCurrentState;
            }
        }
        private bool StreamCurrentState = false;
        public void Dispose()
        {
            if (_direct3D11CaptureFramePool != null)
                _direct3D11CaptureFramePool.Dispose();
            if (_graphicsCaptureSession != null)
                _graphicsCaptureSession.Dispose();
            if (dev != null)
                dev.Dispose();

            StreamCurrentState = false;
        }

        public static IDirect3DDevice GetDirectdevice()
        {
            using (var sharpDxDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                     SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport))
            {
                var D = CaptureInterop.CreateDirect3DDeviceFromSharpDXDevice(sharpDxDevice);
                return D;
            }
        }

        DisplayId displayId = new();

        public static IDirect3DDevice dev;
        private DirectXPixelFormat Format = DirectXPixelFormat.B8G8R8A8UIntNormalized;



        public void Stream()
        {


            StreamCurrentState = !StreamCurrentState;
            if (StreamCurrentState == true)
            {
                try
                {
                    _captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);

                    dev = GetDirectdevice();
                    


                    _direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(dev, Format, 1, new(ScreenWidth, ScreenHeight));

                    _direct3D11CaptureFramePool.FrameArrived += UdpServer._direct3D11CaptureFramePool_FrameArrived;


                    _graphicsCaptureSession = _direct3D11CaptureFramePool.CreateCaptureSession(_captureItem);

                    _graphicsCaptureSession.IsBorderRequired = true;
                    _graphicsCaptureSession.StartCapture();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            var w = IsStreaming;

        }

        public void EndStream()
        {
            _direct3D11CaptureFramePool.FrameArrived -= UdpServer._direct3D11CaptureFramePool_FrameArrived;
            Dispose();
        }

    }
}

