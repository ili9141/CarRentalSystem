using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class AdminsUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private int _currentAdminId;
        private DataGridView dataGridView;
        private Button btnAddAdmin;
        private Button btnDeleteAdmin;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;

        public AdminsUserControl(ApplicationDbContext context, int currentAdminId = 0)
        {
            // Create fresh context to avoid disposed errors
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);
            _currentAdminId = currentAdminId;

            SetupUI();
            LoadAdmins();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Create controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 50 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Search controls
            txtSearch = new TextBox
            {
                PlaceholderText = "Search by username or email...",
                Width = 250,
                Location = new Point(10, 10)
            };

            btnSearch = new Button
            {
                Text = "Search",
                Location = new Point(270, 10),
                Width = 80,
                AutoSize = true
            };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(360, 10),
                Width = 80,
                AutoSize = true
            };
            btnRefresh.Click += BtnRefresh_Click;

            // Action buttons
            btnAddAdmin = new Button
            {
                Text = "+ Add Admin",
                Location = new Point(450, 10),
                Width = 100,
                BackColor = Color.LightGreen,
                AutoSize = true
            };
            btnAddAdmin.Click += BtnAddAdmin_Click;

            btnDeleteAdmin = new Button
            {
                Text = "Delete Selected",
                Location = new Point(570, 10),
                Width = 120,
                BackColor = Color.LightCoral,
                AutoSize = true
            };
            btnDeleteAdmin.Click += BtnDeleteAdmin_Click;

            // Add to top panel
            panelTop.Controls.AddRange(new Control[]
            {
                txtSearch, btnSearch, btnRefresh, btnAddAdmin, btnDeleteAdmin
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

        private void LoadAdmins()
        {
            try
            {
                var admins = _context.AdminUsers
                    .OrderBy(a => a.CreatedAt)
                    .ToList();

                dataGridView.DataSource = admins.Select(a => new
                {
                    a.Id,
                    a.Username,
                    a.Email,
                    CreatedAt = a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Current = (a.Id == _currentAdminId) ? "✓" : ""
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading admins: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var search = txtSearch.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(search))
            {
                LoadAdmins();
                return;
            }

            try
            {
                var admins = _context.AdminUsers
                    .Where(a => a.Username.ToLower().Contains(search) ||
                               a.Email.ToLower().Contains(search))
                    .OrderBy(a => a.CreatedAt)
                    .ToList();

                dataGridView.DataSource = admins.Select(a => new
                {
                    a.Id,
                    a.Username,
                    a.Email,
                    CreatedAt = a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Current = (a.Id == _currentAdminId) ? "✓" : ""
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Search error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            LoadAdmins();
        }

        private void BtnAddAdmin_Click(object sender, EventArgs e)
        {
            using (var addForm = new AddAdminForm())
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create new admin with BCrypt hash
                        var newAdmin = new AdminUser
                        {
                            Username = addForm.Username,
                            Email = addForm.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(addForm.Password),
                            CreatedAt = DateTime.Now
                        };

                        _context.AdminUsers.Add(newAdmin);
                        _context.SaveChanges();

                        MessageBox.Show($"Admin '{addForm.Username}' added successfully!",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadAdmins();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding admin: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDeleteAdmin_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select an admin to delete",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var username = dataGridView.CurrentRow.Cells["Username"].Value.ToString();

            // Prevent deleting yourself
            if (id == _currentAdminId)
            {
                MessageBox.Show("You cannot delete your own account!",
                    "Self-Deletion Prevented", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm deletion
            var result = MessageBox.Show(
                $"Delete admin '{username}'? This cannot be undone!",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var admin = _context.AdminUsers.Find(id);
                    if (admin != null)
                    {
                        _context.AdminUsers.Remove(admin);
                        _context.SaveChanges();

                        MessageBox.Show($"Admin '{username}' deleted",
                            "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadAdmins();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Add Admin Form
        private class AddAdminForm : Form
        {
            private TextBox txtUsername;
            private TextBox txtEmail;
            private TextBox txtPassword;
            private TextBox txtConfirmPassword;
            private Button btnSave;
            private Button btnCancel;

            public string Username => txtUsername.Text.Trim();
            public string Email => txtEmail.Text.Trim();
            public string Password => txtPassword.Text;

            public AddAdminForm()
            {
                InitializeForm();
            }

            private void InitializeForm()
            {
                this.Text = "Add New Admin";
                this.Size = new Size(400, 280);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                // Labels and textboxes
                var lblUsername = new Label { Text = "Username:", Location = new Point(20, 20), Width = 100 };
                txtUsername = new TextBox { Location = new Point(130, 17), Width = 230 };

                var lblEmail = new Label { Text = "Email:", Location = new Point(20, 60), Width = 100 };
                txtEmail = new TextBox { Location = new Point(130, 57), Width = 230 };

                var lblPassword = new Label { Text = "Password:", Location = new Point(20, 100), Width = 100 };
                txtPassword = new TextBox { Location = new Point(130, 97), Width = 230, UseSystemPasswordChar = true };

                var lblConfirm = new Label { Text = "Confirm:", Location = new Point(20, 140), Width = 100 };
                txtConfirmPassword = new TextBox { Location = new Point(130, 137), Width = 230, UseSystemPasswordChar = true };

                // Buttons
                btnSave = new Button { Text = "Save", Location = new Point(130, 190), Width = 100, AutoSize = true };
                btnSave.Click += BtnSave_Click;

                btnCancel = new Button { Text = "Cancel", Location = new Point(240, 190), Width = 100, AutoSize = true };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                // Add controls
                this.Controls.AddRange(new Control[]
                {
                    lblUsername, txtUsername,
                    lblEmail, txtEmail,
                    lblPassword, txtPassword,
                    lblConfirm, txtConfirmPassword,
                    btnSave, btnCancel
                });
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                // Validation
                if (string.IsNullOrWhiteSpace(Username))
                {
                    MessageBox.Show("Username is required", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
                {
                    MessageBox.Show("Valid email is required", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password) || Password.Length < 4)
                {
                    MessageBox.Show("Password must be at least 4 characters", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (Password != txtConfirmPassword.Text)
                {
                    MessageBox.Show("Passwords do not match", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}