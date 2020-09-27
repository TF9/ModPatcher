using BytePatternMatch;
using ModPatcher.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ModPatcher
{
   public class Form1 : Form
   {
      private int nmbFiles;
      private SplitContainer splitContainer1;
      private FolderBrowserDialog folderBrowserDialog1;
      private Button button1;
      private TextBox textBox1;
      private Button button2;
      private Label label1;
      private TextBox textBox2;
      private TextBox tbLog;
      private Label label2;

      public Form1()
      {
         InitializeComponent();
         textBox1.Text = Settings.Default.LastPath;
         textBox2.Text = Settings.Default.Extension;
         folderBrowserDialog1.SelectedPath = textBox1.Text;
      }

      private void button1_Click(object sender, EventArgs e)
      {
         folderBrowserDialog1.ShowDialog();
         textBox1.Text = folderBrowserDialog1.SelectedPath;
         Settings.Default.LastPath = textBox1.Text;
         Settings.Default.Save();
      }

      private void button2_Click(object sender, EventArgs e)
      {
         label1.Text = "...";
         Stopwatch sw = new Stopwatch();
         nmbFiles = 0;
         sw.Reset();
         sw.Start();
         DirectoryInfo directoryInfo = new DirectoryInfo(textBox1.Text);
         BuildTree(directoryInfo);
         sw.Stop();
         label1.Text = $"{nmbFiles} files modified in {sw.Elapsed.TotalSeconds} seconds";
         MessageBox.Show(this, "Finished.", "Mod-Patcher", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
      }

      private void BuildTree(DirectoryInfo directoryInfo)
      {
         FileInfo[] files = directoryInfo.GetFiles();
         foreach (FileInfo fileInfo in files)
         {
            string b = Path.GetExtension(fileInfo.Name).ToLower();
            string str = "";
            if ("." + Settings.Default.Extension.ToLower() == b)
            {
               tbLog.AppendText(DateTime.Now + ": " + fileInfo.FullName + " (" + String.Format("{0:0,0.00}", (float)fileInfo.Length / 1048576.0) + "Mb)" + "\r\n");
               int num = 0;
               FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
               num = PatchAspectRatio16_9(fileStream);
               fileStream.Close();
               str = $" -->> {num} tags modified";
               nmbFiles++;
            }
            Application.DoEvents();
         }
         DirectoryInfo[] directories = directoryInfo.GetDirectories();
         foreach (DirectoryInfo directoryInfo2 in directories)
         {
            BuildTree(directoryInfo2);
         }
      }
      private int PatchAspectRatio16_9(FileStream fs)
      {
         Stopwatch sw = new Stopwatch();
         int num = 0;
         int mod = 0;
         sw.Reset();
         using (BinaryReader binaryReader = new BinaryReader(fs))
         {
            byte[] pattern = new byte[7] { 0x00, 0x00, 0x01, 0xB3, 0x2C, 0x02, 0x40 };
            tbLog.AppendText("   Read... ");
            sw.Start();
            byte[] text = binaryReader.ReadBytes((int)fs.Length);
            sw.Stop();
            tbLog.AppendText(sw.ElapsedMilliseconds + "ms\r\n");
            sw.Reset();
            tbLog.AppendText("   Analyze... ");
            sw.Start();
            BoyerMoore bm = new BoyerMoore(pattern);
            List<long> ret = bm.BoyerMooreMatch(text, 0);
            sw.Stop();
            tbLog.AppendText(sw.ElapsedMilliseconds + "ms\r\n");
            sw.Reset();
            tbLog.AppendText("   Modify... ");
            sw.Start();
            foreach (long item in ret)
            {
               fs.Seek(item + 7, SeekOrigin.Begin);
               byte b = (byte)fs.ReadByte();
               if(0x30 != (b & 0xF0))
               {
                  b = (byte)(b & 0xF);
                  b = (byte)(b | 0x30);
                  fs.Seek(-1L, SeekOrigin.Current);
                  fs.WriteByte(b);
                  ++mod;
               }
               num++;
            }
            sw.Stop();
            tbLog.AppendText(sw.ElapsedMilliseconds + "ms\r\n");
            tbLog.AppendText($"   MPEG header: found: {num} / modified: {mod} .\r\n\r\n");
            return num;
         }
      }

      private void textBox2_TextChanged(object sender, EventArgs e)
      {
         Settings.Default.Extension = textBox2.Text;
         Settings.Default.Save();
      }

      private void InitializeComponent()
      {
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.label2 = new System.Windows.Forms.Label();
         this.textBox2 = new System.Windows.Forms.TextBox();
         this.label1 = new System.Windows.Forms.Label();
         this.button2 = new System.Windows.Forms.Button();
         this.button1 = new System.Windows.Forms.Button();
         this.textBox1 = new System.Windows.Forms.TextBox();
         this.tbLog = new System.Windows.Forms.TextBox();
         this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer1
         // 
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
         this.splitContainer1.Location = new System.Drawing.Point(0, 0);
         this.splitContainer1.Name = "splitContainer1";
         this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainer1.Panel1.Controls.Add(this.label2);
         this.splitContainer1.Panel1.Controls.Add(this.textBox2);
         this.splitContainer1.Panel1.Controls.Add(this.label1);
         this.splitContainer1.Panel1.Controls.Add(this.button2);
         this.splitContainer1.Panel1.Controls.Add(this.button1);
         this.splitContainer1.Panel1.Controls.Add(this.textBox1);
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.tbLog);
         this.splitContainer1.Size = new System.Drawing.Size(532, 440);
         this.splitContainer1.SplitterDistance = 58;
         this.splitContainer1.TabIndex = 1;
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Location = new System.Drawing.Point(344, 16);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(87, 13);
         this.label2.TabIndex = 5;
         this.label2.Text = "Dateierweiterung";
         // 
         // textBox2
         // 
         this.textBox2.Location = new System.Drawing.Point(295, 13);
         this.textBox2.Name = "textBox2";
         this.textBox2.Size = new System.Drawing.Size(43, 20);
         this.textBox2.TabIndex = 4;
         this.textBox2.Text = "MOD";
         this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(14, 38);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(16, 13);
         this.label1.TabIndex = 3;
         this.label1.Text = "...";
         // 
         // button2
         // 
         this.button2.Location = new System.Drawing.Point(438, 10);
         this.button2.Name = "button2";
         this.button2.Size = new System.Drawing.Size(75, 23);
         this.button2.TabIndex = 2;
         this.button2.Text = "Start";
         this.button2.UseVisualStyleBackColor = true;
         this.button2.Click += new System.EventHandler(this.button2_Click);
         // 
         // button1
         // 
         this.button1.Location = new System.Drawing.Point(259, 11);
         this.button1.Name = "button1";
         this.button1.Size = new System.Drawing.Size(24, 21);
         this.button1.TabIndex = 1;
         this.button1.Text = "...";
         this.button1.UseVisualStyleBackColor = true;
         this.button1.Click += new System.EventHandler(this.button1_Click);
         // 
         // textBox1
         // 
         this.textBox1.Location = new System.Drawing.Point(12, 12);
         this.textBox1.Name = "textBox1";
         this.textBox1.Size = new System.Drawing.Size(247, 20);
         this.textBox1.TabIndex = 0;
         this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
         // 
         // tbLog
         // 
         this.tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tbLog.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.tbLog.Location = new System.Drawing.Point(0, 0);
         this.tbLog.Multiline = true;
         this.tbLog.Name = "tbLog";
         this.tbLog.ReadOnly = true;
         this.tbLog.Size = new System.Drawing.Size(532, 378);
         this.tbLog.TabIndex = 0;
         // 
         // Form1
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(532, 440);
         this.Controls.Add(this.splitContainer1);
         this.Name = "Form1";
         this.Text = "MOD 16/9 Patcher";
         this.splitContainer1.Panel1.ResumeLayout(false);
         this.splitContainer1.Panel1.PerformLayout();
         this.splitContainer1.Panel2.ResumeLayout(false);
         this.splitContainer1.Panel2.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      private void textBox1_TextChanged(object sender, EventArgs e)
      {
         Settings.Default.LastPath = textBox1.Text;
         Settings.Default.Save();
      }
   }
}
