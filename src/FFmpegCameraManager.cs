﻿using FFmpeg.AutoGen.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using DirectShowLib;
using SIPSorceryMedia.FFmpeg.Interop.Android;
namespace SIPSorceryMedia.FFmpeg
{
    public unsafe class FFmpegCameraManager
    {
        static public List<Camera>? GetCameraDevices()
        {
            List<Camera>? result = null;

            string inputFormat = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dshow"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "v4l2"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "avfoundation"
#if NET5_0_OR_GREATER
                                    : OperatingSystem.IsAndroid() ? "android_camera"
                                    : OperatingSystem.IsIOS() ? "avfoundation"
#endif
                                    : throw new NotSupportedException($"Cannot find adequate input format - OSArchitecture:[{RuntimeInformation.OSArchitecture}] - OSDescription:[{RuntimeInformation.OSDescription}]");
            

            // FFmpeg doesn't implement avdevice_list_input_sources() for the DShow input format yet.
            if (inputFormat == "dshow")
            {
                result = new List<Camera>();
                var dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                for (int i = 0; i < dsDevices.Length; i++)
                {
                    var dsDevice = dsDevices[i];
                    if ((dsDevice.Name != null) && (dsDevice.Name.Length > 0))
                    {
                        Camera camera = new Camera
                        {
                            Name = dsDevice.Name,
                            Path = $"video={dsDevice.Name}"
                        };
                        result.Add(camera);
                    }
                }
            }
            else if (inputFormat == "avfoundation")
            {
                result = SIPSorceryMedia.FFmpeg.Interop.MacOS.AvFoundation.GetCameraDevices();
            }
            else if(inputFormat == "alsa")
            {
                AVInputFormat* avInputFormat = ffmpeg.av_find_input_format(inputFormat);
                AVDeviceInfoList* avDeviceInfoList = null;

                ffmpeg.avdevice_list_input_sources(avInputFormat, null, null, &avDeviceInfoList).ThrowExceptionIfError();
                int nDevices = avDeviceInfoList->nb_devices;
                var avDevices = avDeviceInfoList->devices;

                result = new List<Camera>();
                for (int i = 0; i < nDevices; i++)
                {
                    var avDevice = avDevices[i];
                    var name = Marshal.PtrToStringAnsi((IntPtr)avDevice->device_description);
                    var path = Marshal.PtrToStringAnsi((IntPtr)avDevice->device_name);

                    if ((name != null) && (name.Length > 0))
                    {
                        Camera camera = new Camera
                        {
                            Name = (name == null) ? "" : name,
                            Path = (path == null) ? "" : path,
                        };
                        result.Add(camera);
                    }
                }

                ffmpeg.avdevice_free_list_devices(&avDeviceInfoList);
            }
            else if(inputFormat == "android_camera")
            {
#if ANDROID
                result = AndroidCamera.GetCameras();
#endif
            }
            return result;
        }
    }

    public class Camera
    {
        public String Name { get; set; }

        public String Path { get; set; }

        public Camera()
        {
            Name = Path = "";
        }
    }
}
