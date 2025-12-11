using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class ClientsUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private DataGridView dataGridView;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private Button btnViewDetails;
        private Button btnViewRentals;
        private ComboBox cmbSortBy;

        public ClientsUserControl(ApplicationDbContext context)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);

            SetupUI();
            LoadClients();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Top panel with controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 80 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Search section
            var lblSearch = new Label { Text = "Search:", Location = new Point(10, 15), AutoSize = true };
            txtSearch = new TextBox
            {
                PlaceholderText = "Search by name, email, phone, license...",
                Location = new Point(60, 12),
                Width = 300
            };

            btnSearch = new Button { Text = "Search", Location = new Point(370, 10), Width = 80 };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button { Text = "Clear", Location = new Point(460, 10), Width = 80 };
            btnRefresh.Click += BtnRefresh_Click;

            // Sort dropdown
            var lblSort = new Label { Text = "Sort by:", Location = new Point(10, 50), AutoSize = true };
            cmbSortBy = new ComboBox { Location = new Point(60, 47), Width = 150 };
            cmbSortBy.Items.AddRange(new string[] {
                "Name (A-Z)",
                "Name (Z-A)",
                "Newest First",
                "Oldest First",
                "Most Rentals",
                "Least Rentals"
            });
            cmbSortBy.SelectedIndex = 0;
            cmbSortBy.SelectedIndexChanged += CmbSortBy_SelectedIndexChanged;

            // Action buttons (VIEW ONLY - no create/delete)
            btnViewDetails = new Button
            {
                Text = "📋 View Details",
                Location = new Point(550, 10),
                Width = 120,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            btnViewDetails.Click += BtnViewDetails_Click;

            btnViewRentals = new Button
            {
                Text = "📅 View Rentals",
                Location = new Point(680, 10),
                Width = 120,
                BackColor = Color.DarkOrange,
                ForeColor = Color.White
            };
            btnViewRentals.Click += BtnViewRentals_Click;

            // Info label (remind admin about read-only)
            var lblInfo = new Label
            {
                Text = "ℹ️ Clients self-register via website. Admin can only view client information.",
                Location = new Point(220, 50),
                AutoSize = true,
                ForeColor = Color.DarkSlateGray,
                Font = new Font("Arial", 8)
            };

            // Add to top panel
            panelTop.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, btnSearch, btnRefresh,
                lblSort, cmbSortBy, lblInfo,
                btnViewDetails, btnViewRentals
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



        private void LoadClients(string searchTerm = "", string sortBy = "Name (A-Z)")
        {
            try
            {
                var query = _context.Clients.AsQueryable();

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    string search = searchTerm.ToLower();
                    query = query.Where(c =>
                        c.FirstName.ToLower().Contains(search) ||
                        c.LastName.ToLower().Contains(search) ||
                        c.Email.ToLower().Contains(search) ||
                        c.Phone.Contains(search) ||
                        c.LicenseNumber.ToLower().Contains(search));
                }

                // Apply sorting
                switch (sortBy)
                {
                    case "Name (A-Z)":
                        query = query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
                        break;
                    case "Name (Z-A)":
                        query = query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName);
                        break;
                    case "Newest First":
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case "Oldest First":
                        query = query.OrderBy(c => c.CreatedAt);
                        break;
                    case "Most Rentals":
                        query = query.OrderByDescending(c => c.Rentals.Count);
                        break;
                    case "Least Rentals":
                        query = query.OrderBy(c => c.Rentals.Count);
                        break;
                }

                var clients = query.ToList();

                // Get rental counts for each client
                var clientData = clients.Select(c => new
                {
                    c.Id,
                    Name = $"{c.FirstName} {c.LastName}",
                    c.Email,
                    c.Phone,
                    License = c.LicenseNumber,
                    Address = c.Address?.Length > 30 ? c.Address.Substring(0, 30) + "..." : c.Address ?? "",
                    TotalRentals = c.Rentals?.Count ?? 0,
                    MemberSince = c.CreatedAt.ToString("yyyy-MM-dd"),
                    LastRental = c.Rentals != null && c.Rentals.Any()
                        ? c.Rentals.Max(r => r.StartDate).ToString("yyyy-MM-dd")
                        : "Never"
                }).ToList();

                dataGridView.DataSource = clientData;

            

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading clients: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            LoadClients(txtSearch.Text, cmbSortBy.SelectedItem.ToString());
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbSortBy.SelectedIndex = 0;
            LoadClients();
        }

        private void CmbSortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadClients(txtSearch.Text, cmbSortBy.SelectedItem.ToString());
        }

        private void BtnViewDetails_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a client first", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int clientId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var client = _context.Clients
                .Include(c => c.Rentals)
                .ThenInclude(r => r.Vehicle)
                .FirstOrDefault(c => c.Id == clientId);

            if (client == null)
            {
                MessageBox.Show("Client not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Show client details form
            using (var form = new ClientDetailsForm(client))
            {
                form.ShowDialog();
            }
        }

        private void BtnViewRentals_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a client first", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int clientId = (int)dataGridView.CurrentRow.Cells["Id"].Value;

            try
            {
                var client = _context.Clients
                    .Include(c => c.Rentals)
                        .ThenInclude(r => r.Vehicle) // MAKE SURE Vehicle is included
                    .FirstOrDefault(c => c.Id == clientId);

                if (client == null)
                {
                    MessageBox.Show("Client not found", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Show client rentals form
                using (var form = new ClientRentalsForm(client))
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Nested form for client details
        private class ClientDetailsForm : Form
        {
            public ClientDetailsForm(Client client)
            {
                InitializeForm(client);
            }

            private void InitializeForm(Client client)
            {
                this.Text = $"Client Details: {client.FirstName} {client.LastName}";
                this.Size = new Size(500, 400);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;
                int labelWidth = 120;

                // Client Information
                var lblTitle = new Label
                {
                    Text = "📋 CLIENT INFORMATION",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Personal Info
                AddLabelValue(panel, "Full Name:", $"{client.FirstName} {client.LastName}", ref y, labelWidth);
                AddLabelValue(panel, "Email:", client.Email, ref y, labelWidth);
                AddLabelValue(panel, "Phone:", client.Phone, ref y, labelWidth);
                AddLabelValue(panel, "License #:", client.LicenseNumber, ref y, labelWidth);

                y += 10;

                // Address (multiline)
                var lblAddress = new Label { Text = "Address:", Location = new Point(20, y), Width = labelWidth };
                var txtAddress = new TextBox
                {
                    Location = new Point(150, y - 3),
                    Width = 300,
                    Height = 60,
                    Multiline = true,
                    ReadOnly = true,
                    Text = client.Address,
                    ScrollBars = ScrollBars.Vertical
                };
                panel.Controls.Add(lblAddress);
                panel.Controls.Add(txtAddress);
                y += 70;

                // Statistics
                AddLabelValue(panel, "Member Since:", client.CreatedAt.ToString("yyyy-MM-dd HH:mm"), ref y, labelWidth);
                AddLabelValue(panel, "Total Rentals:", (client.Rentals?.Count ?? 0).ToString(), ref y, labelWidth);

                // Calculate total spent
                decimal totalSpent = client.Rentals?.Sum(r => r.TotalPrice) ?? 0;
                AddLabelValue(panel, "Total Spent:", $"${totalSpent:F2}", ref y, labelWidth);

                // Close button
                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(200, y + 20),
                    Width = 100
                };
                btnClose.Click += (s, e) => this.Close();

                panel.Controls.Add(lblTitle);
                panel.Controls.Add(btnClose);
                this.Controls.Add(panel);
            }

            private void AddLabelValue(Panel panel, string label, string value, ref int y, int labelWidth)
            {
                var lbl = new Label { Text = label, Location = new Point(20, y), Width = labelWidth };
                var lblValue = new Label { Text = value, Location = new Point(150, y), AutoSize = true };

                panel.Controls.Add(lbl);
                panel.Controls.Add(lblValue);
                y += 30;
            }
        }

        // Nested form for client rentals
        // Nested form for client rentals
        private class ClientRentalsForm : Form
        {
            public ClientRentalsForm(Client client)
            {
                InitializeForm(client);
            }

            private void InitializeForm(Client client)
            {
                this.Text = $"Rental History: {client.FirstName} {client.LastName}";
                this.Size = new Size(700, 500);
                this.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

                // Title
                var lblTitle = new Label
                {
                    Text = $"📅 RENTAL HISTORY FOR {client.FirstName.ToUpper()} {client.LastName.ToUpper()}",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(10, 10),
                    AutoSize = true
                };

                // DataGridView for rentals
                var dataGridView = new DataGridView
                {
                    Location = new Point(10, 50),
                    Size = new Size(660, 380),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                if (client.Rentals != null && client.Rentals.Any())
                {
                    // SAFE: Handle null Vehicle references
                    var rentalData = client.Rentals.Select(r => new
                    {
                        r.Id,
                        Vehicle = r.Vehicle != null ? $"{r.Vehicle.Brand} {r.Vehicle.Model}" : "[Vehicle Deleted]",
                        Period = $"{r.StartDate:yyyy-MM-dd} to {r.EndDate:yyyy-MM-dd}",
                        Days = (r.EndDate - r.StartDate).Days,
                        Total = $"${r.TotalPrice:F2}",
                        BookedOn = r.CreatedAt.ToString("yyyy-MM-dd")
                    }).ToList();

                    dataGridView.DataSource = rentalData;

                    // Format
                    if (dataGridView.Columns.Contains("Id"))
                        dataGridView.Columns["Id"].Visible = false;

                    if (dataGridView.Columns.Contains("Days"))
                        dataGridView.Columns["Days"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else
                {
                    dataGridView.Visible = false;

                    var lblNoRentals = new Label
                    {
                        Text = "No rental history found",
                        Font = new Font("Arial", 14, FontStyle.Italic),
                        Location = new Point(250, 200),
                        AutoSize = true,
                        ForeColor = Color.Gray
                    };
                    panel.Controls.Add(lblNoRentals);
                }

                // Summary - with null check
                int totalRentals = client.Rentals?.Count ?? 0;
                decimal totalSpent = client.Rentals?.Sum(r => r.TotalPrice) ?? 0;

                var lblSummary = new Label
                {
                    Text = $"Total Rentals: {totalRentals} | Total Spent: ${totalSpent:F2}",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(10, 440),
                    AutoSize = true
                };

                // Close button
                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(300, 440),
                    Width = 100
                };
                btnClose.Click += (s, e) => this.Close();

                panel.Controls.AddRange(new Control[] { lblTitle, dataGridView, lblSummary, btnClose });
                this.Controls.Add(panel);
            }
        }
    }
}