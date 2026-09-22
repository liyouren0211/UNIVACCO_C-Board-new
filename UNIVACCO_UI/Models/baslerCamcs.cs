using Basler.Pylon;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNIVACCO_UI
{
    public class baslerCamcs
    {
        public int CameraNumber = CameraFinder.Enumerate().Count;
        private readonly object lockObject = new object();
        public delegate void CameraImage(Bitmap bmp);
        public event CameraImage CameraImageEvent;
        private PixelDataConverter pxConvert = new PixelDataConverter();
        private Camera camera;
        private bool isCaptureRequested = false;
        public event CameraImage CameraSnapshotCaptured;  // 單張快照事件
        //控制相機採集圖片的過程！
        public bool Graber;
        public bool freed;
        public bool trigger;
        private bool isKeepShooting = false;

        public baslerCamcs()
        {
            Debug.WriteLine(CameraNumber);
        }


        public void CameraInit()
        {
            Graber = true;
            freed = true;
            camera = new Camera();
            camera.CameraOpened += Configuration.AcquireContinuous;
            camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
            camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
            camera.Open();
        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            Graber = true;
        }

        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            lock (lockObject)
            {
                IGrabResult grabResult = e.GrabResult;
                if (grabResult.IsValid && Graber)
                {
                    //委托就用到了！
                    //CameraImageEvent(GrabResult2Bmp(grabResult));
                    // 將抓取結果轉成 Bitmap（一次就好）
                    Bitmap bmp = GrabResult2Bmp(grabResult);

                    // 即時預覽
                    CameraImageEvent?.Invoke(bmp);

                    // 若有請求截圖
                    if (isCaptureRequested)
                    {
                        isCaptureRequested = false;  // 重設旗標
                        CameraSnapshotCaptured?.Invoke((Bitmap)bmp.Clone()); //拍照事件（外部儲存用）

                    }
                }

            }
        }
        // 攝影機類別中

        public void Stop()
        {
            if (camera != null && camera.StreamGrabber.IsGrabbing)
            {
                try
                {
                    camera.StreamGrabber.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Stop error: {ex.Message}");
                }
            }

            // 🔑 每次 Stop 自動 reset 狀態，避免下次重啟卡住
            isCaptureRequested = false;
            isKeepShooting = false;
        }


        public double GetExposureTime()
        {
            if (camera != null)
            {
                return camera.Parameters[PLCamera.ExposureTime].GetValue();
            }
            return 0;
        }

        public void ExposureTime(int CameraExposureTime)
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.ExposureTime].TrySetValue(CameraExposureTime);
            }
        }

        public void OneShot()
        {
            trigger = true;
            if (Graber == true)
            {
                //PLCamera:所有可用于basler摄像机设备dByStre的参数名称列表  SingleFrame：启用单帧采集模式。               
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);

            }
        }

        public void KeepShot()
        {
            if (isKeepShooting) return;

            trigger = true;

            if (Graber == true)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);

                lock (lockObject)
                {
                    if (!camera.StreamGrabber.IsGrabbing)
                    {
                        camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                        isKeepShooting = true;
                    }
                }
            }
        }

        //串流時觸發拍照一張
        public void CaptureOneFrame()
        {
            isCaptureRequested = true;
        }
        private Bitmap GrabResult2Bmp(IGrabResult grabResult)
        {
            //  PixelFormat   format:新的像素格式 System.Drawing.Bitmap。 这必须指定一个值，开头 Format。(参1:图宽，参2:图高，参3:像素格式)
            Bitmap b = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppArgb);
            //BitmapData:指定位图像的属性
            BitmapData bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);
            //PixelType.BGRA8packed:抓取结果返回像素类型，由图像处理支持类使用
            pxConvert.OutputPixelFormat = PixelType.BGRA8packed;
            IntPtr bmpIntpr = bitmapData.Scan0;
            pxConvert.Convert(bmpIntpr, bitmapData.Stride * b.Height, grabResult);
            b.UnlockBits(bitmapData);
            return b;
        }

        public void DestroyCamera()
        {
            if (freed == true)
            {
                camera.Close();
                camera.Dispose();
                freed = false;
                Graber = false;
                trigger = false;
            }
            else
            {
                return;
            }
        }
    }
}