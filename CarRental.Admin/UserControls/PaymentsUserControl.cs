using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class PaymentsUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private DataGridView dataGridView;
        private Button btnViewDetails;
        private Button btnMarkPaid;
        private Button btnGenerateReceipt;
        private Button btnRefresh;
        private TextBox txtSearch;
        private Button btnSearch;
        private ComboBox cmbFilterMethod;
        private ComboBox cmbFilterStatus;
        private DateTimePicker dtpFromDate;
        private DateTimePicker dtpToDate;
        private Label lblStats;

        public PaymentsUserControl(ApplicationDbContext context)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);

            SetupUI();
            LoadPayments();
            UpdateStats();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Top panel with controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 130 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Stats label
            lblStats = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(600, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            // Search section
            var lblSearch = new Label { Text = "Search:", Location = new Point(10, 45), AutoSize = true };
            txtSearch = new TextBox
            {
                PlaceholderText = "Search by client, rental ID...",
                Location = new Point(60, 42),
                Width = 200
            };

            btnSearch = new Button { Text = "Search", Location = new Point(270, 40), Width = 80 };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button { Text = "Clear", Location = new Point(360, 40), Width = 80 };
            btnRefresh.Click += BtnRefresh_Click;

            // Date filters
            var lblDates = new Label { Text = "Date Range:", Location = new Point(10, 85), AutoSize = true };
            dtpFromDate = new DateTimePicker
            {
                Location = new Point(80, 82),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-1)
            };

            var lblTo = new Label { Text = "to", Location = new Point(195, 85), AutoSize = true };
            dtpToDate = new DateTimePicker
            {
                Location = new Point(210, 82),
                Width = 110,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            dtpFromDate.ValueChanged += DateFilterChanged;
            dtpToDate.ValueChanged += DateFilterChanged;

            // Payment method filter
            var lblMethod = new Label { Text = "Method:", Location = new Point(330, 85), AutoSize = true };
            cmbFilterMethod = new ComboBox { Location = new Point(380, 82), Width = 100 };
            cmbFilterMethod.Items.AddRange(new string[] {
                "All Methods",
                "Cash",
                "Credit Card",
                "Bank Transfer"
            });
            cmbFilterMethod.SelectedIndex = 0;
            cmbFilterMethod.SelectedIndexChanged += FilterChanged;

            // Status filter
            var lblStatus = new Label { Text = "Status:", Location = new Point(490, 85), AutoSize = true };
            cmbFilterStatus = new ComboBox { Location = new Point(530, 82), Width = 100 };
            cmbFilterStatus.Items.AddRange(new string[] {
                "All Status",
                "Paid",
                "Pending"
            });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += FilterChanged;

            // Action buttons
            btnViewDetails = new Button
            {
                Text = "📋 View Details",
                Location = new Point(640, 40),
                Width = 120
            };
            btnViewDetails.Click += BtnViewDetails_Click;

            btnMarkPaid = new Button
            {
                Text = "✅ Mark as Paid",
                Location = new Point(640, 82),
                Width = 120,
                BackColor = Color.LightGreen
            };
            btnMarkPaid.Click += BtnMarkPaid_Click;

            btnGenerateReceipt = new Button
            {
                Text = "🧾 View Receipt",
                Location = new Point(770, 40),
                Width = 140
            };
            btnGenerateReceipt.Click += BtnGenerateReceipt_Click;

            // Add to top panel
            panelTop.Controls.AddRange(new Control[]
            {
                lblStats,
                lblSearch, txtSearch, btnSearch, btnRefresh,
                lblDates, dtpFromDate, lblTo, dtpToDate,
                lblMethod, cmbFilterMethod, lblStatus, cmbFilterStatus,
                btnViewDetails, btnMarkPaid, btnGenerateReceipt
            });

            // DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            panelGrid.Controls.Add(dataGridView);

            // Add panels to user control
            this.Controls.Add(panelGrid);
            this.Controls.Add(panelTop);
        }

        private void LoadPayments(string searchTerm = "", string method = "All Methods", string status = "All Status")
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Rental)
                        .ThenInclude(r => r.Client)
                    .Include(p => p.Rental)
                        .ThenInclude(r => r.Vehicle)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    string search = searchTerm.ToLower();
                    query = query.Where(p =>
                        (p.Rental.Client.FirstName + " " + p.Rental.Client.LastName).ToLower().Contains(search) ||
                        p.RentalId.ToString().Contains(search));
                }

                // Apply date filter
                query = query.Where(p => p.PaymentDate.Date >= dtpFromDate.Value.Date &&
                                         p.PaymentDate.Date <= dtpToDate.Value.Date);

                // Apply method filter
                if (method != "All Methods")
                {
                    query = query.Where(p => p.PaymentMethod == method);
                }

                // Apply status filter
                if (status != "All Status")
                {
                    query = query.Where(p => p.Status == status);
                }

                var payments = query
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                var paymentData = payments.Select(p => new
                {
                    p.Id,
                    ReceiptNo = $"PAY-{p.Id:0000}", // Generate receipt number from ID
                    Client = $"{p.Rental.Client.FirstName} {p.Rental.Client.LastName}",
                    RentalId = p.RentalId,
                    Vehicle = $"{p.Rental.Vehicle.Brand} {p.Rental.Vehicle.Model}",
                    Amount = $"${p.Amount:F2}",
                    Method = p.PaymentMethod,
                    Status = GetStatusWithIcon(p.Status),
                    Date = p.PaymentDate.ToString("yyyy-MM-dd HH:mm")
                }).ToList();

                dataGridView.DataSource = paymentData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStatusWithIcon(string status)
        {
            return status switch
            {
                "Paid" => "✅ Paid",
                "Pending" => "⏳ Pending",
                _ => status
            };
        }

        private void UpdateStats()
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime monthStart = new DateTime(today.Year, today.Month, 1);

                // Total revenue (only paid payments)
                decimal totalRevenue = _context.Payments
                    .Where(p => p.Status == "Paid")
                    .Sum(p => p.Amount);

                // Monthly revenue
                decimal monthlyRevenue = _context.Payments
                    .Where(p => p.Status == "Paid" && p.PaymentDate >= monthStart)
                    .Sum(p => p.Amount);

                // Pending payments
                int pendingCount = _context.Payments.Count(p => p.Status == "Pending");
                decimal pendingAmount = _context.Payments
                    .Where(p => p.Status == "Pending")
                    .Sum(p => p.Amount);

                // Today's revenue
                decimal todayRevenue = _context.Payments
                    .Where(p => p.Status == "Paid" && p.PaymentDate.Date == today)
                    .Sum(p => p.Amount);

                lblStats.Text = $"💰 Stats: Total Revenue: ${totalRevenue:F2} | Monthly: ${monthlyRevenue:F2} | " +
                              $"Today: ${todayRevenue:F2} | Pending: {pendingCount} (${pendingAmount:F2})";
            }
            catch
            {
                lblStats.Text = "💰 Stats: Loading...";
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            LoadPayments(txtSearch.Text, cmbFilterMethod.SelectedItem.ToString(), cmbFilterStatus.SelectedItem.ToString());
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbFilterMethod.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndex = 0;
            dtpFromDate.Value = DateTime.Today.AddMonths(-1);
            dtpToDate.Value = DateTime.Today;
            LoadPayments();
            UpdateStats();
        }

        private void DateFilterChanged(object sender, EventArgs e)
        {
            if (dtpFromDate.Value > dtpToDate.Value)
            {
                dtpToDate.Value = dtpFromDate.Value;
            }
            LoadPayments(txtSearch.Text, cmbFilterMethod.SelectedItem.ToString(), cmbFilterStatus.SelectedItem.ToString());
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            LoadPayments(txtSearch.Text, cmbFilterMethod.SelectedItem.ToString(), cmbFilterStatus.SelectedItem.ToString());
        }

        private void BtnViewDetails_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a payment to view details", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int paymentId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var payment = _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Client)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Vehicle)
                .FirstOrDefault(p => p.Id == paymentId);

            if (payment == null)
            {
                MessageBox.Show("Payment not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Simple details in MessageBox (no nested form needed)
            string details = $"💰 PAYMENT DETAILS #{payment.Id}\n\n" +
                           $"Client: {payment.Rental.Client.FirstName} {payment.Rental.Client.LastName}\n" +
                           $"Vehicle: {payment.Rental.Vehicle.Brand} {payment.Rental.Vehicle.Model}\n" +
                           $"Rental ID: {payment.RentalId}\n" +
                           $"Amount: ${payment.Amount:F2}\n" +
                           $"Method: {payment.PaymentMethod}\n" +
                           $"Status: {payment.Status}\n" +
                           $"Date: {payment.PaymentDate:yyyy-MM-dd HH:mm}\n" +
                           $"Receipt: PAY-{payment.Id:0000}";

            MessageBox.Show(details, "Payment Details",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnMarkPaid_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a payment to mark as paid", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int paymentId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            string receiptNo = dataGridView.CurrentRow.Cells["ReceiptNo"].Value.ToString();
            string amount = dataGridView.CurrentRow.Cells["Amount"].Value.ToString();
            string currentStatus = dataGridView.CurrentRow.Cells["Status"].Value.ToString();

            if (currentStatus.Contains("✅"))
            {
                MessageBox.Show("This payment is already marked as paid", "Already Paid",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Mark payment {receiptNo} ({amount}) as paid?\n\n" +
                "This will update the payment status and record the current date/time.",
                "Confirm Payment",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var payment = _context.Payments.Find(paymentId);
                    if (payment != null)
                    {
                        payment.Status = "Paid";
                        payment.PaymentDate = DateTime.Now;
                        payment.PaymentMethod = payment.PaymentMethod ?? "Cash"; // Default if null

                        _context.SaveChanges();

                        MessageBox.Show($"Payment {receiptNo} marked as paid successfully!", "Payment Updated",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadPayments(txtSearch.Text, cmbFilterMethod.SelectedItem.ToString(), cmbFilterStatus.SelectedItem.ToString());
                        UpdateStats();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating payment: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnGenerateReceipt_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a payment to generate receipt", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int paymentId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var payment = _context.Payments
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Client)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Vehicle)
                .FirstOrDefault(p => p.Id == paymentId);

            if (payment == null)
            {
                MessageBox.Show("Payment not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (payment.Status != "Paid")
            {
                MessageBox.Show("Cannot generate receipt for unpaid payments", "Not Paid",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Simple receipt in MessageBox
            string receipt = $"═══════════════════════════════\n" +
                           $"         CAR RENTAL RECEIPT\n" +
                           $"═══════════════════════════════\n\n" +
                           $"Receipt: PAY-{payment.Id:0000}\n" +
                           $"Date: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n" +
                           $"Client: {payment.Rental.Client.FirstName} {payment.Rental.Client.LastName}\n" +
                           $"Vehicle: {payment.Rental.Vehicle.Brand} {payment.Rental.Vehicle.Model}\n" +
                           $"Rental ID: {payment.RentalId}\n\n" +
                           $"═══════════════════════════════\n" +
                           $"Amount Paid: ${payment.Amount:F2}\n" +
                           $"Payment Method: {payment.PaymentMethod}\n" +
                           $"Status: {payment.Status}\n" +
                           $"Payment Date: {payment.PaymentDate:yyyy-MM-dd HH:mm}\n" +
                           $"═══════════════════════════════\n\n" +
                           $"Thank you for your business!\n" +
                           $"Car Rental System";

            MessageBox.Show(receipt, $"Receipt PAY-{payment.Id:0000}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}