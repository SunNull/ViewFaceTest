using AForge.Video.DirectShow;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using ViewFaceCore;
using ViewFaceCore.Core;
using ViewFaceCore.Model;
using ViewFaceTest.Extensions;
using ViewFaceTest.Models;
using ViewFaceTest.Utils;
using FaceInfo = ViewFaceCore.Model.FaceInfo;

namespace ViewFaceTest
{
    public partial class MainForm : Form
    {
        private const string _defaultCapability = "1280x720";
        private const string _enableBtnString = "�ر�����ͷ";
        private const string _disableBtnString = "������ͷ��ʶ������";

        private bool isDetecting = false;

        /// <summary>
        /// ����ͷ�豸��Ϣ����
        /// </summary>
        private FilterInfoCollection videoDevices;

        /// <summary>
        /// ȡ������
        /// </summary>
        private CancellationTokenSource token = null;

        private ViewFaceFactory faceFactory = null;
        public MainForm()
        {
            InitializeComponent();
        }

        #region Events

        /// <summary>
        /// �������ʱ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Load(object sender, EventArgs e)
        {
            // ��������ͷ����ؼ�
            VideoPlayer.Visible = false;
            //��ʼ��VideoDevices
            �������ͷToolStripMenuItem_Click(null, null);
            //Ĭ�Ͻ������հ�ť
            FormHelper.SetControlStatus(this.ButtonSave, false);
        }

        /// <summary>
        /// ����ر�ʱ���ر�����ͷ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// �����ʼ��ťʱ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            if (ButtonStart.Text == _disableBtnString)
            {
                //��ʼ
                Start();
            }
            else if (ButtonStart.Text == _enableBtnString)
            {
                //ֹͣ
                Stop();
            }
            else
            {
                MessageBox.Show($"Emmmmm...���Ʋ��ԣ�����", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var videoCapture = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
            List<string> supports = videoCapture.VideoCapabilities.OrderBy(p => p.FrameSize.Width).Select(p => $"{p.FrameSize.Width}x{p.FrameSize.Height}").ToList();
            this.comboBox2.DataSource = supports;
            if (supports.Contains(_defaultCapability))
            {
                comboBox2.SelectedIndex = supports.IndexOf(_defaultCapability);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxDetect_CheckedChanged(object sender, EventArgs e)
        {
            CheckBoxFaceProperty.Enabled = CheckBoxDetect.Checked;
            CheckBoxFaceMask.Enabled = CheckBoxDetect.Checked;
            CheckBoxFPS.Enabled = CheckBoxDetect.Checked;
            numericUpDownFPSTime.Enabled = CheckBoxDetect.Checked;
        }

        private void ��Ա����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserManageForm userManageForm = new UserManageForm();
            userManageForm.Show();
        }

        private void �˳�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("����~", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (!VideoPlayer.IsRunning)
            {
                MessageBox.Show("���ȿ�������ʶ��", "����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FormHelper.SetControlStatus(ButtonSave, false);
            _ = Task.Run(() =>
            {
                using (Bitmap bitmap = VideoPlayer.GetCurrentVideoFrame())
                {
                    if (bitmap == null)
                    {
                        MessageBox.Show("����ʧ�ܣ�û�л�ȡ��ͼ�������ԣ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        FormHelper.SetControlStatus(ButtonSave, true);
                        return;
                    }
                    else
                    {
                        UserInfoFormParam formParam = new UserInfoFormParam()
                        {
                            Bitmap = bitmap.DeepClone(),
                        };
                        FormHelper.SetControlStatus(ButtonSave, true);
                        //�򿪱����
                        UserInfoForm saveUser = new UserInfoForm(formParam);
                        saveUser.ShowDialog();
                    }
                }
            });
        }

        private void �������ͷToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.comboBox1.Enabled)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                comboBox1.Items.Clear();
                comboBox2.DataSource = null;
                foreach (FilterInfo info in videoDevices)
                {
                    comboBox1.Items.Add(info.Name);
                }
                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
        }

        private void ǿ��ˢ�»���ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CacheManager.Instance.Refesh();
            MessageBox.Show($"������ˢ�£�", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Methods

        private (int width, int height) GetSelectCapability()
        {
            string selectStr = this.comboBox2.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectStr))
            {
                selectStr = _defaultCapability;
            }
            string[] items = selectStr.Split('x');
            if (items.Length != 2)
            {
                throw new Exception("Get capability from select item failed.");
            }
            return (int.Parse(items[0]), int.Parse(items[1]));
        }

        private void Start()
        {
            if (VideoPlayer.IsRunning)
            {
                Stop();
            }

            FormHelper.SetControlStatus(this.comboBox1, false);
            FormHelper.SetControlStatus(this.comboBox2, false);
            FormHelper.SetControlStatus(this.ButtonStart, false);
            FormHelper.SetControlStatus(this.ButtonSave, false);
            bool isSuccess = true;

            try
            {
                if (comboBox1.SelectedIndex < 0)
                {
                    MessageBox.Show($"û���ҵ����õ�����ͷ�������ԣ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isSuccess = false;
                    return;
                }
                (int width, int height) = GetSelectCapability();
                if (faceFactory == null)
                {
                    faceFactory = new ViewFaceFactory(width, height);
                }
                FilterInfo info = videoDevices[comboBox1.SelectedIndex];
                VideoCaptureDevice videoCapture = new VideoCaptureDevice(info.MonikerString);
                var videoResolution = videoCapture.VideoCapabilities.Where(p => p.FrameSize.Width == width && p.FrameSize.Height == height).FirstOrDefault();
                if (videoResolution == null)
                {
                    List<string> supports = videoCapture.VideoCapabilities.OrderBy(p => p.FrameSize.Width).Select(p => $"{p.FrameSize.Width}x{p.FrameSize.Height}").ToList();
                    string supportStr = "�ޣ����ȡʧ��";
                    if (supports.Any())
                    {
                        supportStr = string.Join("|", supports);
                    }
                    MessageBox.Show($"����ͷ��֧������ֱ���Ϊ{width}x{height}����Ƶ��������ָ���ֱ��ʡ�\n֧�ֱַ��ʣ�{supportStr}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isSuccess = false;
                    return;
                }
                videoCapture.VideoResolution = videoResolution;
                VideoPlayer.VideoSource = videoCapture;
                VideoPlayer.Start();

                Stopwatch stopwatch = Stopwatch.StartNew();
                while (!VideoPlayer.IsRunning)
                {
                    if (stopwatch.ElapsedMilliseconds > 10000)
                    {
                        //10s��ʱ
                        stopwatch.Stop();
                        isSuccess = false;
                        MessageBox.Show($"����ʧ�ܣ������ԣ�", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Thread.Sleep(1);
                }
                stopwatch.Stop();
                CacheManager.Instance.Refesh();
                token = new CancellationTokenSource();
                StartDetector(token.Token);
            }
            finally
            {
                if (isSuccess)
                {
                    FormHelper.SetButtonText(ButtonStart, _enableBtnString);
                    FormHelper.SetControlStatus(this.ButtonStart, true);
                    FormHelper.SetControlStatus(this.ButtonSave, true);
                }
                else
                {
                    FormHelper.SetControlStatus(this.comboBox1, true);
                    FormHelper.SetControlStatus(this.comboBox2, true);
                    FormHelper.SetControlStatus(this.ButtonStart, true);
                    FormHelper.SetControlStatus(this.ButtonSave, false);
                }
            }
        }

        private async void Stop()
        {
            try
            {
                if (!VideoPlayer.IsRunning)
                {
                    return;
                }
                FormHelper.SetControlStatus(this.ButtonStart, false);
                VideoPlayer?.SignalToStop();
                VideoPlayer?.WaitForStop();
                token?.Cancel();

                FormHelper.SetButtonText(ButtonStart, "�ر���...");
                bool isStopped = true;
                //�ȴ�����������
                await Task.Run(() =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (isDetecting)
                    {
                        if (stopwatch.ElapsedMilliseconds > 10000)
                        {
                            isStopped = false;
                            break;
                        }
                    }
                    stopwatch.Stop();
                });
                if (!isStopped)
                {
                    MessageBox.Show($"���󣺹ر�����ͷ��ʱ��", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //����ͼ�οؼ�Ϊ�հ�
                FormHelper.SetPictureBoxImage(FacePictureBox, null);
                //�ͷ�����ʶ�����
                faceFactory?.Dispose();
                faceFactory = null;
                //token�ͷ�
                token.Dispose();
                token = null;
            }
            finally
            {
                FormHelper.SetButtonText(ButtonStart, _disableBtnString);
                FormHelper.SetControlStatus(this.ButtonStart, true);
                FormHelper.SetControlStatus(this.comboBox1, true);
                FormHelper.SetControlStatus(this.comboBox2, true);
                FormHelper.SetControlStatus(this.ButtonSave, false);
            }
        }

        /// <summary>
        /// �������һ��������ֱ��ֹͣ��
        /// </summary>
        /// <param name="token">ȡ�����</param>
        private async void StartDetector(CancellationToken token)
        {
            List<double> fpsList = new List<double>();
            double fps = 0;
            Stopwatch stopwatchFPS = new Stopwatch();
            Stopwatch stopwatch = new Stopwatch();
            isDetecting = true;
            try
            {
                while (VideoPlayer.IsRunning && !token.IsCancellationRequested)
                {
                    try
                    {
                        if (CheckBoxFPS.Checked)
                        {
                            stopwatch.Restart();
                            if (!stopwatchFPS.IsRunning)
                            { stopwatchFPS.Start(); }
                        }
                        Bitmap bitmap = VideoPlayer.GetCurrentVideoFrame(); // ��ȡ����ͷ���� 
                        if (bitmap == null)
                        {
                            await Task.Delay(10, token);
                            FormHelper.SetPictureBoxImage(FacePictureBox, bitmap);
                            continue;
                        }
                        if (!CheckBoxDetect.Checked)
                        {
                            await Task.Delay(1000 / 60, token);
                            FormHelper.SetPictureBoxImage(FacePictureBox, bitmap);
                            continue;
                        }
                        List<Models.FaceInfo> faceInfos = new List<Models.FaceInfo>();

                        using (FaceImage faceImage = ViewFaceSkiaSharpExtension.ToFaceImage(bitmap))
                        {
                            var infos = await faceFactory.Get<FaceTracker>().TrackAsync(faceImage);
                            for (int i = 0; i < infos.Length; i++)
                            {

                                Models.FaceInfo faceInfo = new Models.FaceInfo
                                {
                                    Pid = infos[i].Pid,
                                    Location = infos[i].Location
                                };
                                if (CheckBoxFaceMask.Checked || CheckBoxFaceProperty.Checked)
                                {
                                    FaceInfo info = infos[i].ToFaceInfo();
                                    if (CheckBoxFaceMask.Checked)
                                    {
                                        var maskStatus = await faceFactory.Get<MaskDetector>().PlotMaskAsync(faceImage, info);
                                        faceInfo.HasMask = maskStatus.Masked;
                                    }
                                    if (CheckBoxFaceProperty.Checked)
                                    {
                                        FaceRecognizer faceRecognizer = null;
                                        if (faceInfo.HasMask)
                                        {
                                            faceRecognizer = faceFactory.GetFaceRecognizerWithMask();
                                        }
                                        else
                                        {
                                            faceRecognizer = faceFactory.Get<FaceRecognizer>();
                                        }
                                        var points = await faceFactory.Get<FaceLandmarker>().MarkAsync(faceImage, info);
                                        float[] extractData = await faceRecognizer.ExtractAsync(faceImage, points);

                                        UserInfo userInfo = CacheManager.Instance.Get(faceRecognizer, extractData);
                                        if (userInfo != null)
                                        {
                                            faceInfo.Name = userInfo.Name;
                                            faceInfo.Age = userInfo.Age;
                                            switch (userInfo.Gender)
                                            {
                                                case GenderEnum.Male:
                                                    faceInfo.Gender = Gender.Male;
                                                    break;
                                                case GenderEnum.Female:
                                                    faceInfo.Gender = Gender.Female;
                                                    break;
                                                case GenderEnum.Unknown:
                                                    faceInfo.Gender = Gender.Unknown;
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            faceInfo.Age = await faceFactory.Get<AgePredictor>().PredictAgeAsync(faceImage, points);
                                            faceInfo.Gender = await faceFactory.Get<GenderPredictor>().PredictGenderAsync(faceImage, points);
                                        }
                                    }
                                }
                                faceInfos.Add(faceInfo);
                            }
                        }
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            if (faceInfos.Any()) // ������������� bitmap �ϻ��Ƴ�������λ����Ϣ
                            {
                                g.DrawRectangles(new Pen(Color.Red, 4), faceInfos.Select(p => p.Rectangle).ToArray());
                                if (CheckBoxDetect.Checked)
                                {
                                    for (int i = 0; i < faceInfos.Count; i++)
                                    {
                                        StringBuilder builder = new StringBuilder();
                                        if (CheckBoxFaceMask.Checked || CheckBoxFaceProperty.Checked)
                                        {
                                            builder.Append($"Pid: {faceInfos[i].Pid}");
                                            builder.Append(" | ");
                                        }
                                        if (CheckBoxFaceMask.Checked)
                                        {
                                            builder.Append($"���֣�{(faceInfos[i].HasMask ? "��" : "��")}");
                                            if (CheckBoxFaceProperty.Checked)
                                            {
                                                builder.Append(" | ");
                                            }
                                        }
                                        if (CheckBoxFaceProperty.Checked)
                                        {
                                            if (!string.IsNullOrEmpty(faceInfos[i].Name))
                                            {
                                                builder.Append(faceInfos[i].Name);
                                                builder.Append(" | ");
                                            }
                                            builder.Append($"{faceInfos[i].Age} ��");
                                            builder.Append(" | ");
                                            builder.Append($"{faceInfos[i].GenderDescribe}");
                                            builder.Append(" | ");
                                        }
                                        g.DrawString(builder.ToString(), new Font("΢���ź�", 24), Brushes.Green, new PointF(faceInfos[i].Location.X + faceInfos[i].Location.Width + 24, faceInfos[i].Location.Y));
                                    }
                                }
                            }
                            if (CheckBoxFPS.Checked)
                            {
                                stopwatch.Stop();

                                if (numericUpDownFPSTime.Value > 0)
                                {
                                    fpsList.Add(1000f / stopwatch.ElapsedMilliseconds);
                                    if (stopwatchFPS.ElapsedMilliseconds >= numericUpDownFPSTime.Value)
                                    {
                                        fps = fpsList.Average();
                                        fpsList.Clear();
                                        stopwatchFPS.Reset();
                                    }
                                }
                                else
                                {
                                    fps = 1000f / stopwatch.ElapsedMilliseconds;
                                }
                                g.DrawString($"{fps:#.#} FPS", new Font("΢���ź�", 24), Brushes.Green, new Point(10, 10));
                            }
                        }
                        FormHelper.SetPictureBoxImage(FacePictureBox, bitmap);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch { }
                }
            }
            finally
            {
                isDetecting = false;
            }
        }

        #endregion
    }
}