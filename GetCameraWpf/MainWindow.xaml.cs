using System;
using System.Collections.Generic;
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
using Accord.Video.DirectShow;
using Accord.Video;
using Accord.Video.VFW;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GetCameraWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            devices = new List<string>();
            InitializeComponent();
            DataContext = this;
            Framenumber = 0;
            frames = new Dictionary<int, Bitmap>();
            Sources = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            Status = "Select a Device";
            foreach (FilterInfo f in Sources)
            {
                devices.Add(f.Name);
            }
            cmbDevices.ItemsSource = devices;
            //lblSource.Content = "";
            filename = "-snapshot.png";
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            txtSaveLocation.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            FrameRate = 20;

        }
        public FilterInfoCollection Sources;
        public List<string> devices;
        private FilterInfo sourceInfo;
        private VideoCaptureDevice source;
        public BitmapImage frame;
        private string folder;
        private string filename;
        private string status;
        private int framerate;
        private int framenumber;
        private string _sourceDevice;

        private VideoCapabilities[] currentCaps;
        private Dictionary<int,Bitmap> frames;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Folder
        {
            get => folder; set
            {
                folder = value;
                NotifyPropertyChanged();
            }
        }
        public string Filename
        {
            get { return filename; }
            set
            {
                filename = value;
                NotifyPropertyChanged();
            }
        }
        public string Status
        {
            get => status; set
            {
                status = value;
                NotifyPropertyChanged();
            }
        }
        public int Framenumber
        {
            get => framenumber; set
            {
                framenumber = value;
                NotifyPropertyChanged();
            }
        }

        public string SourceDevice { get { return _sourceDevice; } set {
                _sourceDevice = value;
                NotifyPropertyChanged(); } }

        public int FrameRate { get { return framerate; } set{ framerate = value; NotifyPropertyChanged(); } }

        private void SetSource()
        {
            int index = cmbDevices.SelectedIndex;
            if (index > -1)
            {
                SourceDevice = devices[index];
                Status = "Selected the " + devices[index] + ".";
                sourceInfo = Sources[index];
                source = new VideoCaptureDevice(Sources[index].MonikerString);
                source.NewFrame += new NewFrameEventHandler(ProcessFrames);
                currentCaps = source.VideoCapabilities;
                source.DesiredAverageTimePerFrame = 0;
                
            }
            else
            {

                Status = "Error, no device available, or something.";

            }

        }

        private void StartRecording()
        {
            if (sourceInfo != null)
            {
                source.Start();
            }
        }

        private void StopRecording()
        {

            if (source != null)
            {
                source.Stop();
            }
        }

        private void ProcessFrames(object sender, NewFrameEventArgs e)
        {
            System.Drawing.Image img = (Bitmap)e.Frame.Clone();
            frames.Add(Framenumber, (Bitmap)img);
            MemoryStream ms = new MemoryStream();
            img.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            frame = bi;
            Framenumber++;
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                imgCamFeed.Source = bi;
                txbFrame.Text = Framenumber.ToString();
            }));

        }

        private void ExportFrames() {
            AVIWriter aw = new AVIWriter();
            aw.FrameRate = FrameRate;
            aw.Open(System.IO.Path.Combine(Folder, DateTime.Now.ToFileTime().ToString() + "camera.avi"), Convert.ToInt32(640), Convert.ToInt32(480));

            List<Bitmap> fs = new List<Bitmap>(frames.Values);
            foreach (var f in fs)
            {
                aw.AddFrame(f);
            }
            aw.Close();
        }

        private void SaveFrame()
        {
            if (frame != null)
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame));
                var p = System.IO.Path.Combine(Folder, DateTime.Now.ToFileTime().ToString() + filename);
                using (var fileStream = new FileStream(p, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }

        public void SelectFolderDialog()
        {
            using (FolderBrowserDialog fd = new FolderBrowserDialog())
            {
                DialogResult result = fd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Folder = fd.SelectedPath.ToString();
                    txtSaveLocation.Text = Folder;
                }


            }
        }



        private void cmbDevices_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {

            SetSource();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFrame();
        }

        private void btnChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            SelectFolderDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRecording();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportFrames();
        }
    }
    public class RelayCommand : ICommand
    {
        private Action<object> _execute;
        private Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute)
       : this(execute, null)
        {
        }
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }

        }

        public bool CanExecute(object parameters)
        {
            return _canExecute == null ? true : _canExecute(parameters);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

}
