using AeroEduLib;
using Booth_Camera.Lib;
using cs_framework;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Booth_Camera
{
    public partial class frmMain1 : DevComponents.DotNetBar.RibbonForm
    {
        #region MyRegion

        string rtspCam;
        string savePath;
        bool isRecord = false;
        bool isMouseDown;// 鼠标是否按下
        bool isPause;//是否暂停
        bool isEnlarge; // 是否放大
        Rectangle MouseRect;// 鼠标拖拽的矩形
        Point pStart, pEnd;// 鼠标拖拽后，起点与终点的坐标
        bool isDraw = false;// 画笔开关是否打开

        Graphics gp = null;//显示的画板
        Graphics gp1 = null;//隐藏的画板
        Graphics gp2 = null;//最小化时保存当前图像的画板
        Pen pen = null;
        Image img = null;
        int penWidth = 2;

        int saveCommentInterval = 5;
        int maxCommentImageCount = 20;

        string[] subjects;

        #endregion

        public frmMain1()
        {
            InitializeComponent();
            
        }

        private void RibbonForm1_Resize(object sender, EventArgs e)
        {
            // 设置视频窗口比例为16:9
            plCamera.Height = (plCamera.Width * 9) / 16;
            if (img != null)
                ReDraw();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Init();
            btnColor.SelectedColor = Color.Red;
            SetPen(ref pen);
            imglist1.Visible = true;
            comboTree1.SelectedIndex = 0;
            comboTree2.SelectedIndex = 0;
        }
        // 初始化
        private void Init()
        {
            LoadConfig();
            Booth.fnInit(this.Handle, plCamera.Handle);
            Preview();
            BuildDir();
            DisablePenControl();
        }
        /// <summary>
        /// 建立各种目录 
        /// </summary>
        private void BuildDir()
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            if (!Directory.Exists(savePath + "\\图片"))
                Directory.CreateDirectory(savePath + "\\图片");
            if (!Directory.Exists(savePath + "\\视频"))
                Directory.CreateDirectory(savePath + "\\视频");
            if (!Directory.Exists(savePath + "\\错题集"))
                Directory.CreateDirectory(savePath + "\\错题集");

            if (subjects.Length > 0)
            {
                comboTree2.Nodes.Add(new DevComponents.AdvTree.Node("选择科目"));
                foreach (string item in subjects)
                {
                    comboTree2.Nodes.Add(new DevComponents.AdvTree.Node(item));
                    if (!Directory.Exists(savePath + "\\错题集\\" + item))
                        Directory.CreateDirectory(savePath + "\\错题集\\" + item);
                }
            }
        }
        
        // 预览。。。。
        private CSAVFrameWork m_csAVFrm;
        int audioPreviewerId, videoPreviewerId;
        int rtspRecorderId;
        string audioName;
        void Preview()
        {
            m_csAVFrm = new CSAVFrameWork();
            CSAVFrameWork.initialize();
            audioName = CSAVFrameWork.getAudioDefaultInputDeviceName();
            audioPreviewerId = m_csAVFrm.startPreview(IntPtr.Zero, emAVDType.emUSBMicroPhone, audioName);
            videoPreviewerId = m_csAVFrm.startPreview(btnRecordPause.Handle, emAVDType.emRtsp, rtspCam);
        }
        // 录像
        void RecordStart()
        {
            string timespan = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string packName = savePath + "\\视频\\" + timespan;
            string fileName = packName + "\\video.mp4";
            
            if (!Directory.Exists(packName))
                Directory.CreateDirectory(packName);

            rtspRecorderId = m_csAVFrm.createMp4Recorder(fileName);
            m_csAVFrm.addPreviewerToRecorder(rtspRecorderId, audioPreviewerId);
            m_csAVFrm.addPreviewerToRecorder(rtspRecorderId, videoPreviewerId);
            m_csAVFrm.startRecordMp4(rtspRecorderId);
        }
        // 停止
        void RecordStop()
        {
            m_csAVFrm.stopRecordMp4(rtspRecorderId);
        }
        // 录制
        private void btnRecordControl_Click(object sender, EventArgs e)
        {
            if (!isRecord)
            {
                RecordStart();
                isRecord = true;
                btnRecordControl.Text = "停止";
                btnRecordControl.Tooltip = "停止录像";
                lbRecordTime.Text = "00:00:00";
                timer1.Start();
                btnComment.Enabled = false;
                imglist1.Enabled = false;
                comboTree2.Enabled = false;
                btnJoinErrCol.Enabled = false;
                btnOpenErr.Enabled = false;
                btnSnapshot.Enabled = false;
                btnOpendir.Enabled = false;
                btnRecordPause.Enabled = true;
                NoticeShow("开始录像。");
            }
            else
            {
                img = null;
                RecordStop();
                isRecord = false;
                btnRecordControl.Text = "录像";
                btnRecordControl.Tooltip = "开始录像";
                timer1.Stop();
                btnRecordPause.Text = "暂停录像";
                isPause = false;
                Booth.fnOnRButtonDown();
                recordSenconds = 0;
                btnComment.Enabled = true;
                imglist1.Enabled = true;
                comboTree2.Enabled = true;
                btnJoinErrCol.Enabled = true;
                btnOpenErr.Enabled = true;
                btnSnapshot.Enabled = true;
                btnOpendir.Enabled = true;
                btnRecordPause.Enabled = false;
                NoticeShow("录像结束。");
            }
        }
        // 截图
        private void btnSnapshot_Click(object sender, EventArgs e)
        {
            Booth.fnCatchPic();
            NoticeShow("截图已保存。");
        }
        
        // 录制计时器
        int recordSenconds = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            recordSenconds++;
            lbRecordTime.Text = Convert.ToDateTime("00:00:00").AddSeconds(recordSenconds).ToString("HH:mm:ss");
        }
        // 恢复默认配置 未启用
        // 打开保存目录
        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(savePath + "\\视频");
        }
        // 放大后的恢复 与 暂停恢复
        private void btnEnlargeReset_Click(object sender, EventArgs e)
        {
            img = null;
            EnlargeReset();
            plCamera.Refresh();
            ButtonEnable(true);
            // 画笔关闭
            isDraw = false;
            btnComment.Text = "开始批注";
            DisablePenControl();
            btnRecordControl.Enabled = true;
        }

        private void DisablePenControl()
        {
            btnColor.Enabled = false;
            comboTree1.Enabled = false;
            btnSaveComment.Enabled = false;
        }

        private void EnablePenControl()
        {
            btnColor.Enabled = true;
            comboTree1.Enabled = true;
            btnSaveComment.Enabled = true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hwnd, ref Rectangle lpRect);

        // 放大恢复
        private void EnlargeReset()
        {
            Booth.fnOnRButtonDown();
            isEnlarge = false;
            isPause = false;
        }
        #region 鼠标拖拽矩形及局部放大代码
        private void panelCamera_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            pEnd = new Point(e.X, e.Y);
            Cursor.Clip = Rectangle.Empty;
            // 画笔
            
        }

        private void panelCamera_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isRecord)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (!isDraw)
                    {
                        isDraw = true;
                        Booth.fnOnLButtonDown();
                        isPause = true;
                        this.btnComment.Text = "结束批注";
                        EnablePenControl();
                        btnRecordControl.Enabled = false;
                        btnNear.Enabled = false;
                        btnFar.Enabled = false;
                        gp = plCamera.CreateGraphics();
                        gp.SmoothingMode = SmoothingMode.AntiAlias;  //使绘图质量最高，即消除锯齿
                        gp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gp.CompositingQuality = CompositingQuality.HighQuality;

                        ImageFromControl(ref gp1);
                        NoticeShow("已经开始批注。");
                    }

                    if (isDraw)
                    {
                        isPause = true;
                        pStart = new Point(e.X, e.Y);// ?
                        Booth.fnOnLButtonDown();
                        DrawRectangle();
                        isMouseDown = true;
                        DrawStart(e.Location);
                    }
                }
            }
        }

        private void panelCamera_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                if (isDraw)
                {
                    DrawLine(e.Location);
                }
               
            }
        }

        private void DrawLine(Point p)
        {
            gp.DrawCurve(pen, new Point[2] { pStart, p });
            gp1.DrawLine(pen, pStart, p);
            pStart = p;
        }

        private void ResizeToRectangle(Point p)
        {
            DrawRectangle();
            MouseRect.Width = p.X - MouseRect.Left;
            MouseRect.Height = p.Y - MouseRect.Top;
            DrawRectangle();
        }

        private void DrawRectangle()
        {
            ControlPaint.DrawReversibleFrame(Rectangle.Empty, Color.Gray, FrameStyle.Dashed);
            Rectangle rect = this.RectangleToScreen(MouseRect);
            ControlPaint.DrawReversibleFrame(rect, Color.Gray, FrameStyle.Dashed);
        }

        private void DrawStart(Point StartPoint)
        {
            this.plCamera.Capture = true;
            //这是设置鼠标筐选时鼠标的移动区域 和控件对鼠标的捕获
           
            Cursor.Clip = this.RectangleToScreen(this.plCamera.ClientRectangle);
            MouseRect = new Rectangle(StartPoint.X, StartPoint.Y, 0, 0);
        }
        #endregion
        // 保存批注
        int i;
        // 屏幕截图
        private void ImageFromControl(ref Graphics g)
        {
            Bitmap bit = new Bitmap(plCamera.Width, plCamera.Height);//实例化一个和窗体一样大的bitmap
            g = Graphics.FromImage(bit);
            g.CompositingQuality = CompositingQuality.HighQuality;//质量设为最高
           
            g.CopyFromScreen(plCamera.PointToScreen(Point.Empty), Point.Empty, plCamera.Size);//只保存某个控件（这里是panel游戏区）
            img = bit;
        }
       
        // 退出程序
        private void btnExitApp_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("是否要退出？", "退出提示", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        // 最小化
        private void btnMinsize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ButtonEnable(bool enable)
        {
            btnFar.Enabled = enable;
            btnNear.Enabled = enable;
            btnOpendir.Enabled = enable;
            btnRecordControl.Enabled = enable;
        }
        // 重绘
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (img != null)
                ReDraw();
        }
        // 重绘
        private void ReDraw()
        {
            gp2 = plCamera.CreateGraphics();
            gp2.DrawImage(img, 0, 0, img.Width, img.Height);
        }
        // 更改颜色
        private void btnColor_SelectedColorChanged(object sender, EventArgs e)
        {
            SetPen(ref pen);
        }
        // 设置画笔
        private void SetPen(ref Pen pen)
        {
            pen = new Pen(btnColor.SelectedColor, penWidth);
            pen.LineJoin = LineJoin.Round;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
        }
        // 开始批注
        // 通知消失
        private void timer2_Tick(object sender, EventArgs e)
        {
        }
        // 通知
        void NoticeShow(string str)
        {
            timer2.Stop();
            timer2.Start();
            
        }
        // 点击批注历史
        private void imglist1_PictureBoxClick(int i)
        {
            isDraw = true;
            isPause = true;
            Booth.fnOnLButtonDown();
            this.btnComment.Text = "结束批注";
            EnablePenControl();
            btnRecordControl.Enabled = false;
            btnNear.Enabled = false;
            btnFar.Enabled = false;
            gp = plCamera.CreateGraphics();
            gp.SmoothingMode = SmoothingMode.AntiAlias;  //使绘图质量最高，即消除锯齿
            
            img = Image.FromFile(imglist1.GetImage(i));
            gp.DrawImage(img, 0, 0, plCamera.Width, plCamera.Height);
            ImageFromControl(ref gp1);
        }
        // 画笔粗细选择
        private void comboTree1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboTree1.SelectedIndex)
            {
                case 0:
                    penWidth = 2;
                    break;
                case 1:
                    penWidth = 5;
                    break;
                case 2:
                    penWidth = 8;
                    break;
                case 3:
                    penWidth = 12;
                    break;
            }

            SetPen(ref pen);
        }
        // panel1重绘
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (img != null)
                ReDraw();
        }
        // 退出时保存录像
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRecord)
            {
                Booth.fnStopRecord();
                this.Dispose(true);
                Application.Exit();
            }
            CSAVFrameWork.uninitialize();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (i <= saveCommentInterval && i > 1)
            {
                i = i - 1;
                btnSaveComment.Text = "保存批注(" + i.ToString() + ")";
            }
            else
            {
                if (isDraw)
                    btnSaveComment.Enabled = true;
                timer3.Stop();
                i = saveCommentInterval;
                btnSaveComment.Text = "保存批注";
            }
        }
        // 加入错题集：如果是批注状态，批注的图片入错题集，否则原始图片入错题集
        private void btnJoinErrCol_Click(object sender, EventArgs e)
        {
            if (comboTree2.SelectedIndex == 0)
            {
                MessageBox.Show("请选择科目.");
                return;
            }

            if (imglist1.Count < maxCommentImageCount)
            {
                btnJoinErrCol.Enabled = false;
                string dirName = savePath + "\\错题集\\" + comboTree2.SelectedNode.Text + "\\" + DateTime.Now.ToString("yyyyMMdd");
                if (!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                string imgPath = dirName + "\\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".PNG";
                if (img == null)
                {
                    ImageFromControl(ref gp1);
                }
                if (isDraw)
                {
                    img.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    Graphics g = plCamera.CreateGraphics();
                    ImageFromControl(ref g);
                    img.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                imglist1.AddPic(imgPath);
                NoticeShow("已加入错题集。");
                Thread.Sleep(1000);
                btnJoinErrCol.Enabled = true;
                img = null;
            }
            else
            {
                NoticeShow(string.Format("浏览历史列表只能保存{0}个批注，请清空后再保存。", maxCommentImageCount));
            }
        }
        // 打开错题集目录
        private void btnOpenErr_Click(object sender, EventArgs e)
        {
            if (comboTree2.SelectedIndex == 0)
            {
                System.Diagnostics.Process.Start(savePath + "\\错题集\\");
            }
            else
            {
                System.Diagnostics.Process.Start(savePath + "\\错题集\\" + comboTree2.SelectedNode.Text);
            }
        }

        private void comboTree2_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnJoinErrCol.Enabled = true;
            btnOpenErr.Enabled = true;
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(savePath + "\\图片");
        }
        // 录制暂停/恢复
        // 录制暂停
        private void RecordPause()
        {
            isPause = true;
            Booth.fnOnLButtonDown();
            timer1.Stop();
            btnRecordPause.Text = "恢复录像";
        }
        // 继续录制
        private void RecordResume()
        {
            isPause = false;
            Booth.fnOnRButtonDown();
            timer1.Start();
            btnRecordPause.Text = "暂停录像";
        }
        
        XmlDocument xd = new XmlDocument();
        private void LoadConfig()
        {
            xd.Load("Config.xml");
            savePath = xd.SelectSingleNode("/config/projectPath").InnerText;
            if (savePath.LastIndexOf('\\') != savePath.Length - 1)
            {
                savePath = savePath.TrimEnd('\\');
            }
            rtspCam = xd.SelectSingleNode("/config/rtspSource/rtspFormat").InnerText;
            subjects = xd.SelectSingleNode("/config/Subjects").InnerText.Split('|');
            int.TryParse(xd.SelectSingleNode("/config/MaxCommentImageCount").InnerText, out maxCommentImageCount);
            int.TryParse(xd.SelectSingleNode("/config/SaveCommentInterval").InnerText, out saveCommentInterval);
        }

        private void btnNear_MouseDown(object sender, MouseEventArgs e)
        {
        }

        private void btnNear_MouseUp(object sender, MouseEventArgs e)
        {
        }

        private void btnFar_MouseDown(object sender, MouseEventArgs e)
        {
        }

        private void btnFar_MouseUp(object sender, MouseEventArgs e)
        {
        }
        // 变倍 -
        private void btnFar_Click(object sender, EventArgs e)
        {
            Booth.fnZoomOut();
            Thread.Sleep(500);
            Booth.fnZoomOut();
            NoticeShow("进行缩小。");
        }
        // 变倍 +
        private void btnNear_Click(object sender, EventArgs e)
        {
            Booth.fnZoomIn();
            Thread.Sleep(500);
            Booth.fnZoomIn();
            NoticeShow("进行放大。");
        }

        private void btnSaveComment_Click(object sender, EventArgs e)
        {
            i = saveCommentInterval;
            if (imglist1.Count < maxCommentImageCount)
            {
                string imgPath = string.Format(savePath + @"\图片\{0}\批注{1}.PNG", DateTime.Now.ToString("yyyyMMdd"), DateTime.Now.ToString("yyyyMMddHHmmssfff"));

                if (!Directory.Exists(savePath + "\\图片\\" + DateTime.Now.ToString("yyyyMMdd")))
                    Directory.CreateDirectory(savePath + "\\图片\\" + DateTime.Now.ToString("yyyyMMdd"));

                if (img == null)
                {
                    ImageFromControl(ref gp1);
                }
                img.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
                imglist1.AddPic(imgPath);
                NoticeShow("批注已保存。");
                btnSaveComment.Text = "保存批注(" + i.ToString() + ")";
                timer3.Start();
                btnSaveComment.Enabled = false;
            }
            else
            {
                NoticeShow(string.Format("浏览历史列表只能保存{0}个批注，请清空后再保存。", maxCommentImageCount));
            }
        }

        private void btnComment_Click(object sender, EventArgs e)
        {
            isDraw = !isDraw;
            if (isDraw)
            {
                Booth.fnOnLButtonDown();
                isPause = true;
                this.btnComment.Text = "结束批注";
                EnablePenControl();
                btnRecordControl.Enabled = false;
                btnNear.Enabled = false;
                btnFar.Enabled = false;
                gp = plCamera.CreateGraphics();
                gp.SmoothingMode = SmoothingMode.AntiAlias;  //使绘图质量最高，即消除锯齿
                gp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gp.CompositingQuality = CompositingQuality.HighQuality;

                ImageFromControl(ref gp1);
                NoticeShow("已经开始批注。");
            }
            else
            {
                this.btnComment.Text = "开始批注";
                DisablePenControl();
                btnRecordControl.Enabled = true;
                btnNear.Enabled = true;
                btnFar.Enabled = true;

                btnEnlargeReset_Click(null, null);
                NoticeShow("已经结束批注。");
            }
        }

       

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            if (!isPause)
            {
                RecordPause();
            }
            else
            {
                RecordResume();
            }
        }

    }
}