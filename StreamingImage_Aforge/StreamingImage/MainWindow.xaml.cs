using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace MyStreamingServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// List of cameras which connect to the computer
        /// </summary>
        private List<KeyValuePair<int, string>> ImageResolution = new List<KeyValuePair<int, string>>();

        /// <summary>
        /// List of cameras which connect to the computer
        /// </summary>
        public List<KeyValuePair<int, string>> cameras = new List<KeyValuePair<int, string>>();

        System.ComponentModel.BackgroundWorker MyWorker = new System.ComponentModel.BackgroundWorker();


        VideoCaptureDevice localWebCam;
        FilterInfoCollection videoDevices;


       
        System.Drawing.Image imagesSource;
       


       

        private ImageStreamingServer _Server;

        private const int PORT_INFOR = 4567;

       

        public MainWindow()
        {
            InitializeComponent();
            initResolution();
            MyWorker.DoWork += MyWorker_DoWork;
            MyWorker.RunWorkerCompleted += MyWorker_RunWorkerCompleted;

            String exePath = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            string dir = System.IO.Path.GetDirectoryName(exePath);
 
            this.Closed += MainWindow_Closed;
            string hostName = Dns.GetHostName();
            var temp = Dns.GetHostByName(hostName).AddressList.ToList();
            foreach (IPAddress ip in temp)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    lblIpCam.Content = "Ip Camera: " + ip.ToString();
                }
            
            }
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            StopServer();
        }

       
        List<System.Drawing.Image> frameList = new List<System.Drawing.Image>();

        public IEnumerable<System.Drawing.Image> Snapshots()
        {

            while (true)
            {

                Bitmap dstImage = frameList.LastOrDefault() as Bitmap;
                yield return dstImage;

            }
        }

        

        private void initCamera()
        {

            this.cmbCamera.Items.Clear();
            this.cmbCamera.ItemsSource = this.cameras;
            this.cmbCamera.DisplayMemberPath = "Value";
            this.cmbCamera.SelectedValuePath = "Key";
        }

        private void initResolution()
        {

            KeyValuePair<int, string> resolution1 = new KeyValuePair<int, string>(1, "320x240");
            KeyValuePair<int, string> resolution2 = new KeyValuePair<int, string>(2, "480x320");
            KeyValuePair<int, string> resolution3 = new KeyValuePair<int, string>(3, "640x480");
            KeyValuePair<int, string> resolution4 = new KeyValuePair<int, string>(4, "1920x1080");

            ImageResolution.Add(resolution1);
            ImageResolution.Add(resolution2);
            ImageResolution.Add(resolution3);
            ImageResolution.Add(resolution4);

            this.cmbImageSize.ItemsSource = ImageResolution;
            this.cmbImageSize.DisplayMemberPath = "Value";
            this.cmbImageSize.SelectedValuePath = "Key";


        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            cmbCamera.IsEnabled = false;
            this.btnSearch.IsEnabled = false;
            this.btnSearch.Content = "Detecting...";
            MyWorker.RunWorkerAsync();


        }

        void MyWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            this.btnSearch.Content = "Search";
            this.btnSearch.IsEnabled = true;
            cmbCamera.IsEnabled = true;
            initCamera();
            int countCamera = cmbCamera.Items.Count;
            lblCameraCount.Content = countCamera.ToString() + " Camera is found";
        }



        private void MyWorker_DoWork(object Sender, System.ComponentModel.DoWorkEventArgs e)
        {


            Thread.Sleep(200);
            videoDevices = new FilterInfoCollection(
                      FilterCategory.VideoInputDevice);

            int _DeviceIndex = 0;
            for (int i = videoDevices.Count; i > 0; i--)
            {
                this.cameras.Add(new KeyValuePair<int, string>(_DeviceIndex, videoDevices[i - 1].Name));
                _DeviceIndex++;
            }



            if (MyWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }


        }

        private void cmbCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int indexCam = this.cmbCamera.SelectedIndex;
            localWebCam = new VideoCaptureDevice(videoDevices[0].MonikerString);
            localWebCam.NewFrame += localWebCam_NewFrame;
            localWebCam.DesiredFrameRate = 60;
            localWebCam.DesiredFrameSize = new System.Drawing.Size(320, 240);
            localWebCam.Start();
          
            
            if (_Server == null)
            {
                _Server = new ImageStreamingServer(Snapshots());
                _Server.Interval = 30;
                string port = txtCameraPort.Text;
                if (!_Server.IsRunning)
                    _Server.Start(Convert.ToInt32(port));
            }

        }




        void localWebCam_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            try
            {
                System.Drawing.Image img = (Bitmap)eventArgs.Frame.Clone();

                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                ms.Seek(0, SeekOrigin.Begin);

                bi.StreamSource = ms;
                bi.EndInit();

                bi.Freeze();
               
               

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = bi;

                    this.cvImageView.Background = ib;
                    lblFrame.Content = "Frames/s: " + localWebCam.DesiredFrameRate.ToString();
                    imagesSource = img;
                    frameList.Clear();
                    frameList.Add(img as Bitmap);
                   
                }));
            }
            catch (Exception ex)
            {
            }
        }

        private void cmbImageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int imageType = this.cmbImageSize.SelectedIndex;
            switch (imageType)
            {
                case 0:
                    //oImage.ImageSize = "320x240";
                    localWebCam.Stop();
                    localWebCam.DesiredFrameSize = new System.Drawing.Size(320, 240);
                    localWebCam.Start();
                  
                   
                    break;
                case 1:
                    // oImage.ImageSize = "480x320";
                    localWebCam.Stop();
                    localWebCam.DesiredFrameSize = new System.Drawing.Size(480, 320);
                    localWebCam.Start();
                    
                   
                    break;
                case 2:
                    //oImage.ImageSize = "640x480";
                    localWebCam.Stop();
                    localWebCam.DesiredFrameSize = new System.Drawing.Size(640, 480);
                    localWebCam.Start();
                   
                   
                    break;
                case 3:
                    // oImage.ImageSize = "1920x1080";
                    localWebCam.Stop();
                    localWebCam.DesiredFrameSize = new System.Drawing.Size(1920, 1080);
                    localWebCam.Start();
                   
                    
                    break;
                default:
                    // oImage.ImageSize = "480x320";
                    localWebCam.Stop();
                    localWebCam.DesiredFrameSize = new System.Drawing.Size(480, 320);
                    localWebCam.Start();
                   
                    
                    break;
            }

        }

       

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopServer();

        }

        private void StopServer()
        {
            if (_Server != null) _Server.Stop();
            if (localWebCam != null) localWebCam.Stop();
           
        }

       
    }
}
