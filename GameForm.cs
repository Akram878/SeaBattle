using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace SeaBattle
{
    public partial class GameForm : Form
    {


        // =================== General Settings ===================
        private const int CellSize = 30;
        private const int Port = 5000;

        private Button[,] _myButtons = new Button[Board.GridSize, Board.GridSize];
        private Button[,] _enemyButtons = new Button[Board.GridSize, Board.GridSize];

        private GameSession _session = new GameSession();
        private NetworkManager _network;
       
        private bool _isHost;

        // Connection / app-level state (UI)
        private enum AppState
        {
            Idle,                 // No connection, no game
            WaitingForConnection, // Host/Join waiting for connection
            Connected             // Connected (round may be active or finished)
        }




        private AppState _state = AppState.Idle;

        // For manual placement
        private bool _manualSetupMode = false;
        private bool _manualHorizontal = true;
        private Dictionary<int, int> _remainingShips = new Dictionary<int, int>();

        // Last shot we fired (for result)
        private Point? _lastShotPoint = null;

        // Suppress message when disconnect expected
        private bool _suppressNextDisconnectMessage = false;

        private string _localIp = "Unknown";

        // Replay coordination flags // no
        private bool _localWantsReplay = false;
        private bool _opponentWantsReplay = false;



        private enum ReplayChoice
        {
            None,
            Yes,
            No
        }

        private ReplayChoice _localReplayChoice = ReplayChoice.None;
        private ReplayChoice _remoteReplayChoice = ReplayChoice.None;
        // =================== Constructor ===================
        public GameForm()
        {
            InitializeComponent();

            _txtIp.Text = "";

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            this.FormClosing += GameForm_FormClosing;

            // Local IP label
            _localIp = GetLocalIPv4();
            try
            {
                _lblMyIp.Text = $"Your IP: {_localIp}";
            }
            catch { }

            CreateBoards();
            PrepareEmptyBoards();
            SetState(AppState.Idle, "Status: No connection.");

            try
            {
                _cmbShipSize.Enabled = false;
                _cmbShipSize.Items.Clear();
                _btnToggleOrientation.Enabled = false;
                _btnToggleOrientation.Text = "Horizontal";
            }
            catch { }

            // Make sure orientation button is wired
            _btnToggleOrientation.Click += _btnToggleOrientation_Click;
        }

        // =================== Get Local IPv4 ===================
        private string GetLocalIPv4()
        {
            try
            {
                string hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);

                foreach (var a in hostEntry.AddressList)
                {
                    if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(a))
                    {
                        return a.ToString();
                    }
                }

                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // =================== State Machine ===================
        private void SetState(AppState newState, string statusText = null)
        {
            _state = newState;

            switch (_state)
            {
                case AppState.Idle:
                    _btnHost.Enabled = true;
                    _btnJoin.Enabled = true;
                    _txtIp.Enabled = true;
                    _btnCancel.Enabled = false;
                    _btnEndGame.Enabled = false;
                    break;

                case AppState.WaitingForConnection:
                    _btnHost.Enabled = false;
                    _btnJoin.Enabled = false;
                    _txtIp.Enabled = false;
                    _btnCancel.Enabled = true;
                    _btnEndGame.Enabled = false;
                    break;

                case AppState.Connected:
                    _btnHost.Enabled = false;
                    _btnJoin.Enabled = false;
                    _txtIp.Enabled = false;
                    _btnCancel.Enabled = false;
                    _btnEndGame.Enabled = true;
                    break;
            }

            if (statusText != null)
                _lblStatus.Text = statusText;
        }

        // =================== Board Creation ===================
        private void CreateBoards()
        {
            _panelMyBoard.Controls.Clear();
            _panelEnemyBoard.Controls.Clear();

            // My board
            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    var btn = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Left = x * CellSize,
                        Top = y * CellSize,
                        Enabled = false,
                        BackColor = Color.LightBlue,
                        Margin = Padding.Empty,
                        Tag = new Point(x, y)
                    };
                    btn.Click += MyBoardButton_Click;
                    _myButtons[x, y] = btn;
                    _panelMyBoard.Controls.Add(btn);
                }
            }

            // Enemy board
            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    var btn = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Left = x * CellSize,
                        Top = y * CellSize,
                        BackColor = Color.LightGray,
                        Tag = new Point(x, y),
                        Margin = Padding.Empty
                    };
                    btn.Click += EnemyButton_Click;
                    _enemyButtons[x, y] = btn;
                    _panelEnemyBoard.Controls.Add(btn);
                }
            }
        }

        // =================== Empty Boards (no ships) ===================
        private void PrepareEmptyBoards()
        {
            _manualSetupMode = false;
            _session.ClearAll();
            _lastShotPoint = null;

            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    _myButtons[x, y].BackColor = Color.LightBlue;
                    _myButtons[x, y].Enabled = false;
                }
            }

            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    _enemyButtons[x, y].BackColor = Color.LightGray;
                }
            }

            try
            {
                _cmbShipSize.Enabled = false;
                _cmbShipSize.Items.Clear();
                _btnToggleOrientation.Enabled = false;
                _btnToggleOrientation.Text = "Horizontal";
            }
            catch { }
        }

        // =================== Random Fleet ===================
        private void PrepareRandomFleet()
        {
            _manualSetupMode = false;
            _session.PrepareRandomFleetForMe();
            _lastShotPoint = null;

            // Draw my board
            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    var cell = _session.MyBoard.GetCell(x, y);
                    _myButtons[x, y].BackColor =
                        (cell == CellState.Ship) ? Color.Navy : Color.LightBlue;
                    _myButtons[x, y].Enabled = false;
                }
            }

            // Reset enemy board visual
            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    _enemyButtons[x, y].BackColor = Color.LightGray;
                }
            }

            try
            {
                _cmbShipSize.Enabled = false;
                _cmbShipSize.Items.Clear();
                _btnToggleOrientation.Enabled = false;
                _btnToggleOrientation.Text = "Horizontal";
            }
            catch { }
        }

        // =================== Manual Fleet Setup Helpers ===================
        private void InitRemainingShips()
        {
            _remainingShips = new Dictionary<int, int>
            {
                { 4, 1 },
                { 3, 2 },
                { 2, 3 },
                { 1, 4 }
            };
        }

        private void UpdateShipSelectionUI()
        {
            try
            {
                _cmbShipSize.Items.Clear();
                foreach (var kv in _remainingShips)
                {
                    int size = kv.Key;
                    int count = kv.Value;
                    _cmbShipSize.Items.Add($"{size}-cell ship (x{count} left)");
                }

                if (_cmbShipSize.Items.Count > 0)
                {
                    if (_cmbShipSize.SelectedIndex < 0)
                        _cmbShipSize.SelectedIndex = 0;
                    _cmbShipSize.Enabled = true;
                }
                else
                {
                    _cmbShipSize.Enabled = false;
                }
            }
            catch { }
        }

        private int? GetSelectedShipSize()
        {
            try
            {
                if (_cmbShipSize.SelectedItem == null)
                    return null;

                string text = _cmbShipSize.SelectedItem.ToString();
                int dashIndex = text.IndexOf('-');
                if (dashIndex <= 0)
                    return null;

                string numStr = text.Substring(0, dashIndex);
                if (int.TryParse(numStr, out int size))
                    return size;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void PrepareEmptyBoardForManual()
        {
            _session.ClearAll();
            _lastShotPoint = null;

            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    _myButtons[x, y].BackColor = Color.LightBlue;
                    _myButtons[x, y].Enabled = true;
                }
            }

            for (int y = 0; y < Board.GridSize; y++)
            {
                for (int x = 0; x < Board.GridSize; x++)
                {
                    _enemyButtons[x, y].BackColor = Color.LightGray;
                }
            }

            InitRemainingShips();
            _manualHorizontal = true;
            _manualSetupMode = true;

            UpdateShipSelectionUI();

            try
            {
                _btnToggleOrientation.Enabled = true;
                _btnToggleOrientation.Text = "Horizontal";
            }
            catch { }

            _lblStatus.Text =
                "Manual setup: select ship size from list, then click on the board. Orientation: Horizontal.";
        }

        // =================== IP Validation ===================
        private bool IsValidIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            ip = ip.Trim();

            if (ip.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            return IPAddress.TryParse(ip, out _);
        }

        // =================== Network Handling ===================
        private void CreateNetwork(bool isHost, string ip, int port)
        {
            _network = new NetworkManager(isHost, ip, port);
            _network.Connected += OnConnected;
            _network.ShotReceived += OnShotReceived;
            _network.ResultReceived += OnResultReceived;
            _network.ResetReceived += OnResetReceived;
            _network.Disconnected += OnDisconnected;
            _network.ShipDestroyed += OnShipDestroyed;
        }

        private void StartNetworkInBackground()
        {
            var net = _network;
            var t = new Thread(() =>
            {
                try { net.Start(); }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void SafeCloseNetwork()
        {
            var net = _network;
            _network = null;

            if (net == null)
                return;

            _suppressNextDisconnectMessage = true;

            var t = new Thread(() =>
            {
                try { net.Close(); }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        // =================== Host / Join / Cancel / EndGame ===================
        private void BtnHost_Click(object sender, EventArgs e)
        {
            if (_state != AppState.Idle)
            {
                MessageBox.Show(
                    "You cannot host right now. Please end the current connection first.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_session.IsFleetReady)
            {
                var res = MessageBox.Show(
                    "Your fleet is not complete.\nDo you want to place ships randomly?",
                    "Fleet incomplete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    PrepareRandomFleet();
                }
                else
                {
                    MessageBox.Show(
                        "Finish manual setup of your fleet before hosting.",
                        "Manual setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }

            _isHost = true;
            _session.SetRole(true);

            CreateNetwork(true, "", Port);
            SetState(AppState.WaitingForConnection, "Status: Waiting for client connection...");
            StartNetworkInBackground();
        }

        private void BtnJoin_Click(object sender, EventArgs e)
        {
            if (_state != AppState.Idle)
            {
                MessageBox.Show(
                    "You cannot join right now. Please end the current connection first.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_session.MyBoard.IsFleetComplete)
            {
                var res = MessageBox.Show(
                    "Your fleet is not complete.\nDo you want to place ships randomly?",
                    "Fleet incomplete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    PrepareRandomFleet();
                }
                else
                {
                    MessageBox.Show(
                        "Finish manual setup of your fleet before joining.",
                        "Manual setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
            }

            string ip = _txtIp.Text.Trim();

            if (ip.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                ip = "127.0.0.1";

            if (!IsValidIp(ip))
            {
                MessageBox.Show(
                    "Please enter a valid host IP address (example: 192.168.1.10 or localhost).",
                    "Invalid IP Address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtIp.Focus();
                _txtIp.SelectAll();
                return;
            }

            _isHost = false;
            _session.SetRole(false);

            CreateNetwork(false, ip, Port);
            SetState(AppState.WaitingForConnection, $"Status: Connecting to host {ip}...");
            StartNetworkInBackground();
        }

        private void _btnCancel_Click(object sender, EventArgs e)
        {
            if (_state != AppState.WaitingForConnection)
            {
                MessageBox.Show(
                    "There is no pending connection to cancel.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SafeCloseNetwork();
            PrepareEmptyBoards();
            SetState(AppState.Idle, "Status: Connection canceled.");
        }

        private void _btnEndGame_Click(object sender, EventArgs e)
        {
            if (_state != AppState.Connected)
            {
                MessageBox.Show(
                    "There is no active connection to end.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var res = MessageBox.Show(
                "Are you sure you want to end this round and disconnect?",
                "End Round",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.No)
                return;

            SafeCloseNetwork();
            PrepareEmptyBoards();
            SetState(AppState.Idle, "Status: No connection.");
        }

        private void _btnManualSetup_Click(object sender, EventArgs e)
        {
            if (_state != AppState.Idle)
            {
                MessageBox.Show(
                    "You can only arrange ships manually before starting a connection.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            PrepareEmptyBoardForManual();
        }

        private void _btnToggleOrientation_Click(object sender, EventArgs e)
        {
            // عكس الاتجاه
            _manualHorizontal = !_manualHorizontal;

            // تحديث نص الزر
            try
            {
                _btnToggleOrientation.Text = _manualHorizontal ? "Horizontal" : "Vertical";
            }
            catch { }

            // تحديث رسالة الحالة
            if (_manualSetupMode)
            {
                _lblStatus.Text =
                    $"Manual setup: select ship size, then click on the board. Orientation: {(_manualHorizontal ? "Horizontal" : "Vertical")}.";
            }
        }

        // =================== Network Events ===================
        private void OnConnected()
        {
            Invoke((Action)(() =>
            {
                if (_state != AppState.WaitingForConnection)
                    return;

                try
                {
                    _session.StartRound();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot start round: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                SetState(AppState.Connected,
                    _isHost
                        ? "Status: Connected - You are the Host. Start playing."
                        : "Status: Connected - You are the Client. Wait for your turn.");

                MessageBox.Show(
                    "Connection successful!\nThe game can start now.",
                    "Connection Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }));
        }

        private void OnShotReceived(int x, int y)
        {
            Invoke((Action)(() =>
            {
                if (_state != AppState.Connected || _session.Phase != GamePhase.InProgress)
                    return;

                var result = _session.ReceiveEnemyShot(x, y);
                var state = result.state;
                bool isHit = result.isHit;
                bool hasLost = result.hasLost;

                if (state == CellState.Hit)
                    _myButtons[x, y].BackColor = Color.Red;
                else if (state == CellState.Miss)
                    _myButtons[x, y].BackColor = Color.Gray;

                // تحقق من تدمير السفينة بعد الضربة
                var destroyedShip = _session.MyBoard.CheckIfShipDestroyed(x, y);
                if (destroyedShip != null)
                {
                    // أرسل إشعار للخصم فوراً (غير阻-blocking)
                    try { _network?.SendShipDestroyed(destroyedShip.Size); } catch { }

                    // أشعار محلي غير阻-blocking
                    BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show(
                            $"You have destroyed an enemy ship of size {destroyedShip.Size}!",
                            "Enemy Ship Destroyed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }));
                }

                string msg = isHit ? "HIT" : "MISS";
                if (hasLost)
                    msg = "WIN";

                try { _network?.SendResult(msg); } catch { }

                if (hasLost)
                {
                    // ⬅ هذا الطرف هو الخاسر
                    MessageBox.Show(
                        "You lost! The opponent has won.",
                        "Game Over",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // نسأل هذا اللاعب عن الإعادة
                    AskReplay(iWon: false);
                }
            }));
        }


        private void OnResultReceived(string result)
        {
            Invoke((Action)(() =>
            {
                if (_state != AppState.Connected || _session.Phase != GamePhase.InProgress)
                    return;

                if (!_lastShotPoint.HasValue)
                    return;

                int x = _lastShotPoint.Value.X;
                int y = _lastShotPoint.Value.Y;

                var r = _session.ApplyMyShotResult(x, y, result);
                bool isHit = r.isHit;
                bool enemyLost = r.enemyLost;

                if (isHit)
                {
                    // إصابة ناجحة → خلية العدو باللون الأحمر الداكن
                    _enemyButtons[x, y].BackColor = Color.DarkRed;
                }
                else
                {
                    // ضربة خاطئة (MISS) → خلية العدو باللون الأخضر الفاتح
                    _enemyButtons[x, y].BackColor = Color.LightGreen;
                }

                if (enemyLost)
                {
                    // ⬅ هذا الطرف هو الفائز
                    MessageBox.Show(
                        "Congratulations! You won!",
                        "Game Over",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // نسأل هذا اللاعب أيضًا عن الإعادة
                    AskReplay(iWon: true);
                }
            }));
        }

        private void OnResetReceived()
        {
            Invoke((Action)(() =>
            {
                if (_state != AppState.Connected)
                    return;

                // الخصم وافق على Replay
                _remoteReplayChoice = ReplayChoice.Yes;

                // إذا نحن أيضاً وافقنا، نبدأ التحضير للجولة الجديدة
                TryStartReplayIfReady();
            }));
        }

        private void OnDisconnected()
        {
            Invoke((Action)(() =>
            {
                if (_suppressNextDisconnectMessage)
                {
                    _suppressNextDisconnectMessage = false;
                    return;
                }

                _network = null;
                _session.ClearAll();

                if (_state == AppState.WaitingForConnection)
                {
                    MessageBox.Show(
                        "Failed to connect to the other player.\nPlease try again.",
                        "Connection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else if (_state == AppState.Connected)
                {
                    MessageBox.Show(
                        "The connection was lost during the game.\nYou may start a new game or reconnect.",
                        "Connection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                this.Activate();
                this.BringToFront();

                PrepareEmptyBoards();
                SetState(AppState.Idle, "Status: No connection.");
            }));
        }

        // =================== Manual Placement Click ===================
        private void MyBoardButton_Click(object sender, EventArgs e)
        {
            if (!_manualSetupMode)
                return;

            if (_remainingShips == null || _remainingShips.Count == 0)
                return;

            Button btn = sender as Button;
            if (btn == null)
                return;

            var p = (Point)btn.Tag;
            int x = p.X;
            int y = p.Y;

            int? sizeOpt = GetSelectedShipSize();
            if (!sizeOpt.HasValue)
            {
                MessageBox.Show(
                    "Please select a ship size from the list before placing.",
                    "No ship selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int size = sizeOpt.Value;

            if (!_remainingShips.ContainsKey(size) || _remainingShips[size] <= 0)
            {
                MessageBox.Show(
                    "No ships of this size are left.\nPlease select another size.",
                    "Ship limit reached",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                UpdateShipSelectionUI();
                return;
            }

            // 🚩 هنا المهم: نستخدم _manualHorizontal فعلاً
            bool ok = _session.MyBoard.TryPlaceShip(x, y, size, _manualHorizontal);
            if (!ok)
            {
                MessageBox.Show(
                    "Cannot place ship here.\nShips must not overlap or touch each other and must be on the board.",
                    "Invalid position",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // تلوين السفينة حسب الاتجاه الحالي
            for (int i = 0; i < size; i++)
            {
                int cx = _manualHorizontal ? x + i : x;
                int cy = _manualHorizontal ? y : y + i;
                _myButtons[cx, cy].BackColor = Color.Navy;
            }

            // نقص عدد السفن المتبقية من هذا النوع
            _remainingShips[size]--;
            if (_remainingShips[size] <= 0)
                _remainingShips.Remove(size);

            UpdateShipSelectionUI();

            if (_remainingShips.Count == 0)
            {
                _manualSetupMode = false;

                foreach (var b in _myButtons)
                    b.Enabled = false;

                _session.NotifyManualFleetCompleted();

                if (_state == AppState.Idle)
                {
                    MessageBox.Show(
                        "All ships placed successfully.\nYou can now start the network game.",
                        "Fleet ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    _lblStatus.Text = "Fleet ready. You can Host or Join a game.";
                }
                else if (_state == AppState.Connected && _session.Phase == GamePhase.FleetReady)
                {
                    try
                    {
                        _session.StartRound();

                        MessageBox.Show(
                            "All ships placed successfully.\nThe new round begins.",
                            "Fleet ready",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        _lblStatus.Text = _isHost
                            ? "Status: New round - You are the Host."
                            : "Status: New round - You are the Client.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Cannot start new round: " + ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                else
                {
                    _lblStatus.Text = "Fleet ready.";
                }
            }
            else
            {
                _lblStatus.Text =
                    $"Manual setup: select ship size, then click on the board. Orientation: {(_manualHorizontal ? "Horizontal" : "Vertical")}.";
            }
        }

        // =================== Enemy Board Click (Shooting) ===================
        private void EnemyButton_Click(object sender, EventArgs e)
        {
            if (_state != AppState.Connected || _session.Phase != GamePhase.InProgress)
            {
                MessageBox.Show(
                    "There is no active round at the moment.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_session.IsMyTurn)
            {
                MessageBox.Show(
                    "It is not your turn yet.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var btn = sender as Button;
            if (btn == null) return;

            var p = (Point)btn.Tag;
            int x = p.X;
            int y = p.Y;

            var cell = _session.EnemyBoard.GetCell(x, y);
            if (cell == CellState.Hit || cell == CellState.Miss)
            {
                MessageBox.Show(
                    "This cell has already been attacked.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _lastShotPoint = p;

            try
            {
                _network?.SendShot(x, y);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error while sending the shot: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Called when the round is finished (either win or lose).
        /// Both players will see this on their side.
        /// Replay will start only if BOTH players answer Yes.
        /// If any player answers No, the connection is closed.
        /// </summary>
        private void AskReplay(bool iWon)
        {
            if (_network == null || _state != AppState.Connected)
            {
                // لا يوجد اتصال شبكة فعّال، نعرض خيار إعادة محلية فقط
                var localRes = MessageBox.Show(
                    "The round is over.\nDo you want to prepare a new board locally (no network)?",
                    "Local Replay?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (localRes == DialogResult.Yes)
                {
                    PrepareEmptyBoards();
                    SetState(AppState.Idle, "Status: No connection.");
                }
                return;
            }

            string msg = iWon
                ? "You won this round.\nDo you want to play another round with the same connection?"
                : "You lost this round.\nDo you want to play another round with the same connection?";

            var res = MessageBox.Show(
                msg,
                "Replay?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                // هذا اللاعب يريد إعادة اللعب
                _localReplayChoice = ReplayChoice.Yes;

                // نخبر الخصم أننا نريد Replay (باستخدام Reset)
                try { _network.SendReset(); } catch { }

                // لو الخصم وافق مسبقًا، نبدأ التحضير الآن
                TryStartReplayIfReady();
            }
            else
            {
                // هذا اللاعب لا يريد إعادة → نغلق الاتصال للطرفين
                _localReplayChoice = ReplayChoice.No;
                _remoteReplayChoice = ReplayChoice.None;

                SafeCloseNetwork();
                PrepareEmptyBoards();
                SetState(AppState.Idle, "Status: No connection. Replay was declined.");
            }
        }

        /// <summary>
        /// Called when BOTH players have agreed to replay:
        /// - locally via AskReplay (Yes)
        /// - remotely via OnResetReceived (opponent sent reset).
        /// Then we ask this local player how to arrange ships (random/manual)
        /// and prepare a new round accordingly.
        /// </summary>
        /// <summary>
        /// Called once both players have agreed to replay.
        /// Each player chooses random/manual for their own board.
        /// </summary>
        private void DoReplaySetup()
        {
            var res = MessageBox.Show(
                "Both players agreed to replay.\nDo you want to place ships randomly?\n\nYes = Random placement\nNo = Manual placement",
                "New Round Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                // عشوائي
                PrepareRandomFleet();

                try
                {
                    _session.StartRound();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Cannot start new round: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                SetState(AppState.Connected,
                    _isHost
                        ? "Status: New round - You are the Host."
                        : "Status: New round - You are the Client.");
            }
            else
            {
                // ترتيب يدوي للجولة الجديدة
                PrepareEmptyBoardForManual();

                _lblStatus.Text =
                    "Manual setup for the new round: arrange your fleet; the round will start automatically after you finish.";

                // StartRound سيتم استدعاؤها في MyBoardButton_Click
                // عندما تنتهي من ترتيب السفن وفي حالة:
                // _state == AppState.Connected && _session.Phase == GamePhase.FleetReady
            }
        }

        // =================== IP TextBox Input Restriction ===================
        private void _txtIp_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) &&
                !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '.' &&
                !char.IsLetter(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // =================== Form Events ===================
        private void GameForm_Load(object sender, EventArgs e)
        {
            // Nothing special for now
        }

        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_network != null)
            {
                if (_state == AppState.Connected && _session.Phase == GamePhase.InProgress)
                {
                    var res = MessageBox.Show(
                        "You are currently connected and the game has not ended yet.\nAre you sure you want to exit?",
                        "Exit Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (_state == AppState.WaitingForConnection)
                {
                    var res = MessageBox.Show(
                        "A connection attempt is currently in progress.\nClosing now will cancel it.\nDo you want to continue?",
                        "Exit Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                SafeCloseNetwork();
            }
        }

        /// <summary>
        /// Starts replay setup only if BOTH sides answered Yes.
        /// </summary>
        private void TryStartReplayIfReady()
        {
            if (_localReplayChoice == ReplayChoice.Yes &&
                _remoteReplayChoice == ReplayChoice.Yes)
            {
                // نعيد ضبط الخيارات حتى لا نكرر التشغيل
                _localReplayChoice = ReplayChoice.None;
                _remoteReplayChoice = ReplayChoice.None;

                DoReplaySetup();
            }
        }


        // هذا الحدث سيتم تفعيله عند تدمير السفينة من قبل الخصم
        private void OnShipDestroyed(int destroyedShipSize)
        {
            // إشعار غير阻-blocking
            BeginInvoke((Action)(() =>
            {
                MessageBox.Show(
                    $"One of your ships of size {destroyedShipSize} was destroyed!",
                    "Ship Lost",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }));
        }



    }
}
