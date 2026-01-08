using System;

namespace CodeWalker
{
    partial class CustomPedsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (autoRotateTimer != null)
                {
                    autoRotateTimer.Stop();
                    autoRotateTimer.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomPedsForm));
            this.ToolsPanelShowButton = new System.Windows.Forms.Button();
            this.ToolsPanelHideButton = new System.Windows.Forms.Button();
            this.ToolsDragPanel = new System.Windows.Forms.Panel();
            this.ConsolePanel = new System.Windows.Forms.Panel();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.MousedLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatsLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatsUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.ToolsPanel = new System.Windows.Forms.Panel();
            this.ToolsTabControl = new System.Windows.Forms.TabControl();
            this.ToolsPedTabPage = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.CustomAnimComboBox = new System.Windows.Forms.ComboBox();
            this.PolygonCountText = new System.Windows.Forms.Label();
            this.VertexCountText = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.PlaybackSpeedTrackBar = new System.Windows.Forms.TrackBar();
            this.PlaybackSpeedLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.EnableAnimationCheckBox = new System.Windows.Forms.CheckBox();
            this.EnableRootMotionCheckBox = new System.Windows.Forms.CheckBox();
            this.label23 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.ClipComboBox = new System.Windows.Forms.ComboBox();
            this.label21 = new System.Windows.Forms.Label();
            this.ClipDictComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PedNameComboBox = new System.Windows.Forms.ComboBox();
            this.ToolsModelsTabPage = new System.Windows.Forms.TabPage();
            this.ToolsTexturesTabPage = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.diffuseRadio = new System.Windows.Forms.RadioButton();
            this.liveTxtButton = new System.Windows.Forms.Button();
            this.specularRadio = new System.Windows.Forms.RadioButton();
            this.normalRadio = new System.Windows.Forms.RadioButton();
            this.ToolsOptionsTabPage = new System.Windows.Forms.TabPage();
            this.OnlySelectedCheckBox = new System.Windows.Forms.CheckBox();
            this.AutoRotatePedCheckBox = new System.Windows.Forms.CheckBox();
            this.floorUpDown = new System.Windows.Forms.NumericUpDown();
            this.floorCheckbox = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.WireframeCheckBox = new System.Windows.Forms.CheckBox();
            this.SkeletonsCheckBox = new System.Windows.Forms.CheckBox();
            this.ShadowsCheckBox = new System.Windows.Forms.CheckBox();
            this.HDRRenderingCheckBox = new System.Windows.Forms.CheckBox();
            this.RenderModeComboBox = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.TextureSamplerComboBox = new System.Windows.Forms.ComboBox();
            this.TextureCoordsComboBox = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.TimeOfDayLabel = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.TimeOfDayTrackBar = new System.Windows.Forms.TrackBar();
            this.ControlLightDirCheckBox = new System.Windows.Forms.CheckBox();
            this.Save_defaultComp = new System.Windows.Forms.Button();
            this.feet_label = new System.Windows.Forms.Label();
            this.feet_updown = new System.Windows.Forms.NumericUpDown();
            this.lowr_label = new System.Windows.Forms.Label();
            this.lowr_updown = new System.Windows.Forms.NumericUpDown();
            this.uppr_label = new System.Windows.Forms.Label();
            this.uppr_updown = new System.Windows.Forms.NumericUpDown();
            this.hair_label = new System.Windows.Forms.Label();
            this.hair_updown = new System.Windows.Forms.NumericUpDown();
            this.berd_label = new System.Windows.Forms.Label();
            this.berd_updown = new System.Windows.Forms.NumericUpDown();
            this.head_label = new System.Windows.Forms.Label();
            this.head_updown = new System.Windows.Forms.NumericUpDown();
            this.StatusBarCheckBox = new System.Windows.Forms.CheckBox();
            this.ErrorConsoleCheckBox = new System.Windows.Forms.CheckBox();
            this.ToolsCameraTabPage = new System.Windows.Forms.TabPage();
            this.CameraPresetsDataGridView = new System.Windows.Forms.DataGridView();
            this.DataGridViewName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataGridViewDelete = new System.Windows.Forms.DataGridViewButtonColumn();
            this.CameraDistanceTextBox = new System.Windows.Forms.TextBox();
            this.CameraSavePresetTextBox = new System.Windows.Forms.TextBox();
            this.label102 = new System.Windows.Forms.Label();
            this.btn_addCameraPreset = new System.Windows.Forms.Button();
            this.label101 = new System.Windows.Forms.Label();
            this.CameraRotationTextBox = new System.Windows.Forms.TextBox();
            this.label100 = new System.Windows.Forms.Label();
            this.CameraPositionTextBox = new System.Windows.Forms.TextBox();
            this.btn_restartCamera = new System.Windows.Forms.Button();
            this.ConsoleTextBox = new CodeWalker.WinForms.TextBoxFix();
            this.ModelsTreeView = new CodeWalker.WinForms.TreeViewFix();
            this.TexturesTreeView = new CodeWalker.WinForms.TreeViewFix();
            this.ConsolePanel.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.ToolsPanel.SuspendLayout();
            this.ToolsTabControl.SuspendLayout();
            this.ToolsPedTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PlaybackSpeedTrackBar)).BeginInit();
            this.ToolsModelsTabPage.SuspendLayout();
            this.ToolsTexturesTabPage.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.ToolsOptionsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.floorUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeOfDayTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.feet_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowr_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.uppr_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.hair_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.berd_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.head_updown)).BeginInit();
            this.ToolsCameraTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CameraPresetsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // ToolsPanelShowButton
            // 
            this.ToolsPanelShowButton.Location = new System.Drawing.Point(15, 15);
            this.ToolsPanelShowButton.Name = "ToolsPanelShowButton";
            this.ToolsPanelShowButton.Size = new System.Drawing.Size(30, 23);
            this.ToolsPanelShowButton.TabIndex = 8;
            this.ToolsPanelShowButton.Text = ">>";
            this.ToolsPanelShowButton.UseVisualStyleBackColor = true;
            this.ToolsPanelShowButton.Click += new System.EventHandler(this.ToolsPanelShowButton_Click);
            // 
            // ToolsPanelHideButton
            // 
            this.ToolsPanelHideButton.Location = new System.Drawing.Point(3, 3);
            this.ToolsPanelHideButton.Name = "ToolsPanelHideButton";
            this.ToolsPanelHideButton.Size = new System.Drawing.Size(30, 23);
            this.ToolsPanelHideButton.TabIndex = 0;
            this.ToolsPanelHideButton.Text = "<<";
            this.ToolsPanelHideButton.UseVisualStyleBackColor = true;
            this.ToolsPanelHideButton.Click += new System.EventHandler(this.ToolsPanelHideButton_Click);
            // 
            // ToolsDragPanel
            // 
            this.ToolsDragPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ToolsDragPanel.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.ToolsDragPanel.Location = new System.Drawing.Point(249, 0);
            this.ToolsDragPanel.Name = "ToolsDragPanel";
            this.ToolsDragPanel.Size = new System.Drawing.Size(4, 666);
            this.ToolsDragPanel.TabIndex = 17;
            this.ToolsDragPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ToolsDragPanel_MouseDown);
            this.ToolsDragPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ToolsDragPanel_MouseMove);
            this.ToolsDragPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ToolsDragPanel_MouseUp);
            // 
            // ConsolePanel
            // 
            this.ConsolePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ConsolePanel.BackColor = System.Drawing.SystemColors.Control;
            this.ConsolePanel.Controls.Add(this.ConsoleTextBox);
            this.ConsolePanel.Location = new System.Drawing.Point(271, 577);
            this.ConsolePanel.Name = "ConsolePanel";
            this.ConsolePanel.Size = new System.Drawing.Size(701, 101);
            this.ConsolePanel.TabIndex = 9;
            this.ConsolePanel.Visible = false;
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel,
            this.MousedLabel,
            this.StatsLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 689);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(984, 22);
            this.StatusStrip.TabIndex = 6;
            this.StatusStrip.Text = "statusStrip1";
            // 
            // StatusLabel
            // 
            this.StatusLabel.BackColor = System.Drawing.SystemColors.Control;
            this.StatusLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(878, 17);
            this.StatusLabel.Spring = true;
            this.StatusLabel.Text = "Initialising";
            this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MousedLabel
            // 
            this.MousedLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MousedLabel.Name = "MousedLabel";
            this.MousedLabel.Size = new System.Drawing.Size(16, 17);
            this.MousedLabel.Text = "   ";
            // 
            // StatsLabel
            // 
            this.StatsLabel.BackColor = System.Drawing.SystemColors.Control;
            this.StatsLabel.Name = "StatsLabel";
            this.StatsLabel.Size = new System.Drawing.Size(75, 17);
            this.StatsLabel.Text = "0 geometries";
            // 
            // StatsUpdateTimer
            // 
            this.StatsUpdateTimer.Enabled = true;
            this.StatsUpdateTimer.Interval = 500;
            this.StatsUpdateTimer.Tick += new System.EventHandler(this.StatsUpdateTimer_Tick);
            // 
            // ToolsPanel
            // 
            this.ToolsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ToolsPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ToolsPanel.Controls.Add(this.ToolsTabControl);
            this.ToolsPanel.Controls.Add(this.ToolsPanelHideButton);
            this.ToolsPanel.Controls.Add(this.ToolsDragPanel);
            this.ToolsPanel.Location = new System.Drawing.Point(12, 12);
            this.ToolsPanel.Name = "ToolsPanel";
            this.ToolsPanel.Size = new System.Drawing.Size(252, 666);
            this.ToolsPanel.TabIndex = 7;
            this.ToolsPanel.Visible = false;
            // 
            // ToolsTabControl
            // 
            this.ToolsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ToolsTabControl.Controls.Add(this.ToolsPedTabPage);
            this.ToolsTabControl.Controls.Add(this.ToolsModelsTabPage);
            this.ToolsTabControl.Controls.Add(this.ToolsTexturesTabPage);
            this.ToolsTabControl.Controls.Add(this.ToolsOptionsTabPage);
            this.ToolsTabControl.Controls.Add(this.ToolsCameraTabPage);
            this.ToolsTabControl.Location = new System.Drawing.Point(2, 30);
            this.ToolsTabControl.Name = "ToolsTabControl";
            this.ToolsTabControl.SelectedIndex = 0;
            this.ToolsTabControl.Size = new System.Drawing.Size(247, 633);
            this.ToolsTabControl.TabIndex = 1;
            // 
            // ToolsPedTabPage
            // 
            this.ToolsPedTabPage.Controls.Add(this.button1);
            this.ToolsPedTabPage.Controls.Add(this.label13);
            this.ToolsPedTabPage.Controls.Add(this.CustomAnimComboBox);
            this.ToolsPedTabPage.Controls.Add(this.PolygonCountText);
            this.ToolsPedTabPage.Controls.Add(this.VertexCountText);
            this.ToolsPedTabPage.Controls.Add(this.label12);
            this.ToolsPedTabPage.Controls.Add(this.label9);
            this.ToolsPedTabPage.Controls.Add(this.label8);
            this.ToolsPedTabPage.Controls.Add(this.PlaybackSpeedTrackBar);
            this.ToolsPedTabPage.Controls.Add(this.PlaybackSpeedLabel);
            this.ToolsPedTabPage.Controls.Add(this.label2);
            this.ToolsPedTabPage.Controls.Add(this.EnableAnimationCheckBox);
            this.ToolsPedTabPage.Controls.Add(this.EnableRootMotionCheckBox);
            this.ToolsPedTabPage.Controls.Add(this.label23);
            this.ToolsPedTabPage.Controls.Add(this.label22);
            this.ToolsPedTabPage.Controls.Add(this.ClipComboBox);
            this.ToolsPedTabPage.Controls.Add(this.label21);
            this.ToolsPedTabPage.Controls.Add(this.ClipDictComboBox);
            this.ToolsPedTabPage.Controls.Add(this.label3);
            this.ToolsPedTabPage.Controls.Add(this.PedNameComboBox);
            this.ToolsPedTabPage.Location = new System.Drawing.Point(4, 22);
            this.ToolsPedTabPage.Name = "ToolsPedTabPage";
            this.ToolsPedTabPage.Size = new System.Drawing.Size(239, 607);
            this.ToolsPedTabPage.TabIndex = 4;
            this.ToolsPedTabPage.Text = "Ped";
            this.ToolsPedTabPage.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(213, 148);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 59;
            this.button1.Text = "+";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.AddCustomAnimButton_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(7, 152);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(45, 13);
            this.label13.TabIndex = 58;
            this.label13.Text = "Custom:";
            // 
            // CustomAnimComboBox
            // 
            this.CustomAnimComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CustomAnimComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.CustomAnimComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.CustomAnimComboBox.FormattingEnabled = true;
            this.CustomAnimComboBox.Location = new System.Drawing.Point(53, 149);
            this.CustomAnimComboBox.Name = "CustomAnimComboBox";
            this.CustomAnimComboBox.Size = new System.Drawing.Size(154, 21);
            this.CustomAnimComboBox.TabIndex = 57;
            this.CustomAnimComboBox.SelectedIndexChanged += new System.EventHandler(this.CustomAnimComboBox_SelectedIndexChanged);
            // 
            // PolygonCountText
            // 
            this.PolygonCountText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PolygonCountText.AutoSize = true;
            this.PolygonCountText.Location = new System.Drawing.Point(89, 580);
            this.PolygonCountText.Name = "PolygonCountText";
            this.PolygonCountText.Size = new System.Drawing.Size(13, 13);
            this.PolygonCountText.TabIndex = 56;
            this.PolygonCountText.Text = "0";
            // 
            // VertexCountText
            // 
            this.VertexCountText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.VertexCountText.AutoSize = true;
            this.VertexCountText.Location = new System.Drawing.Point(89, 557);
            this.VertexCountText.Name = "VertexCountText";
            this.VertexCountText.Size = new System.Drawing.Size(13, 13);
            this.VertexCountText.TabIndex = 55;
            this.VertexCountText.Text = "0";
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 580);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(81, 13);
            this.label12.TabIndex = 54;
            this.label12.Text = "Polygon count: ";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 557);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 13);
            this.label9.TabIndex = 53;
            this.label9.Text = "Vertex count:";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label8.Location = new System.Drawing.Point(2, 529);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(162, 20);
            this.label8.TabIndex = 52;
            this.label8.Text = "Selected drawable:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PlaybackSpeedTrackBar
            // 
            this.PlaybackSpeedTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PlaybackSpeedTrackBar.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.PlaybackSpeedTrackBar.LargeChange = 1;
            this.PlaybackSpeedTrackBar.Location = new System.Drawing.Point(6, 196);
            this.PlaybackSpeedTrackBar.Maximum = 60;
            this.PlaybackSpeedTrackBar.Name = "PlaybackSpeedTrackBar";
            this.PlaybackSpeedTrackBar.Size = new System.Drawing.Size(222, 45);
            this.PlaybackSpeedTrackBar.TabIndex = 51;
            this.PlaybackSpeedTrackBar.TickFrequency = 6;
            this.PlaybackSpeedTrackBar.Value = 60;
            this.PlaybackSpeedTrackBar.Scroll += new System.EventHandler(this.PlaybackSpeedTrackBar_Scroll);
            // 
            // PlaybackSpeedLabel
            // 
            this.PlaybackSpeedLabel.AutoSize = true;
            this.PlaybackSpeedLabel.Location = new System.Drawing.Point(89, 178);
            this.PlaybackSpeedLabel.Name = "PlaybackSpeedLabel";
            this.PlaybackSpeedLabel.Size = new System.Drawing.Size(13, 13);
            this.PlaybackSpeedLabel.TabIndex = 50;
            this.PlaybackSpeedLabel.Text = "1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 178);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 49;
            this.label2.Text = "Playback speed:";
            // 
            // EnableAnimationCheckBox
            // 
            this.EnableAnimationCheckBox.AutoSize = true;
            this.EnableAnimationCheckBox.Location = new System.Drawing.Point(6, 75);
            this.EnableAnimationCheckBox.Name = "EnableAnimationCheckBox";
            this.EnableAnimationCheckBox.Size = new System.Drawing.Size(107, 17);
            this.EnableAnimationCheckBox.TabIndex = 47;
            this.EnableAnimationCheckBox.Text = "Enable animation";
            this.EnableAnimationCheckBox.UseVisualStyleBackColor = true;
            this.EnableAnimationCheckBox.CheckedChanged += new System.EventHandler(this.EnableAnimationCheckBox_CheckedChanged);
            // 
            // EnableRootMotionCheckBox
            // 
            this.EnableRootMotionCheckBox.AutoSize = true;
            this.EnableRootMotionCheckBox.Location = new System.Drawing.Point(114, 75);
            this.EnableRootMotionCheckBox.Name = "EnableRootMotionCheckBox";
            this.EnableRootMotionCheckBox.Size = new System.Drawing.Size(114, 17);
            this.EnableRootMotionCheckBox.TabIndex = 32;
            this.EnableRootMotionCheckBox.Text = "Enable root motion";
            this.EnableRootMotionCheckBox.UseVisualStyleBackColor = true;
            this.EnableRootMotionCheckBox.CheckedChanged += new System.EventHandler(this.EnableRootMotionCheckBox_CheckedChanged);
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label23.Location = new System.Drawing.Point(2, 54);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(94, 20);
            this.label23.TabIndex = 31;
            this.label23.Text = "Animation:";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(23, 126);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(27, 13);
            this.label22.TabIndex = 30;
            this.label22.Text = "Clip:";
            // 
            // ClipComboBox
            // 
            this.ClipComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ClipComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.ClipComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.ClipComboBox.FormattingEnabled = true;
            this.ClipComboBox.Location = new System.Drawing.Point(53, 123);
            this.ClipComboBox.Name = "ClipComboBox";
            this.ClipComboBox.Size = new System.Drawing.Size(182, 21);
            this.ClipComboBox.TabIndex = 29;
            this.ClipComboBox.TextChanged += new System.EventHandler(this.ClipComboBox_TextChanged);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(1, 99);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(49, 13);
            this.label21.TabIndex = 28;
            this.label21.Text = "Clip Dict:";
            // 
            // ClipDictComboBox
            // 
            this.ClipDictComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ClipDictComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.ClipDictComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.ClipDictComboBox.FormattingEnabled = true;
            this.ClipDictComboBox.Location = new System.Drawing.Point(53, 96);
            this.ClipDictComboBox.Name = "ClipDictComboBox";
            this.ClipDictComboBox.Size = new System.Drawing.Size(182, 21);
            this.ClipDictComboBox.TabIndex = 27;
            this.ClipDictComboBox.TextChanged += new System.EventHandler(this.ClipDictComboBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Ped:";
            // 
            // PedNameComboBox
            // 
            this.PedNameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PedNameComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.PedNameComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.PedNameComboBox.FormattingEnabled = true;
            this.PedNameComboBox.Location = new System.Drawing.Point(54, 7);
            this.PedNameComboBox.Name = "PedNameComboBox";
            this.PedNameComboBox.Size = new System.Drawing.Size(182, 21);
            this.PedNameComboBox.TabIndex = 0;
            this.PedNameComboBox.SelectedIndexChanged += new System.EventHandler(this.PedNameComboBox_SelectedIndexChanged);
            // 
            // ToolsModelsTabPage
            // 
            this.ToolsModelsTabPage.Controls.Add(this.ModelsTreeView);
            this.ToolsModelsTabPage.Location = new System.Drawing.Point(4, 22);
            this.ToolsModelsTabPage.Name = "ToolsModelsTabPage";
            this.ToolsModelsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.ToolsModelsTabPage.Size = new System.Drawing.Size(239, 607);
            this.ToolsModelsTabPage.TabIndex = 0;
            this.ToolsModelsTabPage.Text = "Models";
            this.ToolsModelsTabPage.UseVisualStyleBackColor = true;
            // 
            // ToolsTexturesTabPage
            // 
            this.ToolsTexturesTabPage.Controls.Add(this.groupBox1);
            this.ToolsTexturesTabPage.Controls.Add(this.TexturesTreeView);
            this.ToolsTexturesTabPage.Location = new System.Drawing.Point(4, 22);
            this.ToolsTexturesTabPage.Name = "ToolsTexturesTabPage";
            this.ToolsTexturesTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.ToolsTexturesTabPage.Size = new System.Drawing.Size(239, 607);
            this.ToolsTexturesTabPage.TabIndex = 1;
            this.ToolsTexturesTabPage.Text = "Textures";
            this.ToolsTexturesTabPage.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.diffuseRadio);
            this.groupBox1.Controls.Add(this.liveTxtButton);
            this.groupBox1.Controls.Add(this.specularRadio);
            this.groupBox1.Controls.Add(this.normalRadio);
            this.groupBox1.Location = new System.Drawing.Point(6, 460);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(227, 141);
            this.groupBox1.TabIndex = 75;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Live texture preview";
            // 
            // diffuseRadio
            // 
            this.diffuseRadio.AutoSize = true;
            this.diffuseRadio.Checked = true;
            this.diffuseRadio.Location = new System.Drawing.Point(6, 19);
            this.diffuseRadio.Name = "diffuseRadio";
            this.diffuseRadio.Size = new System.Drawing.Size(58, 17);
            this.diffuseRadio.TabIndex = 71;
            this.diffuseRadio.TabStop = true;
            this.diffuseRadio.Text = "Diffuse";
            this.diffuseRadio.UseVisualStyleBackColor = true;
            this.diffuseRadio.CheckedChanged += new System.EventHandler(this.liveTexture_CheckedChanged);
            // 
            // liveTxtButton
            // 
            this.liveTxtButton.Location = new System.Drawing.Point(6, 109);
            this.liveTxtButton.Name = "liveTxtButton";
            this.liveTxtButton.Size = new System.Drawing.Size(94, 26);
            this.liveTxtButton.TabIndex = 69;
            this.liveTxtButton.Text = "Enable";
            this.liveTxtButton.UseVisualStyleBackColor = true;
            this.liveTxtButton.Click += new System.EventHandler(this.LiveTexturePreview_Click);
            // 
            // specularRadio
            // 
            this.specularRadio.AutoSize = true;
            this.specularRadio.Location = new System.Drawing.Point(6, 65);
            this.specularRadio.Name = "specularRadio";
            this.specularRadio.Size = new System.Drawing.Size(67, 17);
            this.specularRadio.TabIndex = 73;
            this.specularRadio.Text = "Specular";
            this.specularRadio.UseVisualStyleBackColor = true;
            this.specularRadio.CheckedChanged += new System.EventHandler(this.liveTexture_CheckedChanged);
            // 
            // normalRadio
            // 
            this.normalRadio.AutoSize = true;
            this.normalRadio.Location = new System.Drawing.Point(6, 42);
            this.normalRadio.Name = "normalRadio";
            this.normalRadio.Size = new System.Drawing.Size(58, 17);
            this.normalRadio.TabIndex = 72;
            this.normalRadio.Text = "Normal";
            this.normalRadio.UseVisualStyleBackColor = true;
            this.normalRadio.CheckedChanged += new System.EventHandler(this.liveTexture_CheckedChanged);
            // 
            // ToolsOptionsTabPage
            // 
            this.ToolsOptionsTabPage.Controls.Add(this.OnlySelectedCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.AutoRotatePedCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.floorUpDown);
            this.ToolsOptionsTabPage.Controls.Add(this.floorCheckbox);
            this.ToolsOptionsTabPage.Controls.Add(this.label7);
            this.ToolsOptionsTabPage.Controls.Add(this.label6);
            this.ToolsOptionsTabPage.Controls.Add(this.label5);
            this.ToolsOptionsTabPage.Controls.Add(this.label4);
            this.ToolsOptionsTabPage.Controls.Add(this.label1);
            this.ToolsOptionsTabPage.Controls.Add(this.WireframeCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.SkeletonsCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.ShadowsCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.HDRRenderingCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.RenderModeComboBox);
            this.ToolsOptionsTabPage.Controls.Add(this.label11);
            this.ToolsOptionsTabPage.Controls.Add(this.TextureSamplerComboBox);
            this.ToolsOptionsTabPage.Controls.Add(this.TextureCoordsComboBox);
            this.ToolsOptionsTabPage.Controls.Add(this.label10);
            this.ToolsOptionsTabPage.Controls.Add(this.label14);
            this.ToolsOptionsTabPage.Controls.Add(this.TimeOfDayLabel);
            this.ToolsOptionsTabPage.Controls.Add(this.label19);
            this.ToolsOptionsTabPage.Controls.Add(this.TimeOfDayTrackBar);
            this.ToolsOptionsTabPage.Controls.Add(this.ControlLightDirCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.Save_defaultComp);
            this.ToolsOptionsTabPage.Controls.Add(this.feet_label);
            this.ToolsOptionsTabPage.Controls.Add(this.feet_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.lowr_label);
            this.ToolsOptionsTabPage.Controls.Add(this.lowr_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.uppr_label);
            this.ToolsOptionsTabPage.Controls.Add(this.uppr_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.hair_label);
            this.ToolsOptionsTabPage.Controls.Add(this.hair_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.berd_label);
            this.ToolsOptionsTabPage.Controls.Add(this.berd_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.head_label);
            this.ToolsOptionsTabPage.Controls.Add(this.head_updown);
            this.ToolsOptionsTabPage.Controls.Add(this.StatusBarCheckBox);
            this.ToolsOptionsTabPage.Controls.Add(this.ErrorConsoleCheckBox);
            this.ToolsOptionsTabPage.Location = new System.Drawing.Point(4, 22);
            this.ToolsOptionsTabPage.Name = "ToolsOptionsTabPage";
            this.ToolsOptionsTabPage.Size = new System.Drawing.Size(239, 607);
            this.ToolsOptionsTabPage.TabIndex = 3;
            this.ToolsOptionsTabPage.Text = "Options";
            this.ToolsOptionsTabPage.UseVisualStyleBackColor = true;
            // 
            // OnlySelectedCheckBox
            // 
            this.OnlySelectedCheckBox.AutoSize = true;
            this.OnlySelectedCheckBox.Location = new System.Drawing.Point(12, 369);
            this.OnlySelectedCheckBox.Name = "OnlySelectedCheckBox";
            this.OnlySelectedCheckBox.Size = new System.Drawing.Size(140, 17);
            this.OnlySelectedCheckBox.TabIndex = 68;
            this.OnlySelectedCheckBox.Text = "Only Selected Drawable";
            this.OnlySelectedCheckBox.UseVisualStyleBackColor = true;
            this.OnlySelectedCheckBox.CheckedChanged += new System.EventHandler(this.OnlySelectedCheckBox_CheckedChanged);
            // 
            // AutoRotatePedCheckBox
            // 
            this.AutoRotatePedCheckBox.AutoSize = true;
            this.AutoRotatePedCheckBox.Location = new System.Drawing.Point(12, 346);
            this.AutoRotatePedCheckBox.Name = "AutoRotatePedCheckBox";
            this.AutoRotatePedCheckBox.Size = new System.Drawing.Size(100, 17);
            this.AutoRotatePedCheckBox.TabIndex = 67;
            this.AutoRotatePedCheckBox.Text = "Auto rotate Ped";
            this.AutoRotatePedCheckBox.UseVisualStyleBackColor = true;
            this.AutoRotatePedCheckBox.CheckedChanged += new System.EventHandler(this.AutoRotatePedCheckBox_CheckedChanged);
            // 
            // floorUpDown
            // 
            this.floorUpDown.DecimalPlaces = 1;
            this.floorUpDown.Enabled = false;
            this.floorUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.floorUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.floorUpDown.Location = new System.Drawing.Point(150, 272);
            this.floorUpDown.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.floorUpDown.Name = "floorUpDown";
            this.floorUpDown.Size = new System.Drawing.Size(47, 22);
            this.floorUpDown.TabIndex = 68;
            this.floorUpDown.Tag = "6";
            // 
            // floorCheckbox
            // 
            this.floorCheckbox.AutoSize = true;
            this.floorCheckbox.Location = new System.Drawing.Point(136, 254);
            this.floorCheckbox.Name = "floorCheckbox";
            this.floorCheckbox.Size = new System.Drawing.Size(82, 17);
            this.floorCheckbox.TabIndex = 67;
            this.floorCheckbox.Text = "Enable floor";
            this.floorCheckbox.UseVisualStyleBackColor = true;
            this.floorCheckbox.CheckedChanged += new System.EventHandler(this.FloorCheckbox_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.DimGray;
            this.label7.Location = new System.Drawing.Point(116, 53);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 13);
            this.label7.TabIndex = 66;
            this.label7.Text = "(0 for empty model)";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.Color.DimGray;
            this.label6.Location = new System.Drawing.Point(116, 82);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 13);
            this.label6.TabIndex = 65;
            this.label6.Text = "(0 for empty model)";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.DimGray;
            this.label5.Location = new System.Drawing.Point(116, 110);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(102, 13);
            this.label5.TabIndex = 64;
            this.label5.Text = "(13 for empty model)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.DimGray;
            this.label4.Location = new System.Drawing.Point(116, 137);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 13);
            this.label4.TabIndex = 63;
            this.label4.Text = "(13 for empty model)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.DimGray;
            this.label1.Location = new System.Drawing.Point(116, 165);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 62;
            this.label1.Text = "(12 for empty model)";
            // 
            // WireframeCheckBox
            // 
            this.WireframeCheckBox.AutoSize = true;
            this.WireframeCheckBox.Location = new System.Drawing.Point(12, 323);
            this.WireframeCheckBox.Name = "WireframeCheckBox";
            this.WireframeCheckBox.Size = new System.Drawing.Size(74, 17);
            this.WireframeCheckBox.TabIndex = 60;
            this.WireframeCheckBox.Text = "Wireframe";
            this.WireframeCheckBox.UseVisualStyleBackColor = true;
            this.WireframeCheckBox.CheckedChanged += new System.EventHandler(this.WireframeCheckBox_CheckedChanged);
            // 
            // SkeletonsCheckBox
            // 
            this.SkeletonsCheckBox.AutoSize = true;
            this.SkeletonsCheckBox.Location = new System.Drawing.Point(12, 300);
            this.SkeletonsCheckBox.Name = "SkeletonsCheckBox";
            this.SkeletonsCheckBox.Size = new System.Drawing.Size(103, 17);
            this.SkeletonsCheckBox.TabIndex = 59;
            this.SkeletonsCheckBox.Text = "Show Skeletons";
            this.SkeletonsCheckBox.UseVisualStyleBackColor = true;
            this.SkeletonsCheckBox.CheckedChanged += new System.EventHandler(this.SkeletonsCheckBox_CheckedChanged);
            // 
            // ShadowsCheckBox
            // 
            this.ShadowsCheckBox.AutoSize = true;
            this.ShadowsCheckBox.Checked = true;
            this.ShadowsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShadowsCheckBox.Location = new System.Drawing.Point(12, 277);
            this.ShadowsCheckBox.Name = "ShadowsCheckBox";
            this.ShadowsCheckBox.Size = new System.Drawing.Size(70, 17);
            this.ShadowsCheckBox.TabIndex = 58;
            this.ShadowsCheckBox.Text = "Shadows";
            this.ShadowsCheckBox.UseVisualStyleBackColor = true;
            this.ShadowsCheckBox.CheckedChanged += new System.EventHandler(this.ShadowsCheckBox_CheckedChanged);
            // 
            // HDRRenderingCheckBox
            // 
            this.HDRRenderingCheckBox.AutoSize = true;
            this.HDRRenderingCheckBox.Location = new System.Drawing.Point(12, 254);
            this.HDRRenderingCheckBox.Name = "HDRRenderingCheckBox";
            this.HDRRenderingCheckBox.Size = new System.Drawing.Size(97, 17);
            this.HDRRenderingCheckBox.TabIndex = 57;
            this.HDRRenderingCheckBox.Text = "HDR rendering";
            this.HDRRenderingCheckBox.UseVisualStyleBackColor = true;
            this.HDRRenderingCheckBox.CheckedChanged += new System.EventHandler(this.HDRRenderingCheckBox_CheckedChanged);
            // 
            // RenderModeComboBox
            // 
            this.RenderModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RenderModeComboBox.FormattingEnabled = true;
            this.RenderModeComboBox.Items.AddRange(new object[] {
            "Default",
            "Single texture",
            "Vertex normals",
            "Vertex tangents",
            "Vertex colour 1",
            "Vertex colour 2",
            "Texture coord 1",
            "Texture coord 2",
            "Texture coord 3"});
            this.RenderModeComboBox.Location = new System.Drawing.Point(83, 400);
            this.RenderModeComboBox.Name = "RenderModeComboBox";
            this.RenderModeComboBox.Size = new System.Drawing.Size(114, 21);
            this.RenderModeComboBox.TabIndex = 52;
            this.RenderModeComboBox.SelectedIndexChanged += new System.EventHandler(this.RenderModeComboBox_SelectedIndexChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(7, 430);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(67, 13);
            this.label11.TabIndex = 53;
            this.label11.Text = "Tex sampler:";
            // 
            // TextureSamplerComboBox
            // 
            this.TextureSamplerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TextureSamplerComboBox.Enabled = false;
            this.TextureSamplerComboBox.FormattingEnabled = true;
            this.TextureSamplerComboBox.Location = new System.Drawing.Point(83, 427);
            this.TextureSamplerComboBox.Name = "TextureSamplerComboBox";
            this.TextureSamplerComboBox.Size = new System.Drawing.Size(114, 21);
            this.TextureSamplerComboBox.TabIndex = 54;
            this.TextureSamplerComboBox.SelectedIndexChanged += new System.EventHandler(this.TextureSamplerComboBox_SelectedIndexChanged);
            // 
            // TextureCoordsComboBox
            // 
            this.TextureCoordsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TextureCoordsComboBox.Enabled = false;
            this.TextureCoordsComboBox.FormattingEnabled = true;
            this.TextureCoordsComboBox.Items.AddRange(new object[] {
            "Texture coord 1",
            "Texture coord 2",
            "Texture coord 3"});
            this.TextureCoordsComboBox.Location = new System.Drawing.Point(83, 454);
            this.TextureCoordsComboBox.Name = "TextureCoordsComboBox";
            this.TextureCoordsComboBox.Size = new System.Drawing.Size(114, 21);
            this.TextureCoordsComboBox.TabIndex = 56;
            this.TextureCoordsComboBox.SelectedIndexChanged += new System.EventHandler(this.TextureCoordsComboBox_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(7, 403);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 51;
            this.label10.Text = "Render mode:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(7, 457);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(63, 13);
            this.label14.TabIndex = 55;
            this.label14.Text = "Tex coords:";
            // 
            // TimeOfDayLabel
            // 
            this.TimeOfDayLabel.AutoSize = true;
            this.TimeOfDayLabel.Location = new System.Drawing.Point(80, 507);
            this.TimeOfDayLabel.Name = "TimeOfDayLabel";
            this.TimeOfDayLabel.Size = new System.Drawing.Size(34, 13);
            this.TimeOfDayLabel.TabIndex = 49;
            this.TimeOfDayLabel.Text = "12:00";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(9, 507);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(65, 13);
            this.label19.TabIndex = 48;
            this.label19.Text = "Time of day:";
            // 
            // TimeOfDayTrackBar
            // 
            this.TimeOfDayTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TimeOfDayTrackBar.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.TimeOfDayTrackBar.LargeChange = 60;
            this.TimeOfDayTrackBar.Location = new System.Drawing.Point(10, 536);
            this.TimeOfDayTrackBar.Maximum = 1440;
            this.TimeOfDayTrackBar.Name = "TimeOfDayTrackBar";
            this.TimeOfDayTrackBar.Size = new System.Drawing.Size(222, 45);
            this.TimeOfDayTrackBar.TabIndex = 50;
            this.TimeOfDayTrackBar.TickFrequency = 60;
            this.TimeOfDayTrackBar.Value = 720;
            this.TimeOfDayTrackBar.Scroll += new System.EventHandler(this.TimeOfDayTrackBar_Scroll);
            // 
            // ControlLightDirCheckBox
            // 
            this.ControlLightDirCheckBox.AutoSize = true;
            this.ControlLightDirCheckBox.Location = new System.Drawing.Point(11, 487);
            this.ControlLightDirCheckBox.Name = "ControlLightDirCheckBox";
            this.ControlLightDirCheckBox.Size = new System.Drawing.Size(124, 17);
            this.ControlLightDirCheckBox.TabIndex = 47;
            this.ControlLightDirCheckBox.Text = "Control light direction";
            this.ControlLightDirCheckBox.UseVisualStyleBackColor = true;
            this.ControlLightDirCheckBox.CheckedChanged += new System.EventHandler(this.ControlLightDirCheckBox_CheckedChanged);
            // 
            // Save_defaultComp
            // 
            this.Save_defaultComp.Location = new System.Drawing.Point(10, 189);
            this.Save_defaultComp.Name = "Save_defaultComp";
            this.Save_defaultComp.Size = new System.Drawing.Size(94, 26);
            this.Save_defaultComp.TabIndex = 38;
            this.Save_defaultComp.Text = "Save as default";
            this.Save_defaultComp.UseVisualStyleBackColor = true;
            this.Save_defaultComp.Click += new System.EventHandler(this.Save_defaultComp_Click);
            // 
            // feet_label
            // 
            this.feet_label.AutoSize = true;
            this.feet_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.feet_label.Location = new System.Drawing.Point(13, 163);
            this.feet_label.Name = "feet_label";
            this.feet_label.Size = new System.Drawing.Size(42, 16);
            this.feet_label.TabIndex = 37;
            this.feet_label.Text = "FEET";
            // 
            // feet_updown
            // 
            this.feet_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.feet_updown.Location = new System.Drawing.Point(63, 161);
            this.feet_updown.Name = "feet_updown";
            this.feet_updown.Size = new System.Drawing.Size(47, 22);
            this.feet_updown.TabIndex = 36;
            this.feet_updown.Tag = "6";
            this.feet_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // lowr_label
            // 
            this.lowr_label.AutoSize = true;
            this.lowr_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lowr_label.Location = new System.Drawing.Point(13, 135);
            this.lowr_label.Name = "lowr_label";
            this.lowr_label.Size = new System.Drawing.Size(47, 16);
            this.lowr_label.TabIndex = 35;
            this.lowr_label.Text = "LOWR";
            // 
            // lowr_updown
            // 
            this.lowr_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lowr_updown.Location = new System.Drawing.Point(63, 133);
            this.lowr_updown.Name = "lowr_updown";
            this.lowr_updown.Size = new System.Drawing.Size(47, 22);
            this.lowr_updown.TabIndex = 34;
            this.lowr_updown.Tag = "4";
            this.lowr_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // uppr_label
            // 
            this.uppr_label.AutoSize = true;
            this.uppr_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.uppr_label.Location = new System.Drawing.Point(13, 107);
            this.uppr_label.Name = "uppr_label";
            this.uppr_label.Size = new System.Drawing.Size(45, 16);
            this.uppr_label.TabIndex = 33;
            this.uppr_label.Text = "UPPR";
            // 
            // uppr_updown
            // 
            this.uppr_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.uppr_updown.Location = new System.Drawing.Point(63, 105);
            this.uppr_updown.Name = "uppr_updown";
            this.uppr_updown.Size = new System.Drawing.Size(47, 22);
            this.uppr_updown.TabIndex = 32;
            this.uppr_updown.Tag = "3";
            this.uppr_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // hair_label
            // 
            this.hair_label.AutoSize = true;
            this.hair_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.hair_label.Location = new System.Drawing.Point(13, 79);
            this.hair_label.Name = "hair_label";
            this.hair_label.Size = new System.Drawing.Size(39, 16);
            this.hair_label.TabIndex = 31;
            this.hair_label.Text = "HAIR";
            // 
            // hair_updown
            // 
            this.hair_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.hair_updown.Location = new System.Drawing.Point(63, 77);
            this.hair_updown.Name = "hair_updown";
            this.hair_updown.Size = new System.Drawing.Size(47, 22);
            this.hair_updown.TabIndex = 30;
            this.hair_updown.Tag = "2";
            this.hair_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // berd_label
            // 
            this.berd_label.AutoSize = true;
            this.berd_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.berd_label.Location = new System.Drawing.Point(13, 51);
            this.berd_label.Name = "berd_label";
            this.berd_label.Size = new System.Drawing.Size(45, 16);
            this.berd_label.TabIndex = 29;
            this.berd_label.Text = "BERD";
            // 
            // berd_updown
            // 
            this.berd_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.berd_updown.Location = new System.Drawing.Point(63, 49);
            this.berd_updown.Name = "berd_updown";
            this.berd_updown.Size = new System.Drawing.Size(47, 22);
            this.berd_updown.TabIndex = 28;
            this.berd_updown.Tag = "1";
            this.berd_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // head_label
            // 
            this.head_label.AutoSize = true;
            this.head_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.head_label.Location = new System.Drawing.Point(13, 23);
            this.head_label.Name = "head_label";
            this.head_label.Size = new System.Drawing.Size(45, 16);
            this.head_label.TabIndex = 27;
            this.head_label.Text = "HEAD";
            // 
            // head_updown
            // 
            this.head_updown.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.head_updown.Location = new System.Drawing.Point(63, 21);
            this.head_updown.Name = "head_updown";
            this.head_updown.Size = new System.Drawing.Size(47, 22);
            this.head_updown.TabIndex = 25;
            this.head_updown.Tag = "0";
            this.head_updown.ValueChanged += new System.EventHandler(this.OptionsComponent_UpDown_ValueChanged);
            // 
            // StatusBarCheckBox
            // 
            this.StatusBarCheckBox.AutoSize = true;
            this.StatusBarCheckBox.Checked = true;
            this.StatusBarCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.StatusBarCheckBox.Location = new System.Drawing.Point(3, 587);
            this.StatusBarCheckBox.Name = "StatusBarCheckBox";
            this.StatusBarCheckBox.Size = new System.Drawing.Size(74, 17);
            this.StatusBarCheckBox.TabIndex = 23;
            this.StatusBarCheckBox.Text = "Status bar";
            this.StatusBarCheckBox.UseVisualStyleBackColor = true;
            this.StatusBarCheckBox.CheckedChanged += new System.EventHandler(this.StatusBarCheckBox_CheckedChanged);
            // 
            // ErrorConsoleCheckBox
            // 
            this.ErrorConsoleCheckBox.AutoSize = true;
            this.ErrorConsoleCheckBox.Location = new System.Drawing.Point(148, 587);
            this.ErrorConsoleCheckBox.Name = "ErrorConsoleCheckBox";
            this.ErrorConsoleCheckBox.Size = new System.Drawing.Size(88, 17);
            this.ErrorConsoleCheckBox.TabIndex = 24;
            this.ErrorConsoleCheckBox.Text = "Error console";
            this.ErrorConsoleCheckBox.UseVisualStyleBackColor = true;
            this.ErrorConsoleCheckBox.CheckedChanged += new System.EventHandler(this.ErrorConsoleCheckBox_CheckedChanged);
            // 
            // ToolsCameraTabPage
            // 
            this.ToolsCameraTabPage.Controls.Add(this.CameraPresetsDataGridView);
            this.ToolsCameraTabPage.Controls.Add(this.CameraDistanceTextBox);
            this.ToolsCameraTabPage.Controls.Add(this.CameraSavePresetTextBox);
            this.ToolsCameraTabPage.Controls.Add(this.label102);
            this.ToolsCameraTabPage.Controls.Add(this.btn_addCameraPreset);
            this.ToolsCameraTabPage.Controls.Add(this.label101);
            this.ToolsCameraTabPage.Controls.Add(this.CameraRotationTextBox);
            this.ToolsCameraTabPage.Controls.Add(this.label100);
            this.ToolsCameraTabPage.Controls.Add(this.CameraPositionTextBox);
            this.ToolsCameraTabPage.Controls.Add(this.btn_restartCamera);
            this.ToolsCameraTabPage.Location = new System.Drawing.Point(4, 22);
            this.ToolsCameraTabPage.Name = "ToolsCameraTabPage";
            this.ToolsCameraTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.ToolsCameraTabPage.Size = new System.Drawing.Size(239, 607);
            this.ToolsCameraTabPage.TabIndex = 5;
            this.ToolsCameraTabPage.Text = "Camera";
            this.ToolsCameraTabPage.UseVisualStyleBackColor = true;
            // 
            // CameraPresetsDataGridView
            // 
            this.CameraPresetsDataGridView.AllowUserToAddRows = false;
            this.CameraPresetsDataGridView.AllowUserToDeleteRows = false;
            this.CameraPresetsDataGridView.AllowUserToResizeColumns = false;
            this.CameraPresetsDataGridView.AllowUserToResizeRows = false;
            this.CameraPresetsDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.CameraPresetsDataGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.CameraPresetsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.CameraPresetsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.CameraPresetsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DataGridViewName,
            this.DataGridViewDelete});
            this.CameraPresetsDataGridView.Location = new System.Drawing.Point(9, 90);
            this.CameraPresetsDataGridView.MultiSelect = false;
            this.CameraPresetsDataGridView.Name = "CameraPresetsDataGridView";
            this.CameraPresetsDataGridView.ReadOnly = true;
            this.CameraPresetsDataGridView.RowHeadersVisible = false;
            this.CameraPresetsDataGridView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.CameraPresetsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.CameraPresetsDataGridView.ShowCellErrors = false;
            this.CameraPresetsDataGridView.ShowCellToolTips = false;
            this.CameraPresetsDataGridView.ShowEditingIcon = false;
            this.CameraPresetsDataGridView.ShowRowErrors = false;
            this.CameraPresetsDataGridView.Size = new System.Drawing.Size(224, 413);
            this.CameraPresetsDataGridView.TabIndex = 82;
            this.CameraPresetsDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CameraPresetsDataGridView_CellClick);
            this.CameraPresetsDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CameraPresetsDataGridView_CellContentClick);
            // 
            // DataGridViewName
            // 
            this.DataGridViewName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DataGridViewName.HeaderText = "Name";
            this.DataGridViewName.Name = "DataGridViewName";
            this.DataGridViewName.ReadOnly = true;
            this.DataGridViewName.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // DataGridViewDelete
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Red;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(1);
            this.DataGridViewDelete.DefaultCellStyle = dataGridViewCellStyle2;
            this.DataGridViewDelete.HeaderText = "";
            this.DataGridViewDelete.Name = "DataGridViewDelete";
            this.DataGridViewDelete.ReadOnly = true;
            this.DataGridViewDelete.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.DataGridViewDelete.Width = 20;
            // 
            // CameraDistanceTextBox
            // 
            this.CameraDistanceTextBox.AccessibleName = "CameraDistanceTextBox";
            this.CameraDistanceTextBox.Location = new System.Drawing.Point(59, 64);
            this.CameraDistanceTextBox.Name = "CameraDistanceTextBox";
            this.CameraDistanceTextBox.Size = new System.Drawing.Size(174, 20);
            this.CameraDistanceTextBox.TabIndex = 81;
            this.CameraDistanceTextBox.TextChanged += new System.EventHandler(this.CameraDistanceTextBox_TextChanged);
            // 
            // CameraSavePresetTextBox
            // 
            this.CameraSavePresetTextBox.AccessibleName = "CameraSavePresetTextBox";
            this.CameraSavePresetTextBox.Location = new System.Drawing.Point(109, 515);
            this.CameraSavePresetTextBox.Name = "CameraSavePresetTextBox";
            this.CameraSavePresetTextBox.Size = new System.Drawing.Size(124, 20);
            this.CameraSavePresetTextBox.TabIndex = 80;
            // 
            // label102
            // 
            this.label102.AutoSize = true;
            this.label102.Location = new System.Drawing.Point(8, 70);
            this.label102.Name = "label102";
            this.label102.Size = new System.Drawing.Size(52, 13);
            this.label102.TabIndex = 75;
            this.label102.Text = "Distance:";
            // 
            // btn_addCameraPreset
            // 
            this.btn_addCameraPreset.Location = new System.Drawing.Point(9, 509);
            this.btn_addCameraPreset.Name = "btn_addCameraPreset";
            this.btn_addCameraPreset.Size = new System.Drawing.Size(94, 26);
            this.btn_addCameraPreset.TabIndex = 74;
            this.btn_addCameraPreset.Text = "Add preset";
            this.btn_addCameraPreset.UseVisualStyleBackColor = true;
            this.btn_addCameraPreset.Click += new System.EventHandler(this.btn_addCameraPreset_Click);
            // 
            // label101
            // 
            this.label101.AutoSize = true;
            this.label101.Location = new System.Drawing.Point(9, 42);
            this.label101.Name = "label101";
            this.label101.Size = new System.Drawing.Size(50, 13);
            this.label101.TabIndex = 73;
            this.label101.Text = "Rotation:";
            // 
            // CameraRotationTextBox
            // 
            this.CameraRotationTextBox.AccessibleName = "CameraRotationTextBox";
            this.CameraRotationTextBox.Location = new System.Drawing.Point(59, 36);
            this.CameraRotationTextBox.Name = "CameraRotationTextBox";
            this.CameraRotationTextBox.Size = new System.Drawing.Size(174, 20);
            this.CameraRotationTextBox.TabIndex = 72;
            this.CameraRotationTextBox.TextChanged += new System.EventHandler(this.CameraRotationTextBox_TextChanged);
            // 
            // label100
            // 
            this.label100.AutoSize = true;
            this.label100.Location = new System.Drawing.Point(9, 16);
            this.label100.Name = "label100";
            this.label100.Size = new System.Drawing.Size(47, 13);
            this.label100.TabIndex = 71;
            this.label100.Text = "Position:";
            // 
            // CameraPositionTextBox
            // 
            this.CameraPositionTextBox.AccessibleName = "CameraPositionTextBox";
            this.CameraPositionTextBox.Location = new System.Drawing.Point(59, 10);
            this.CameraPositionTextBox.Name = "CameraPositionTextBox";
            this.CameraPositionTextBox.Size = new System.Drawing.Size(174, 20);
            this.CameraPositionTextBox.TabIndex = 70;
            this.CameraPositionTextBox.TextChanged += new System.EventHandler(this.CameraPositionTextBox_TextChanged);
            // 
            // btn_restartCamera
            // 
            this.btn_restartCamera.Location = new System.Drawing.Point(9, 541);
            this.btn_restartCamera.Name = "btn_restartCamera";
            this.btn_restartCamera.Size = new System.Drawing.Size(94, 26);
            this.btn_restartCamera.TabIndex = 69;
            this.btn_restartCamera.Text = "Reset Camera";
            this.btn_restartCamera.UseVisualStyleBackColor = true;
            this.btn_restartCamera.Click += new System.EventHandler(this.RestartCamera_Click);
            // 
            // ConsoleTextBox
            // 
            this.ConsoleTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ConsoleTextBox.Location = new System.Drawing.Point(3, 3);
            this.ConsoleTextBox.Multiline = true;
            this.ConsoleTextBox.Name = "ConsoleTextBox";
            this.ConsoleTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ConsoleTextBox.Size = new System.Drawing.Size(695, 95);
            this.ConsoleTextBox.TabIndex = 0;
            // 
            // ModelsTreeView
            // 
            this.ModelsTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ModelsTreeView.CheckBoxes = true;
            this.ModelsTreeView.Location = new System.Drawing.Point(0, 3);
            this.ModelsTreeView.Name = "ModelsTreeView";
            this.ModelsTreeView.ShowRootLines = false;
            this.ModelsTreeView.Size = new System.Drawing.Size(268, 604);
            this.ModelsTreeView.TabIndex = 2;
            this.ModelsTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.ModelsTreeView_AfterCheck);
            this.ModelsTreeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.ModelsTreeView_NodeMouseDoubleClick);
            this.ModelsTreeView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ModelsTreeView_KeyPress);
            // 
            // TexturesTreeView
            // 
            this.TexturesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TexturesTreeView.Location = new System.Drawing.Point(0, 3);
            this.TexturesTreeView.Name = "TexturesTreeView";
            this.TexturesTreeView.ShowRootLines = false;
            this.TexturesTreeView.Size = new System.Drawing.Size(268, 451);
            this.TexturesTreeView.TabIndex = 1;
            // 
            // CustomPedsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.MidnightBlue;
            this.ClientSize = new System.Drawing.Size(984, 711);
            this.Controls.Add(this.ConsolePanel);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ToolsPanel);
            this.Controls.Add(this.ToolsPanelShowButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "CustomPedsForm";
            this.Text = " ";
            this.Deactivate += new System.EventHandler(this.PedsForm_Deactivate);
            this.Load += new System.EventHandler(this.PedsForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PedsForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PedsForm_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PedsForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PedsForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PedsForm_MouseUp);
            this.ConsolePanel.ResumeLayout(false);
            this.ConsolePanel.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ToolsPanel.ResumeLayout(false);
            this.ToolsTabControl.ResumeLayout(false);
            this.ToolsPedTabPage.ResumeLayout(false);
            this.ToolsPedTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PlaybackSpeedTrackBar)).EndInit();
            this.ToolsModelsTabPage.ResumeLayout(false);
            this.ToolsTexturesTabPage.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ToolsOptionsTabPage.ResumeLayout(false);
            this.ToolsOptionsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.floorUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeOfDayTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.feet_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowr_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.uppr_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.hair_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.berd_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.head_updown)).EndInit();
            this.ToolsCameraTabPage.ResumeLayout(false);
            this.ToolsCameraTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CameraPresetsDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WinForms.TextBoxFix ConsoleTextBox;
        private System.Windows.Forms.Button ToolsPanelShowButton;
        private System.Windows.Forms.Button ToolsPanelHideButton;
        private System.Windows.Forms.Panel ToolsDragPanel;
        private System.Windows.Forms.Panel ConsolePanel;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel MousedLabel;
        private System.Windows.Forms.ToolStripStatusLabel StatsLabel;
        private System.Windows.Forms.Timer StatsUpdateTimer;
        private System.Windows.Forms.Panel ToolsPanel;
        private System.Windows.Forms.TabControl ToolsTabControl;
        private System.Windows.Forms.TabPage ToolsPedTabPage;
        private System.Windows.Forms.CheckBox EnableRootMotionCheckBox;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ComboBox ClipComboBox;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ComboBox ClipDictComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox PedNameComboBox;
        private System.Windows.Forms.TabPage ToolsModelsTabPage;
        private WinForms.TreeViewFix ModelsTreeView;
        private System.Windows.Forms.TabPage ToolsOptionsTabPage;
        private System.Windows.Forms.CheckBox StatusBarCheckBox;
        private System.Windows.Forms.CheckBox ErrorConsoleCheckBox;
        private System.Windows.Forms.CheckBox EnableAnimationCheckBox;
        private System.Windows.Forms.TrackBar PlaybackSpeedTrackBar;
        private System.Windows.Forms.Label PlaybackSpeedLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label head_label;
        private System.Windows.Forms.NumericUpDown head_updown;
        private System.Windows.Forms.Label feet_label;
        private System.Windows.Forms.NumericUpDown feet_updown;
        private System.Windows.Forms.Label lowr_label;
        private System.Windows.Forms.NumericUpDown lowr_updown;
        private System.Windows.Forms.Label uppr_label;
        private System.Windows.Forms.NumericUpDown uppr_updown;
        private System.Windows.Forms.Label hair_label;
        private System.Windows.Forms.NumericUpDown hair_updown;
        private System.Windows.Forms.Label berd_label;
        private System.Windows.Forms.NumericUpDown berd_updown;
        private System.Windows.Forms.Button Save_defaultComp;
        private System.Windows.Forms.TabPage ToolsTexturesTabPage;
        private WinForms.TreeViewFix TexturesTreeView;
        private System.Windows.Forms.CheckBox WireframeCheckBox;
        private System.Windows.Forms.CheckBox SkeletonsCheckBox;
        private System.Windows.Forms.CheckBox ShadowsCheckBox;
        private System.Windows.Forms.CheckBox HDRRenderingCheckBox;
        private System.Windows.Forms.ComboBox RenderModeComboBox;
        private System.Windows.Forms.ComboBox CustomAnimComboBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox TextureSamplerComboBox;
        private System.Windows.Forms.ComboBox TextureCoordsComboBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label TimeOfDayLabel;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TrackBar TimeOfDayTrackBar;
        private System.Windows.Forms.CheckBox ControlLightDirCheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton specularRadio;
        private System.Windows.Forms.RadioButton normalRadio;
        private System.Windows.Forms.RadioButton diffuseRadio;
        private System.Windows.Forms.Button liveTxtButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox AutoRotatePedCheckBox;
        private System.Windows.Forms.CheckBox OnlySelectedCheckBox;
        private System.Windows.Forms.Button btn_restartCamera;
        private System.Windows.Forms.CheckBox floorCheckbox;
        private System.Windows.Forms.NumericUpDown floorUpDown;
        private System.Windows.Forms.Label PolygonCountText;
        private System.Windows.Forms.Label VertexCountText;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TabPage ToolsCameraTabPage;
        private System.Windows.Forms.Label label100;
        private System.Windows.Forms.TextBox CameraPositionTextBox;
        private System.Windows.Forms.Label label101;
        private System.Windows.Forms.TextBox CameraRotationTextBox;
        private System.Windows.Forms.Button btn_addCameraPreset;
        private System.Windows.Forms.Label label102;
        private System.Windows.Forms.TextBox CameraSavePresetTextBox;
        private System.Windows.Forms.TextBox CameraDistanceTextBox;
        private System.Windows.Forms.DataGridView CameraPresetsDataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn DataGridViewName;
        private System.Windows.Forms.DataGridViewButtonColumn DataGridViewDelete;
    }
}