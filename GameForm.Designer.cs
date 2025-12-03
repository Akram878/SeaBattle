namespace SeaBattle
{
    partial class GameForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // عناصر الواجهة (لا تلمس أسمائها)
        private System.Windows.Forms.TextBox _txtIp;
        private System.Windows.Forms.Button _btnHost;
        private System.Windows.Forms.Button _btnJoin;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Panel _panelMyBoard;
        private System.Windows.Forms.Panel _panelEnemyBoard;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this._txtIp = new System.Windows.Forms.TextBox();
            this._btnHost = new System.Windows.Forms.Button();
            this._btnJoin = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._panelMyBoard = new System.Windows.Forms.Panel();
            this._panelEnemyBoard = new System.Windows.Forms.Panel();
            this._btnEndGame = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._lblMyIp = new System.Windows.Forms.Label();
            this._btnManualSetup = new System.Windows.Forms.Button();
            this._cmbShipSize = new System.Windows.Forms.ComboBox();
            this._btnToggleOrientation = new System.Windows.Forms.Button();
            this._btnToggleOrientation.Click += new System.EventHandler(this._btnToggleOrientation_Click);

            this.SuspendLayout();
            // 
            // _txtIp
            // 
            this._txtIp.Location = new System.Drawing.Point(20, 10);
            this._txtIp.Name = "_txtIp";
            this._txtIp.Size = new System.Drawing.Size(150, 20);
            this._txtIp.TabIndex = 0;
            // 
            // _btnHost
            // 
            this._btnHost.Location = new System.Drawing.Point(176, 8);
            this._btnHost.Name = "_btnHost";
            this._btnHost.Size = new System.Drawing.Size(62, 25);
            this._btnHost.TabIndex = 1;
            this._btnHost.Text = "Host";
            this._btnHost.UseVisualStyleBackColor = true;
            this._btnHost.Click += new System.EventHandler(this.BtnHost_Click);
            // 
            // _btnJoin
            // 
            this._btnJoin.Location = new System.Drawing.Point(244, 8);
            this._btnJoin.Name = "_btnJoin";
            this._btnJoin.Size = new System.Drawing.Size(62, 25);
            this._btnJoin.TabIndex = 2;
            this._btnJoin.Text = "Join";
            this._btnJoin.UseVisualStyleBackColor = true;
            this._btnJoin.Click += new System.EventHandler(this.BtnJoin_Click);
            // 
            // _lblStatus
            // 
            this._lblStatus.AutoSize = true;
            this._lblStatus.Location = new System.Drawing.Point(17, 33);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(0, 13);
            this._lblStatus.TabIndex = 3;
            // 
            // _panelMyBoard
            // 
            this._panelMyBoard.Location = new System.Drawing.Point(20, 60);
            this._panelMyBoard.Name = "_panelMyBoard";
            this._panelMyBoard.Size = new System.Drawing.Size(300, 300);
            this._panelMyBoard.TabIndex = 4;
            // 
            // _panelEnemyBoard
            // 
            this._panelEnemyBoard.Location = new System.Drawing.Point(330, 60);
            this._panelEnemyBoard.Name = "_panelEnemyBoard";
            this._panelEnemyBoard.Size = new System.Drawing.Size(300, 300);
            this._panelEnemyBoard.TabIndex = 5;
            // 
            // _btnEndGame
            // 
            this._btnEndGame.Location = new System.Drawing.Point(380, 8);
            this._btnEndGame.Name = "_btnEndGame";
            this._btnEndGame.Size = new System.Drawing.Size(62, 25);
            this._btnEndGame.TabIndex = 6;
            this._btnEndGame.Text = "retreat";
            this._btnEndGame.UseVisualStyleBackColor = true;
            this._btnEndGame.Click += new System.EventHandler(this._btnEndGame_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Location = new System.Drawing.Point(312, 8);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(62, 25);
            this._btnCancel.TabIndex = 7;
            this._btnCancel.Text = "cancel";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this._btnCancel_Click);
            // 
            // _lblMyIp
            // 
            this._lblMyIp.AutoSize = true;
            this._lblMyIp.Location = new System.Drawing.Point(448, 14);
            this._lblMyIp.Name = "_lblMyIp";
            this._lblMyIp.Size = new System.Drawing.Size(35, 13);
            this._lblMyIp.TabIndex = 8;
            this._lblMyIp.Text = "label1";
            // 
            // _btnManualSetup
            // 
            this._btnManualSetup.Location = new System.Drawing.Point(20, 366);
            this._btnManualSetup.Name = "_btnManualSetup";
            this._btnManualSetup.Size = new System.Drawing.Size(94, 23);
            this._btnManualSetup.TabIndex = 9;
            this._btnManualSetup.Text = "Manual setup";
            this._btnManualSetup.UseVisualStyleBackColor = true;
            this._btnManualSetup.Click += new System.EventHandler(this._btnManualSetup_Click);
            // 
            // _cmbShipSize
            // 
            this._cmbShipSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbShipSize.Enabled = false;
            this._cmbShipSize.FormattingEnabled = true;
            this._cmbShipSize.Location = new System.Drawing.Point(120, 368);
            this._cmbShipSize.Name = "_cmbShipSize";
            this._cmbShipSize.Size = new System.Drawing.Size(121, 21);
            this._cmbShipSize.TabIndex = 10;
            // 
            // _btnToggleOrientation
            // 
            this._btnToggleOrientation.Location = new System.Drawing.Point(247, 368);
            this._btnToggleOrientation.Name = "_btnToggleOrientation";
            this._btnToggleOrientation.Size = new System.Drawing.Size(75, 23);
            this._btnToggleOrientation.TabIndex = 11;
            this._btnToggleOrientation.Text = "button1";
            this._btnToggleOrientation.UseVisualStyleBackColor = true;
            this._btnToggleOrientation.Click += new System.EventHandler(this._btnToggleOrientation_Click);
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(644, 397);
            this.Controls.Add(this._btnToggleOrientation);
            this.Controls.Add(this._cmbShipSize);
            this.Controls.Add(this._btnManualSetup);
            this.Controls.Add(this._lblMyIp);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnEndGame);
            this.Controls.Add(this._panelEnemyBoard);
            this.Controls.Add(this._panelMyBoard);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._btnJoin);
            this.Controls.Add(this._btnHost);
            this.Controls.Add(this._txtIp);
            this.Name = "GameForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Морской бой - OOP Version";
            this.Load += new System.EventHandler(this.GameForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _btnEndGame;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Label _lblMyIp;
        private System.Windows.Forms.Button _btnManualSetup;
        private System.Windows.Forms.ComboBox _cmbShipSize;
        private System.Windows.Forms.Button _btnToggleOrientation;
    }
}
