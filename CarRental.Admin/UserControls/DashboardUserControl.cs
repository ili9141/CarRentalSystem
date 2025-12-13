using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;


namespace CarRental.Admin.UserControls
{
    public class DashboardUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private System.Windows.Forms.Timer refreshTimer;
        private bool isLoading = false;
        private readonly object _lock = new object();
        private bool isInitialLoad = true;
        private readonly Color BgMain = Color.FromArgb(245, 247, 250);
        private readonly Color CardBg = Color.White;
        private readonly Color BorderSoft = Color.FromArgb(230, 230, 230);
        private readonly Font TitleFont = new Font("Segoe UI", 18, FontStyle.Bold);
        private readonly Font SubTitleFont = new Font("Segoe UI", 10);
        private readonly Font CardTitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
        private readonly Font CardValueFont = new Font("Segoe UI", 17, FontStyle.Bold);


        // Stats labels
        private Label lblTotalClients;
        private Label lblTotalVehicles;
        private Label lblTotalRentals;
        private Label lblTotalRevenue;
        private Label lblActiveRentals;
        private Label lblAvailableVehicles;
        private Label lblPendingPayments;
        private Label lblMaintenanceAlerts;
        private Label lblLastUpdated;

        // Recent activity lists
        private ListBox lstRecentRentals;
        private ListBox lstRecentPayments;
        private ListBox lstRecentMaintenance;

        public DashboardUserControl(ApplicationDbContext context)
        {
            _context = context;

            SetupUI();

            // Wait for handle to be created before loading data
            this.HandleCreated += (s, e) =>
            {
                if (isInitialLoad)
                {
                    LoadDashboardData();
                    isInitialLoad = false;
                }

                // Setup auto-refresh every 60 seconds
                refreshTimer = new System.Windows.Forms.Timer();
                refreshTimer.Interval = 60000;
                refreshTimer.Tick += (s, e) => LoadDashboardData();
                refreshTimer.Start();
            };
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = BgMain;


            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Title
            var lblTitle = new Label
            {
                Text = "📊 DASHBOARD - BUSINESS OVERVIEW",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            var lblSubtitle = new Label
            {
                Text = "Real-time insights into your car rental business",
                Font = new Font("Segoe UI", 10),
                Location = new Point(25, 55),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            // Stats Cards Container
            var statsPanel = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(1100, 150)
            };

            // Create 8 stat cards
            var statCards = new[]
            {
                CreateStatCard("👥 Total Clients", "0", Color.FromArgb(41, 128, 185), 0),
                CreateStatCard("🚗 Total Vehicles", "0", Color.FromArgb(39, 174, 96), 140),
                CreateStatCard("📅 Total Rentals", "0", Color.FromArgb(142, 68, 173), 280),
                CreateStatCard("💰 Total Revenue", "$0", Color.FromArgb(230, 126, 34), 420),
                CreateStatCard("✅ Active Rentals", "0", Color.FromArgb(22, 160, 133), 560),
                CreateStatCard("🟢 Available Vehicles", "0", Color.FromArgb(46, 204, 113), 700),
                CreateStatCard("⏳ Pending Payments", "0", Color.FromArgb(241, 196, 15), 840),
                CreateStatCard("⚠️ Maintenance Alerts", "0", Color.FromArgb(231, 76, 60), 980)
            };

            foreach (var card in statCards)
            {
                statsPanel.Controls.Add(card);
            }

            // Store references to the value labels
            lblTotalClients = (Label)statsPanel.Controls.Find("lblValue0", true)[0];
            lblTotalVehicles = (Label)statsPanel.Controls.Find("lblValue1", true)[0];
            lblTotalRentals = (Label)statsPanel.Controls.Find("lblValue2", true)[0];
            lblTotalRevenue = (Label)statsPanel.Controls.Find("lblValue3", true)[0];
            lblActiveRentals = (Label)statsPanel.Controls.Find("lblValue4", true)[0];
            lblAvailableVehicles = (Label)statsPanel.Controls.Find("lblValue5", true)[0];
            lblPendingPayments = (Label)statsPanel.Controls.Find("lblValue6", true)[0];
            lblMaintenanceAlerts = (Label)statsPanel.Controls.Find("lblValue7", true)[0];

            // Recent Activity Container - Now takes full width
            var activityPanel = new Panel
            {
                Location = new Point(20, 260),
                Size = new Size(1100, 390),
                BackColor = CardBg,
                Padding = new Padding(15),
            };

            activityPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, activityPanel.ClientRectangle,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid);
            };


            var lblActivityTitle = new Label
            {
                Text = "📋 RECENT ACTIVITY",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(10, 5),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };


            // Tabs for different activity types
            var tabControl = new TabControl
            {
                Location = new Point(10, 40),
                Size = new Size(1060, 290),
                Font = new Font("Segoe UI", 9),

            };
            tabControl.Appearance = TabAppearance.Normal;
            tabControl.ItemSize = new Size(160, 28);
            tabControl.SizeMode = TabSizeMode.Fixed;


            // Rentals tab
            var tabRentals = new TabPage("📅 Recent Rentals (Last 10)");
            lstRecentRentals = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                ItemHeight = 30,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94),
                SelectionMode = SelectionMode.One
            };

            lstRecentRentals.DrawMode = DrawMode.OwnerDrawFixed;
            lstRecentRentals.DrawItem += DrawListItem;

            tabRentals.Controls.Add(lstRecentRentals);

            // Payments tab
            var tabPayments = new TabPage("💰 Recent Payments (Last 10)");
            lstRecentPayments = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                ItemHeight = 30,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94),
                SelectionMode = SelectionMode.One
            };
            lstRecentPayments.DrawMode = DrawMode.OwnerDrawFixed;
            lstRecentPayments.DrawItem += DrawListItem;
            tabPayments.Controls.Add(lstRecentPayments);

            // Maintenance tab
            var tabMaintenance = new TabPage("🔧 Maintenance Alerts");
            lstRecentMaintenance = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                ItemHeight = 30,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94),
                SelectionMode = SelectionMode.One
            };
            lstRecentMaintenance.DrawMode = DrawMode.OwnerDrawFixed;
            lstRecentMaintenance.DrawItem += DrawListItem;
            tabMaintenance.Controls.Add(lstRecentMaintenance);

            tabControl.TabPages.Add(tabRentals);
            tabControl.TabPages.Add(tabPayments);
            tabControl.TabPages.Add(tabMaintenance);

            activityPanel.Controls.Add(lblActivityTitle);
            activityPanel.Controls.Add(tabControl);

            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(5),
                BackColor = Color.Transparent
            };

            var btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Size = new Size(120, 32),
                Location = new Point(5, 6),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadDashboardData();

            lblLastUpdated = new Label
            {
                Text = "Last updated: --:--:--",
                AutoSize = true,
                Location = new Point(140, 12),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };

            footerPanel.Controls.Add(btnRefresh);
            footerPanel.Controls.Add(lblLastUpdated);
            activityPanel.Controls.Add(footerPanel);



            mainPanel.Controls.AddRange(new Control[]
            {
                    lblTitle,
                    lblSubtitle,
                    statsPanel,
                    activityPanel
            });


            this.Controls.Add(mainPanel);
        }
        private void ShowEmptyState(ListBox listBox, string message)
        {
            listBox.Items.Clear();
            listBox.Items.Add("📭 " + message);
            listBox.ForeColor = Color.Gray;
        }


        private void DrawListItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            ListBox listBox = sender as ListBox;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color backgroundColor = isSelected
                ? Color.FromArgb(230, 244, 255)   // soft blue
                : Color.White;

            Color textColor = Color.FromArgb(52, 73, 94);

            using (SolidBrush bgBrush = new SolidBrush(backgroundColor))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                e.Graphics.DrawString(
                    listBox.Items[e.Index].ToString(),
                    e.Font,
                    textBrush,
                    e.Bounds.Left + 10,
                    e.Bounds.Top + 7
                );
            }

            e.DrawFocusRectangle();
        }

        private Panel CreateStatCard(string title, string value, Color color, int xPosition)
        {
            var card = new Panel
            {
                Location = new Point(xPosition, 0),
                Size = new Size(130, 150),
                BackColor = CardBg,
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };

            // Fake shadow
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid,
                    BorderSoft, 1, ButtonBorderStyle.Solid);
            };

            // Hover effect
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 250, 250);
            card.MouseLeave += (s, e) => card.BackColor = CardBg;

            var colorBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = color
            };

            var lblIcon = new Label
            {
                Text = title.Substring(0, 2),
                Font = new Font("Segoe UI Emoji", 22),
                Location = new Point(45, 20),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = title.Substring(2).Trim(),
                Font = CardTitleFont,
                Location = new Point(5, 70),
                Size = new Size(120, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };

            var lblValue = new Label
            {
                Text = value,
                Name = $"lblValue{xPosition / 140}",
                Font = CardValueFont,
                Location = new Point(5, 100),
                Size = new Size(120, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = color
            };

            card.Controls.AddRange(new Control[]
            {
        colorBar, lblIcon, lblTitle, lblValue
            });

            return card;
        }


        private void LoadDashboardData()
        {
            // Don't load if control is being disposed or not created yet
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (isLoading) return;

            lock (_lock)
            {
                if (isLoading) return;
                isLoading = true;
            }

            try
            {
                SafeUpdateLastUpdatedText("Loading...");

                // Run on background thread
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        var connectionString = "Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;";

                        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                        optionsBuilder.UseSqlServer(connectionString);

                        using (var freshContext = new ApplicationDbContext(optionsBuilder.Options))
                        {
                            LoadStatistics(freshContext);
                            LoadRecentActivities(freshContext);
                        }

                        SafeInvoke(() => UpdateLastUpdatedText($"Last updated: {DateTime.Now:HH:mm:ss}"));
                    }
                    catch (Exception ex)
                    {
                        SafeInvoke(() =>
                        {
                            UpdateLastUpdatedText($"Error: {DateTime.Now:HH:mm:ss}");
                            MessageBox.Show($"Error loading dashboard data: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            isLoading = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SafeUpdateLastUpdatedText($"Error: {DateTime.Now:HH:mm:ss}");
                MessageBox.Show($"Error starting dashboard load: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lock (_lock)
                {
                    isLoading = false;
                }
            }
        }

        private void LoadStatistics(ApplicationDbContext context)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime sixMonthsAgo = now.AddMonths(-6);

                // Load all statistics sequentially
                int totalClients = context.Clients.Count();
                int totalVehicles = context.Vehicles.Count();
                int totalRentals = context.Rentals.Count();

                // Get total revenue from paid payments
                decimal totalRevenue = context.Payments
                    .Where(p => p.Status == "Paid")
                    .Sum(p => p.Amount);

                int activeRentals = context.Rentals
                    .Count(r => r.StartDate <= now && r.EndDate >= now);

                int availableVehicles = context.Vehicles
                    .Count(v => v.IsAvailable);

                int pendingPayments = context.Payments
                    .Count(p => p.Status == "Pending");

                int maintenanceAlerts = context.Vehicles
                    .Count(v => v.LastMaintenanceDate < sixMonthsAgo);

                // Update UI on the main thread
                SafeInvoke(() =>
                {
                    lblTotalClients.Text = totalClients.ToString();
                    lblTotalVehicles.Text = totalVehicles.ToString();
                    lblTotalRentals.Text = totalRentals.ToString();
                    lblTotalRevenue.Text = $"${totalRevenue:F2}";
                    lblActiveRentals.Text = activeRentals.ToString();
                    lblAvailableVehicles.Text = availableVehicles.ToString();
                    lblPendingPayments.Text = pendingPayments.ToString();
                    lblMaintenanceAlerts.Text = maintenanceAlerts.ToString();
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading statistics: {ex.Message}", ex);
            }
        }

        private void LoadRecentActivities(ApplicationDbContext context)
        {
            try
            {
                // Recent rentals (last 10)
                var recentRentals = context.Rentals
                    .Include(r => r.Client)
                    .Include(r => r.Vehicle)
                    .OrderByDescending(r => r.CreatedAt > DateTime.MinValue
                    ? r.CreatedAt
    :               r.StartDate)
                    .Take(10)
                    .ToList();

                // Recent payments (last 10)
                var recentPayments = context.Payments
                    .Include(p => p.Rental)
                        .ThenInclude(r => r.Client)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .ToList();

                // Recent maintenance (vehicles with oldest maintenance)
                var recentMaintenance = context.Vehicles
                    .Where(v => v.LastMaintenanceDate < DateTime.Now.AddMonths(-6))
                    .OrderBy(v => v.LastMaintenanceDate)
                    .Take(10)
                    .ToList();

                // Update UI on the main thread
                SafeInvoke(() =>
                {
                    UpdateActivityLists(recentRentals, recentPayments, recentMaintenance);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading recent activities: {ex.Message}", ex);
            }
        }

        private void UpdateActivityLists(List<Rental> rentals, List<Payment> payments, List<Vehicle> maintenanceVehicles)
        {
            if (this.IsDisposed) return;

            lstRecentRentals.Items.Clear();
            if (rentals.Any())
            {
                lstRecentRentals.ForeColor = Color.FromArgb(52, 73, 94);
                foreach (var rental in rentals)
                {
                    string status = rental.StartDate > DateTime.Now ? "📅" :
                                   rental.EndDate < DateTime.Now ? "📋" : "✅";
                    lstRecentRentals.Items.Add(
                        $"{status} {rental.Client?.FirstName} {rental.Client?.LastName} - " +
                        $"{rental.Vehicle?.Brand} {rental.Vehicle?.Model} - " +
                        $"{rental.StartDate:MM/dd}");
                }
            }
            else
            {
                ShowEmptyState(lstRecentRentals, "No recent rentals");
            }



            lstRecentPayments.Items.Clear();
            if (payments.Any())
            {
                lstRecentPayments.ForeColor = Color.FromArgb(52, 73, 94);
                foreach (var payment in payments)
                {
                    string statusIcon = payment.Status == "Paid" ? "✅" : "⏳";
                    lstRecentPayments.Items.Add(
                        $"{statusIcon} PAY-{payment.Id:0000} - " +
                        $"{payment.Rental?.Client?.FirstName} {payment.Rental?.Client?.LastName} - " +
                        $"${payment.Amount:F2} - {payment.PaymentDate:MM/dd HH:mm}");
                }
            }
            else
            {
                ShowEmptyState(lstRecentPayments, "No recent payments");
            }



            lstRecentMaintenance.Items.Clear();
            if (maintenanceVehicles.Any())
            {
                lstRecentMaintenance.ForeColor = Color.FromArgb(52, 73, 94);
                foreach (var vehicle in maintenanceVehicles)
                {
                    int daysOverdue = (DateTime.Now - vehicle.LastMaintenanceDate).Days;
                    lstRecentMaintenance.Items.Add(
                        $"⚠️ {vehicle.Brand} {vehicle.Model} - " +
                        $"Last: {vehicle.LastMaintenanceDate:yyyy-MM-dd} - " +
                        $"{daysOverdue} days overdue");
                }
            }
            else
            {
                ShowEmptyState(lstRecentMaintenance, "No maintenance alerts");
            }


        }

        private void UpdateLastUpdatedText(string text)
        {
            if (this.IsDisposed || lblLastUpdated == null) return;

            lblLastUpdated.Text = text;
        }

        private void SafeUpdateLastUpdatedText(string text)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action(() => UpdateLastUpdatedText(text)));
                }
                catch (ObjectDisposedException)
                {
                    // Control is being disposed, ignore
                }
            }
            else
            {
                UpdateLastUpdatedText(text);
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // Control is being disposed, ignore
                }
            }
            else
            {
                action();
            }
        }

        private void Cleanup()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
                refreshTimer = null;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Cleanup();
            base.OnHandleDestroyed(e);
        }
    }
}