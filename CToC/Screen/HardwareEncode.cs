using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Transcoding;

namespace SuperMassiveRemote.Screen
{
    public unsafe class HardwareEncode
    {
        private readonly Size _frameSize;
        private readonly int _linesizeU;
        private readonly int _linesizeV;
        private readonly int _linesizeY;
        private readonly AVCodec* _pCodec;
        private readonly AVCodecContext* _pCodecContext;
        private readonly Stream _stream;
        private readonly int _uSize;
        private readonly int _ySize;
        public HardwareEncode(Stream stream, int fps, Size frameSize)
        {
            _stream = stream;
            _frameSize = frameSize;

            var codecId = AVCodecID.AV_CODEC_ID_H264;
            _pCodec = ffmpeg.avcodec_find_encoder(codecId);

            if (_pCodec == null) throw new InvalidOperationException("Codec not found.");

            _pCodecContext = ffmpeg.avcodec_alloc_context3(_pCodec);
            _pCodecContext->width = frameSize.Width;
            _pCodecContext->height = frameSize.Height;
            _pCodecContext->time_base = new AVRational { num = 1, den = fps };
            _pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

            // The elementary stream carries no timestamps, so whoever wraps it into a container
            // can only number the packets as they come. B-frames would make decode order differ
            // from presentation order, and that difference would be lost.
            _pCodecContext->max_b_frames = 0;

            ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "veryslow", 0);

            
           

            _linesizeY = frameSize.Width;
            _linesizeU = frameSize.Width / 2;
            _linesizeV = frameSize.Width / 2;

            _ySize = _linesizeY * frameSize.Height;
            _uSize = _linesizeU * frameSize.Height / 2;
        }

        public void Encode(AVFrame frame)
        {
            if (frame.format != (int)_pCodecContext->pix_fmt)
                throw new ArgumentException("Invalid pixel format.", nameof(frame));
            if (frame.width != _frameSize.Width) throw new ArgumentException("Invalid width.", nameof(frame));
            if (frame.height != _frameSize.Height) throw new ArgumentException("Invalid height.", nameof(frame));
            if (frame.linesize[0] < _linesizeY) throw new ArgumentException("Invalid Y linesize.", nameof(frame));
            if (frame.linesize[1] < _linesizeU) throw new ArgumentException("Invalid U linesize.", nameof(frame));
            if (frame.linesize[2] < _linesizeV) throw new ArgumentException("Invalid V linesize.", nameof(frame));
            if (frame.data[1] - frame.data[0] < _ySize)
                throw new ArgumentException("Invalid Y data size.", nameof(frame));
            if (frame.data[2] - frame.data[1] < _uSize)
                throw new ArgumentException("Invalid U data size.", nameof(frame));

            var pPacket = ffmpeg.av_packet_alloc();

            try
            {

                ffmpeg.avcodec_send_frame(_pCodecContext, &frame);
  
                bool hasFinishedWithThisFrame;

                do
                {
                 
                    ffmpeg.av_packet_unref(pPacket);


                    var response = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);

                    bool isPacketValid;

                    if (response == 0)
                    {

                        isPacketValid = true;
                        hasFinishedWithThisFrame = false;
                    }
                    else if (response == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    {
                        // EAGAIN: there's no more output is available in the current state - user must try to send more input
                        isPacketValid = false;
                        hasFinishedWithThisFrame = true;
                    }
                    else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                    {
                        // EOF: the encoder has been fully flushed, and there will be no more output packets
                        isPacketValid = false;
                        hasFinishedWithThisFrame = true;
                    }
                    else
                    {
                        // AVERROR(EINVAL): codec not opened, or it is a decoder other errors: legitimate encoding errors
                        // , otherwise negative error code:
                        throw new InvalidOperationException($"error from avcodec_receive_packet: {response}");
                    }

                    if (isPacketValid)
                    {
                        using var packetStream = new UnmanagedMemoryStream(pPacket->data, pPacket->size);
                        packetStream.CopyTo(_stream);
                    }
                } while (!hasFinishedWithThisFrame);
            }
            finally
            {
                ffmpeg.av_packet_free(&pPacket);
            }
        }

        public void Drain()
        {

            var pPacket = ffmpeg.av_packet_alloc();

            try
            {


                bool hasFinishedDraining;

                do
                {

                    ffmpeg.av_packet_unref(pPacket);

                    var response = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);

                    bool isPacketValid;

                    if (response == 0)
                    {
                        // 0 on success; as in, successfully retrieved a packet, and expecting us to retrieve another one.
                        isPacketValid = true;
                        hasFinishedDraining = false;
                    }
                    else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                    {
                        // EOF: the encoder has been fully flushed, and there will be no more output packets
                        isPacketValid = false;
                        hasFinishedDraining = true;
                    }
                    else
                    {
                        // Some other error.
                        // Should probably throw here, but in testing we get error -541478725
                        isPacketValid = false;
                        hasFinishedDraining = true;
                    }

                    if (isPacketValid)
                    {
                        using var packetStream = new UnmanagedMemoryStream(pPacket->data, pPacket->size);
                        packetStream.CopyTo(_stream);
                    }
                } while (!hasFinishedDraining);
            }
            finally
            {
                ffmpeg.av_packet_free(&pPacket);
            }
        }
        public void any(IDirect3DDevice device)
        {
            AVBufferRef* hwDeviceCtx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
            AVHWDeviceContext* devCtx = (AVHWDeviceContext*)hwDeviceCtx->data;
            AVD3D11VADeviceContext* d3d11Ctx = (AVD3D11VADeviceContext*)devCtx->hwctx;

            // d3d11Ctx->device = (ID3D11Device*)device.NativePointer; // your D3D11 device
            MediaTranscoder mediaTranscoder = new();
            mediaTranscoder.HardwareAccelerationEnabled = true;
            

        


        }
    }

}

